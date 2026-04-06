using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Example.OPUS.Models;

namespace Example.OPUS
{
    public class OpusTokenProvider
    {
        private readonly OpusConfiguration _opusConfiguration;
        private static readonly HttpClient _http = new HttpClient();
        private DateTime _tokenExpiry;
        private string _token;

        public OpusTokenProvider(OpusConfiguration opusConfiguration)
        {
            _opusConfiguration = opusConfiguration;
        }

        public virtual async Task<string> GetAccessTokenAsync()
        {
            // return cached token if valid
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
            {
                return _token;
            }

            var body = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _opusConfiguration.ClientId },
                { "client_secret", _opusConfiguration.ClientSecret }
            };

            var content = new FormUrlEncodedContent(body);
            var response = await _http.PostAsync(_opusConfiguration.TokenUrl, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<OpusTokenResponse>(json);

            _token = tokenResponse.access_token;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 60);

            return _token;
        }

        public string GetAccessToken()
        {
            return GetAccessTokenAsync().GetAwaiter().GetResult();
        }
    }
}
