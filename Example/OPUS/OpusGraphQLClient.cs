using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Puma.MDE.OPUS.Models;


namespace Puma.MDE.OPUS
{
    public class OpusGraphQLClient
    {
        private const string DefaultFriendlyErrorMessage = "Something went wrong while querying OPUS. Please try again or contact support.";
        private readonly OpusHttpClientHandler _opusHttpClientHandler;
        private readonly OpusTokenProvider _opusTokenProvider;
        private readonly OpusConfiguration _opusConfiguration;
        public OpusCircuitBreaker _opusCircuitBreaker;
        public HttpClient _httpClient;

        public OpusGraphQLClient(OpusHttpClientHandler opusHttpClientHandler,
            OpusTokenProvider opusTokenProvider, OpusConfiguration opusConfiguration)
        {
            _opusHttpClientHandler = opusHttpClientHandler;
            _opusTokenProvider = opusTokenProvider ?? throw new ArgumentNullException("tokenProvider");
            _opusConfiguration = opusConfiguration ?? throw new ArgumentNullException("configuration");
            _httpClient = new HttpClient(
                opusHttpClientHandler?._opusHttpClientHandler ?? new HttpClientHandler())
                    ?? throw new ArgumentNullException("httpClient");
            _opusCircuitBreaker = new OpusCircuitBreaker(
                failureThreshold: 4,      // slightly more tolerant for REST calls
                breakSeconds: 90          // longer break time
            );
        }

        public static OpusOperationResult<OpusGraphQLClient> TryCreate(
            OpusHttpClientHandler opusHttpClientHandler,
            OpusTokenProvider opusTokenProvider,
            OpusConfiguration opusConfiguration)
        {
            try
            {
                return OpusOperationResult<OpusGraphQLClient>.SuccessWithData(
                    new OpusGraphQLClient(opusHttpClientHandler, opusTokenProvider, opusConfiguration));
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error("[OpusGraphQLClient.TryCreate] Failed: " + ex.ToString());
                return OpusOperationResult<OpusGraphQLClient>.FailureWithData(DefaultFriendlyErrorMessage, ex.Message);
            }
        }

        public virtual async Task<T> ExecuteAsync<T>(string query, object variables = null)
        {
            Engine.Instance.Log.Info($"[OpusGraphQLClient] ExecuteAsync started for query type: {typeof(T).Name}");

            try
            {
                string token = await _opusTokenProvider.GetAccessTokenAsync().ConfigureAwait(false);

                var request = new GraphQLRequest
                {
                    query = query,
                    variables = variables
                };

                string json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                Engine.Instance.Log.Debug($"[OpusGraphQLClient] Sending GraphQL request to: {_opusConfiguration.GraphQlUrl}");

                // Wrap the HTTP call in circuit breaker
                T result = await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    HttpResponseMessage response = await _httpClient.PostAsync(
                        _opusConfiguration.GraphQlUrl,
                        content).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        Engine.Instance.Log.Error($"[OpusGraphQLClient] GraphQL request failed with status {response.StatusCode}");
                        throw new HttpRequestException(
                            string.Format("GraphQL request failed with status {0}", response.StatusCode));
                    }

                    string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Engine.Instance.Log.Debug($"[OpusGraphQLClient] Response received, parsing...");

                    var graphQLResponse = JsonConvert.DeserializeObject<GraphQLResponse<T>>(responseJson);

                    if (graphQLResponse != null &&
                        graphQLResponse.errors != null &&
                        graphQLResponse.errors.Length > 0)
                    {
                        string errorMsg = graphQLResponse.errors[0].message;
                        Engine.Instance.Log.Error($"[OpusGraphQLClient] GraphQL error: {errorMsg}");
                        throw new Exception(errorMsg);
                    }

                    Engine.Instance.Log.Info($"[OpusGraphQLClient] GraphQL query succeeded for {typeof(T).Name}");
                    return graphQLResponse.data;
                }).ConfigureAwait(false);

                return result;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[OpusGraphQLClient] ExecuteAsync failed: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        [Obsolete("Use ExecuteAsync<T>() and await it from the caller. Calling this synchronous bridge from a WinForms/WPF UI thread can freeze the UI.")]
        public T Execute<T>(string query, object variables = null)
        {
            // IMPORTANT: Blocking on async methods can cause deadlocks in .NET 4.8
            // This method is a backward-compatibility bridge only. Callers should use ExecuteAsync instead.
            try
            {
                return ExecuteAsync<T>(query, variables)
                       .ConfigureAwait(false)
                       .GetAwaiter()
                       .GetResult();
            }
            catch (AggregateException aex) when (aex.InnerException != null)
            {
                throw aex.InnerException;
            }
        }

        public async Task<OpusOperationResult<T>> TryExecuteAsync<T>(string query, object variables = null)
        {
            try
            {
                T data = await ExecuteAsync<T>(query, variables).ConfigureAwait(false);
                return OpusOperationResult<T>.SuccessWithData(data);
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error("[OpusGraphQLClient.TryExecuteAsync] Failed: " + ex.ToString());
                return OpusOperationResult<T>.FailureWithData(DefaultFriendlyErrorMessage, ex.Message);
            }
        }

        public OpusOperationResult<T> TryExecute<T>(string query, object variables = null)
        {
            try
            {
                T data = ExecuteAsync<T>(query, variables)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
                return OpusOperationResult<T>.SuccessWithData(data);
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error("[OpusGraphQLClient.TryExecute] Failed: " + ex.ToString());
                return OpusOperationResult<T>.FailureWithData(DefaultFriendlyErrorMessage, ex.Message);
            }
        }
    }
}
