using System;


namespace Puma.MDE.OPUS
{
    /// <summary>
    /// Configuration for OPUS API endpoints.
    /// Supports both:
    ///   - https://structured-uat.opus-online.com/opus/api/v3/masterdata/...
    ///   - https://structured-uat.opus-online.com/unicredit-swap-service/api/...
    /// </summary>
    public class OpusConfiguration
    {
        public string TokenUrl { get; set; }

        private string _baseUrl = string.Empty;
        public string BaseUrl
        {
            get => _baseUrl;
            set
            {
                _baseUrl = value?.TrimEnd('/') ?? string.Empty;
                Engine.Instance.Log.Info($"[OpusConfiguration] BaseUrl updated to: {_baseUrl}");
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Main REST Base for standard masterdata endpoints
        // ──────────────────────────────────────────────────────────────
        private string _restSuffix = "v3/masterdata/";
        public string RestUrl
        {
            get => CombineUrls(BaseUrl, _restSuffix);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _restSuffix = string.Empty;
                    Engine.Instance.Log.Debug($"[OpusConfiguration] RestUrl cleared");
                    return;
                }

                if (value.Contains("://"))
                {
                    _restSuffix = value;  // full URL override
                    Engine.Instance.Log.Info($"[OpusConfiguration] RestUrl set to full URL override: {value}");
                }
                else
                {
                    _restSuffix = value.TrimStart('/').TrimEnd('/') + "/";
                    Engine.Instance.Log.Debug($"[OpusConfiguration] RestUrl suffix updated: {_restSuffix}");
                }
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Unicredit Swap Service Base (new endpoint family)
        // ──────────────────────────────────────────────────────────────
        private string _unicreditSwapServiceSuffix = "unicredit-swap-service/api/";
        public string UnicreditSwapServiceUrl
        {
            get => CombineUrls(BaseUrl, _unicreditSwapServiceSuffix);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _unicreditSwapServiceSuffix = string.Empty;
                    Engine.Instance.Log.Debug($"[OpusConfiguration] UnicreditSwapServiceUrl cleared");
                    return;
                }

                if (value.Contains("://"))
                {
                    _unicreditSwapServiceSuffix = value;
                    Engine.Instance.Log.Info($"[OpusConfiguration] UnicreditSwapServiceUrl set to full URL: {value}");
                }
                else
                {
                    _unicreditSwapServiceSuffix = value.TrimStart('/').TrimEnd('/') + "/";
                    Engine.Instance.Log.Debug($"[OpusConfiguration] UnicreditSwapServiceUrl suffix updated: {_unicreditSwapServiceSuffix}");
                }
            }
        }

        private string _graphQlSuffix = string.Empty;
        public string GraphQlUrl
        {
            get => CombineUrls(BaseUrl, _graphQlSuffix);

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _graphQlSuffix = string.Empty;
                    Engine.Instance.Log.Debug($"[OpusConfiguration] GraphQlUrl cleared");
                    return;
                }

                if (!string.IsNullOrEmpty(BaseUrl) &&
                    value.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    _graphQlSuffix = value.Substring(BaseUrl.Length).TrimStart('/');
                    Engine.Instance.Log.Debug($"[OpusConfiguration] GraphQlUrl extracted suffix: {_graphQlSuffix}");
                }
                else if (!value.Contains("://"))
                {
                    _graphQlSuffix = value.TrimStart('/').TrimEnd('/');
                    Engine.Instance.Log.Debug($"[OpusConfiguration] GraphQlUrl set as relative: {_graphQlSuffix}");
                }
                else
                {
                    _graphQlSuffix = value;
                    Engine.Instance.Log.Info($"[OpusConfiguration] GraphQlUrl set to full URL: {value}");
                }
            }
        }

        public string ProxyUrl { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string ClientCertPath { get; set; }

        public string ClientCertPassword { get; set; }

        public string GraphQlQuery { get; set; }

        // Helper to combine URLs cleanly
        private static string CombineUrls(string baseUrl, string suffix)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return suffix?.TrimStart('/') ?? string.Empty;

            if (string.IsNullOrWhiteSpace(suffix))
                return baseUrl.TrimEnd('/');

            if (suffix.Contains("://"))
                return suffix.TrimEnd('/');

            return $"{baseUrl.TrimEnd('/')}/{suffix.TrimStart('/')}";
        }

        /// <summary>
        /// Returns the correct base URL depending on the endpoint type.
        /// Use this in your ApiClient when building full URLs.
        /// </summary>
        public string GetBaseUrlForEndpoint(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return RestUrl;

            // Unicredit Swap Service endpoints
            if (endpoint.Contains("unicredit-swap-service"))
                return BaseUrl;

            // Default to standard masterdata REST
            return RestUrl;
        }
    }
}