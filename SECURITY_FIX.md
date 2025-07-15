# Security Fix: SSRF Vulnerability in Apple Pay Merchant Validation

## Overview

This commit addresses a critical **Server-Side Request Forgery (SSRF)** vulnerability in the Apple Pay merchant validation endpoint that could allow attackers to make HTTP requests to arbitrary internal and external systems.

## Vulnerability Details

**CVE ID:** Pending  
**CVSS Score:** 9.1 (Critical)  
**Affected Component:** `/src/ApplePayJS/Controllers/HomeController.cs` - `Validate` method  
**Root Cause:** Missing domain validation for Apple Pay merchant validation URLs

### Before (Vulnerable Code)

The original code only validated URL format but did not verify that the URL belonged to legitimate Apple Pay domains:

```csharp
if (!ModelState.IsValid ||
    string.IsNullOrWhiteSpace(model?.ValidationUrl) ||
    !Uri.TryCreate(model.ValidationUrl, UriKind.Absolute, out Uri? requestUri))
{
    return BadRequest();
}

// VULNERABLE: Makes request to ANY URL
JsonDocument merchantSession = await client.GetMerchantSessionAsync(requestUri, request, cancellationToken);
```

### Attack Scenarios

1. **Cloud Metadata Access:** `http://169.254.169.254/latest/meta-data/iam/security-credentials/`
2. **Internal Network Scan:** `http://10.0.0.1:8080/admin`
3. **Database Access:** `http://internal-db:5432/`
4. **File System Access:** `file:///etc/passwd`

## Security Fix Implementation

### 1. Domain Whitelist

Added a comprehensive whitelist of legitimate Apple Pay merchant validation domains:

```csharp
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
```

### 2. Comprehensive URL Validation

Implemented `IsValidApplePayDomain()` method with multiple security checks:

- ✅ **HTTPS Only:** Rejects non-HTTPS URLs
- ✅ **Port Validation:** Only allows standard HTTPS port (443)
- ✅ **Domain Whitelist:** Validates against authorized Apple domains
- ✅ **Path Validation:** Ensures path starts with `/paymentservices/`
- ✅ **Query/Fragment Check:** Rejects URLs with query parameters or fragments

### 3. Enhanced Input Validation

Updated `ValidateMerchantSessionModel` with custom validation attribute:

```csharp
[DataType(DataType.Url)]
[Required(ErrorMessage = "Validation URL is required")]
[ApplePayDomain(ErrorMessage = "Only Apple Pay merchant validation domains are allowed")]
public string? ValidationUrl { get; set; }
```

### 4. Security Logging

Added comprehensive logging for security monitoring:

```csharp
logger?.LogWarning("SECURITY: Invalid merchant validation URL attempted: {ValidationUrl} from IP: {ClientIP}", 
    model?.ValidationUrl, HttpContext.Connection.RemoteIpAddress);
```

### 5. Improved Error Handling

Enhanced error responses with proper status codes and secure error messages:

```csharp
return BadRequest(new { error = "Invalid validation URL. Only Apple Pay merchant validation domains are allowed." });
```

## Files Modified

1. **`src/ApplePayJS/Controllers/HomeController.cs`**
   - Added domain whitelist validation
   - Implemented `IsValidApplePayDomain()` security method
   - Enhanced error handling and logging
   - Added comprehensive security documentation

2. **`src/ApplePayJS/Models/ValidateMerchantSessionModel.cs`**
   - Added `ApplePayDomainAttribute` custom validation
   - Enhanced input validation with proper error messages

## Testing

### Positive Tests (Should Pass)
```bash
# Legitimate Apple Pay domains
curl -X POST https://localhost:5001/applepay/validate \
  -H "Content-Type: application/json" \
  -d '{"validationUrl": "https://apple-pay-gateway.apple.com/paymentservices/startSession"}'
```

### Negative Tests (Should Be Blocked)
```bash
# SSRF attempt - AWS metadata
curl -X POST https://localhost:5001/applepay/validate \
  -H "Content-Type: application/json" \
  -d '{"validationUrl": "http://169.254.169.254/latest/meta-data/"}'

# SSRF attempt - Internal network
curl -X POST https://localhost:5001/applepay/validate \
  -H "Content-Type: application/json" \
  -d '{"validationUrl": "http://10.0.0.1:8080/admin"}'
```

## Security Impact

✅ **SSRF Prevention:** Blocks all unauthorized external and internal requests  
✅ **Cloud Security:** Prevents access to cloud metadata services  
✅ **Network Protection:** Stops internal network reconnaissance  
✅ **Data Protection:** Prevents access to internal databases and APIs  
✅ **Compliance:** Helps maintain PCI DSS and SOX compliance  

## References

- [Apple Pay JS Server Requirements](https://developer.apple.com/documentation/applepayjs/setting_up_server_requirements)
- [OWASP SSRF Prevention](https://cheatsheetseries.owasp.org/cheatsheets/Server_Side_Request_Forgery_Prevention_Cheat_Sheet.html)
- [CWE-918: Server-Side Request Forgery](https://cwe.mitre.org/data/definitions/918.html)

## Credit

Security vulnerability discovered and fixed by Muhammad Waseem (@MuhammadWaseem29)

---

**⚠️ IMPORTANT:** This fix addresses a critical security vulnerability. Deploy immediately to production environments.
