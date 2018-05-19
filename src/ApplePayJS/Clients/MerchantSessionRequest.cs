using Newtonsoft.Json;

namespace JustEat.ApplePayJS.Clients
{
    public class MerchantSessionRequest
    {
        [JsonProperty("merchantIdentifier")]
        public string MerchantIdentifier { get; set; }

        [JsonProperty("domainName")]
        public string DomainName { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}
