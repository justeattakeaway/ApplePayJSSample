using System.Text.Json.Serialization;

namespace JustEat.ApplePayJS.Clients
{
    public class MerchantSessionRequest
    {
        [JsonPropertyName("merchantIdentifier")]
        public string? MerchantIdentifier { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("initiative")]
        public string? Initiative { get; set; }

        [JsonPropertyName("initiativeContext")]
        public string? InitiativeContext { get; set; }
    }
}
