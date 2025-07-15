// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace JustEat.ApplePayJS.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Custom validation attribute to ensure URLs are valid Apple Pay merchant validation domains
/// This prevents Server-Side Request Forgery (SSRF) attacks
/// </summary>
public class ApplePayDomainAttribute : ValidationAttribute
{
    private static readonly HashSet<string> AllowedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "apple-pay-gateway.apple.com",
        "apple-pay-gateway-nc-pod1.apple.com",
        "apple-pay-gateway-nc-pod2.apple.com",
        "apple-pay-gateway-nc-pod3.apple.com", 
        "apple-pay-gateway-nc-pod4.apple.com",
        "apple-pay-gateway-nc-pod5.apple.com",
        "apple-pay-gateway-pr-pod1.apple.com",
        "apple-pay-gateway-pr-pod2.apple.com",
        "apple-pay-gateway-pr-pod3.apple.com",
        "apple-pay-gateway-pr-pod4.apple.com",
        "apple-pay-gateway-pr-pod5.apple.com",
        "apple-pay-gateway-cert.apple.com"
    };

    public override bool IsValid(object? value)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        return uri.Scheme == "https" && 
               AllowedDomains.Contains(uri.Host) &&
               uri.AbsolutePath.StartsWith("/paymentservices/", StringComparison.OrdinalIgnoreCase);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must be a valid Apple Pay merchant validation URL.";
    }
}

public class ValidateMerchantSessionModel
{
    [DataType(DataType.Url)]
    [Required(ErrorMessage = "Validation URL is required")]
    [ApplePayDomain(ErrorMessage = "Only Apple Pay merchant validation domains are allowed")]
    public string? ValidationUrl { get; set; }
}
