// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace JustEat.ApplePayJS.Controllers;

using System.Net.Mime;
using System.Text.Json;
using JustEat.ApplePayJS.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models;

public class HomeController(
    ApplePayClient client,
    MerchantCertificate certificate,
    IOptions<ApplePayOptions> options) : Controller
{
    // SECURITY FIX: Apple Pay authorized merchant validation domains
    private static readonly HashSet<string> AllowedApplePayDomains = new(StringComparer.OrdinalIgnoreCase)
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
        "apple-pay-gateway-cert.apple.com"  // Test environment
    };

    public IActionResult Index()
    {
        // Get the merchant identifier and store name for use in the JavaScript by ApplePaySession.
        var model = new HomeModel()
        {
            MerchantId = certificate.GetMerchantIdentifier(),
            StoreName = options.Value.StoreName,
        };

        return View(model);
    }

    [HttpPost]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("applepay/validate", Name = "MerchantValidation")]
    public async Task<IActionResult> Validate([FromBody] ValidateMerchantSessionModel model, CancellationToken cancellationToken = default)
    {
        // SECURITY FIX: Comprehensive validation including domain whitelist to prevent SSRF attacks
        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(model?.ValidationUrl) ||
            !Uri.TryCreate(model.ValidationUrl, UriKind.Absolute, out Uri? requestUri) ||
            !IsValidApplePayDomain(requestUri))
        {
            // Log security violation attempt for monitoring
            var logger = HttpContext.RequestServices.GetService<ILogger<HomeController>>();
            logger?.LogWarning("SECURITY: Invalid merchant validation URL attempted: {ValidationUrl} from IP: {ClientIP}", 
                model?.ValidationUrl, HttpContext.Connection.RemoteIpAddress);
            
            return BadRequest(new { error = "Invalid validation URL. Only Apple Pay merchant validation domains are allowed." });
        }

        // Create the JSON payload to POST to the Apple Pay merchant validation URL.
        var request = new MerchantSessionRequest()
        {
            DisplayName = options.Value.StoreName,
            Initiative = "web",
            InitiativeContext = Request.GetTypedHeaders().Host.Value,
            MerchantIdentifier = certificate.GetMerchantIdentifier(),
        };

        try
        {
            JsonDocument merchantSession = await client.GetMerchantSessionAsync(requestUri, request, cancellationToken);
            
            // Return the merchant session as-is to the JavaScript as JSON.
            return Json(merchantSession.RootElement);
        }
        catch (Exception ex)
        {
            var logger = HttpContext.RequestServices.GetService<ILogger<HomeController>>();
            logger?.LogError(ex, "Error during Apple Pay merchant validation for URL: {ValidationUrl}", requestUri);
            
            return StatusCode(500, new { error = "Merchant validation failed" });
        }
    }

    /// <summary>
    /// SECURITY METHOD: Validates that the URI is an authorized Apple Pay merchant validation domain
    /// This prevents Server-Side Request Forgery (SSRF) attacks by ensuring only legitimate Apple domains are accessed
    /// </summary>
    /// <param name="uri">The URI to validate</param>
    /// <returns>True if the URI is a valid Apple Pay domain, false otherwise</returns>
    private static bool IsValidApplePayDomain(Uri uri)
    {
        // Must use HTTPS protocol for security
        if (uri.Scheme != "https")
        {
            return false;
        }

        // Must be exactly 443 (default HTTPS port) or no port specified
        if (uri.Port != 443 && uri.Port != -1)
        {
            return false;
        }

        // Must be in our whitelist of allowed Apple Pay domains
        if (!AllowedApplePayDomains.Contains(uri.Host))
        {
            return false;
        }

        // Path must start with /paymentservices/ (Apple's endpoint structure)
        if (!uri.AbsolutePath.StartsWith("/paymentservices/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // No query parameters or fragments allowed for additional security
        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        return true;
    }

    public IActionResult Error() => View();
}
