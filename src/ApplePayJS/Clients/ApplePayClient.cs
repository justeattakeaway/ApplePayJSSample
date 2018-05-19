using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace JustEat.ApplePayJS.Clients
{
    public class ApplePayClient
    {
        private readonly HttpClient _httpClient;

        public ApplePayClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<JObject> GetMerchantSessionAsync(
            Uri requestUri,
            MerchantSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            // POST the data to create a valid Apple Pay merchant session.
            using (var response = await _httpClient.PostAsJsonAsync(requestUri, request, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                // Read the opaque merchant session JSON from the response body.
                return await response.Content.ReadAsAsync<JObject>(cancellationToken);
            }
        }
    }
}
