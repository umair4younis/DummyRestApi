using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Puma.MDE.OPUS.Models;


namespace Puma.MDE.OPUS
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PatchAsync(
            this HttpClient client,
            string requestUri,
            object contentObject,
            string mediaType = "application/json")
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrWhiteSpace(requestUri))
                throw new ArgumentNullException(nameof(requestUri));

            // Create Uri
            Uri uri;
            try
            {
                if (client.BaseAddress != null)
                {
                    uri = new Uri(client.BaseAddress, requestUri.TrimStart('/'));
                }
                else
                {
                    uri = new Uri(requestUri);
                }
            }
            catch (UriFormatException ex)
            {
                throw new ArgumentException($"Invalid URI: {requestUri}", nameof(requestUri), ex);
            }

            // Serialize content
            string json = JsonConvert.SerializeObject(contentObject);

            // FIXED: Classic using statement + Encoding.UTF8 (not Encoding.UTF8)
            using (var content = new StringContent(json, Encoding.UTF8, mediaType))
            {
                // Create PATCH request
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), uri);
                request.Content = content;

                // Send request
                return await client.SendAsync(request).ConfigureAwait(false);
            }
        }

        public static async Task<OpusOperationResult<HttpResponseMessage>> TryPatchAsync(
            this HttpClient client,
            string requestUri,
            object contentObject,
            string mediaType = "application/json")
        {
            try
            {
                HttpResponseMessage response = await PatchAsync(client, requestUri, contentObject, mediaType).ConfigureAwait(false);
                return OpusOperationResult<HttpResponseMessage>.SuccessWithData(response);
            }
            catch (Exception ex)
            {
                Puma.MDE.Engine.Instance.Log.Error("[HttpClientExtensions.TryPatchAsync] Failed: " + ex.ToString());
                return OpusOperationResult<HttpResponseMessage>.FailureWithData("Unable to send PATCH request.", ex.Message);
            }
        }
    }
}