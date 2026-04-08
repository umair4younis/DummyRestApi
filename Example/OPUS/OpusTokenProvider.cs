using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Puma.MDE.OPUS.Models;

namespace Puma.MDE.OPUS
{
    public class OpusTokenProvider
    {
        private readonly OpusConfiguration _opusConfiguration;
        private static readonly HttpClient _http = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        })
        {
            Timeout = TimeSpan.FromSeconds(30)  // 30-second timeout to prevent indefinite hangs
        };
        private DateTime _tokenExpiry;
        private string _token;

        public OpusTokenProvider(OpusConfiguration opusConfiguration)
        {
            _opusConfiguration = opusConfiguration;
            Engine.Instance.Log.Info($"[OpusTokenProvider] Initialized with TokenUrl: {opusConfiguration?.TokenUrl ?? "NULL"}");
        }

        public virtual async Task<string> GetAccessTokenAsync()
        {
            // return cached token if valid
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
            {
                Engine.Instance.Log.Debug($"[OpusTokenProvider] Using cached token, valid until {_tokenExpiry:O}");
                return _token;
            }

            Engine.Instance.Log.Info($"[OpusTokenProvider] Requesting new access token from {_opusConfiguration.TokenUrl}");
            var body = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _opusConfiguration.ClientId },
                { "client_secret", "***" }  // Don't log secret
            };

            try
            {
                var content = new FormUrlEncodedContent(body);
                Engine.Instance.Log.Debug($"[OpusTokenProvider] POST to {_opusConfiguration.TokenUrl}");
                var response = await _http.PostAsync(_opusConfiguration.TokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Engine.Instance.Log.Error($"[OpusTokenProvider] Token request failed: {response.StatusCode} - {errorBody}");
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<OpusTokenResponse>(json);

                _token = tokenResponse.access_token;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 60);

                Engine.Instance.Log.Info($"[OpusTokenProvider] Access token acquired successfully, expires at {_tokenExpiry:O}");
                return _token;
            }
            catch (HttpRequestException ex)
            {
                Engine.Instance.Log.Error($"[OpusTokenProvider] HTTP error acquiring token: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[OpusTokenProvider] Unexpected error acquiring token: {ex.Message}");
                throw;
            }
        }

        public string GetAccessToken()
        {
            // IMPORTANT: Calling sync-wrapped async methods can cause deadlocks in .NET 4.8
            // For console apps and non-UI contexts, ConfigureAwait(false) helps, but this is still problematic.
            // The better approach is to refactor callers to be async.
            // This is left as a backward-compatibility bridge only.
            try
            {
                Engine.Instance.Log.Debug($"[OpusTokenProvider] GetAccessToken (sync) called - using bridge to async");
                return GetAccessTokenAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException aex) when (aex.InnerException != null)
            {
                Engine.Instance.Log.Error($"[OpusTokenProvider] Sync bridge failed: {aex.InnerException.Message}");
                throw aex.InnerException;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[OpusTokenProvider] Unexpected error in sync bridge: {ex.Message}");
                throw;
            }
        }
    }
}
