// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace JustEat.ApplePayJS.Clients;

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
