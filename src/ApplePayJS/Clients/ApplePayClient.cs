using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JustEat.ApplePayJS.Clients
{
    public class ApplePayClient
    {
        private readonly HttpClient _httpClient;

        public ApplePayClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<JsonDocument> GetMerchantSessionAsync(
            Uri requestUri,
            MerchantSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            // POST the data to create a valid Apple Pay merchant session.
            string json = JsonSerializer.Serialize(request);

            using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            using var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);

            response.EnsureSuccessStatusCode();

            // Read the opaque merchant session JSON from the response body.
            using var stream = await response.Content.ReadAsStreamAsync();

            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
    }
}
