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

        public virtual async Task<T> ExecuteAsync<T>(string query, object variables = null)
        {
            Engine.Instance.Log.Info($"[OpusGraphQLClient] ExecuteAsync started for query type: {typeof(T).Name}");

            try
            {
                string token = await _opusTokenProvider.GetAccessTokenAsync();

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
                        content);

                    if (!response.IsSuccessStatusCode)
                    {
                        Engine.Instance.Log.Error($"[OpusGraphQLClient] GraphQL request failed with status {response.StatusCode}");
                        throw new HttpRequestException(
                            string.Format("GraphQL request failed with status {0}", response.StatusCode));
                    }

                    string responseJson = await response.Content.ReadAsStringAsync();
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
                });

                return result;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[OpusGraphQLClient] ExecuteAsync failed: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

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
    }
}
