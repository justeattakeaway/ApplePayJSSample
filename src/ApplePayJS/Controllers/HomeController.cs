// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace JustEat.ApplePayJS.Controllers
{
    using System;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class HomeController : Controller
    {
        private readonly ApplePayOptions _options;

        public HomeController(IOptions<ApplePayOptions> options)
        {
            _options = options.Value;
        }

        public IActionResult Index()
        {
            // Get the merchant identifier and store name for use in the JavaScript by ApplePaySession.
            ViewData["MerchantId"] = GetMerchantIdentifier();
            ViewData["StoreName"] = _options.StoreName;

            return View();
        }

        [HttpPost]
        [Produces("application/json")]
        [Route("applepay/validate", Name = "MerchantValidation")]
        public async Task<IActionResult> Validate([FromBody] ValidateMerchantSessionModel model)
        {
            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(model?.ValidationUrl) ||
                !Uri.TryCreate(model.ValidationUrl, UriKind.Absolute, out Uri requestUri))
            {
                return BadRequest();
            }

            // Load the merchant certificate for two-way TLS authentication with the Apple Pay server.
            var certificate = LoadMerchantCertificate();

            // Get the merchant identifier from the certificate to send in the validation payload.
            var merchantIdentifier = GetMerchantIdentifier(certificate);

            // Create the JSON payload to POST to the Apple Pay merchant validation URL.
            var payload = new
            {
                merchantIdentifier = merchantIdentifier,
                domainName = Request.GetTypedHeaders().Host.Value,
                displayName = _options.StoreName
            };

            JObject merchantSession;

            // Create an HTTP client with the merchant certificate
            // for two-way TLS authentication over HTTPS.
            using (var httpClient = CreateHttpClient(certificate))
            {
                var jsonPayload = JsonConvert.SerializeObject(payload);

                using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
                {
                    // POST the data to create a valid Apple Pay merchant session.
                    using (var response = await httpClient.PostAsync(requestUri, content))
                    {
                        response.EnsureSuccessStatusCode();

                        // Read the opaque merchant session JSON from the response body.
                        var merchantSessionJson = await response.Content.ReadAsStringAsync();
                        merchantSession = JObject.Parse(merchantSessionJson);
                    }
                }
            }

            // Return the merchant session as-is to the JavaScript as JSON.
            return Json(merchantSession);
        }

        public IActionResult Error()
        {
            return View();
        }

        private HttpClient CreateHttpClient(X509Certificate2 certificate)
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificate);

            return new HttpClient(handler, disposeHandler: true);
        }

        private X509Certificate2 LoadMerchantCertificate()
        {
            X509Certificate2 certificate;

            if (_options.UseCertificateStore)
            {
                // Load the certificate from the current user's certificate store. This
                // is useful if you do not want to publish the merchant certificate with
                // your application, but it is also required to be able to use an X.509
                // certificate with a private key if the user profile is not available,
                // such as when using IIS hosting in an environment such as Microsoft Azure.
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);

                    var certificates = store.Certificates.Find(
                        X509FindType.FindByThumbprint,
                        _options.MerchantCertificateThumbprint?.Trim(),
                        validOnly: false);

                    if (certificates.Count < 1)
                    {
                        throw new InvalidOperationException(
                            $"Could not find Apple Pay merchant certificate with thumbprint '{_options.MerchantCertificateThumbprint}' from store '{store.Name}' in location '{store.Location}'.");
                    }

                    certificate = certificates[0];
                }
            }
            else
            {
                try
                {
                    // Load the X.509 certificate from disk
                    certificate = new X509Certificate2(
                        _options.MerchantCertificateFileName,
                        _options.MerchantCertificatePassword);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to load Apple Pay merchant certificate file from '{_options.MerchantCertificateFileName}'.", ex);
                }
            }

            return certificate;
        }

        private string GetMerchantIdentifier()
        {
            try
            {
                var merchantCertificate = LoadMerchantCertificate();
                return GetMerchantIdentifier(merchantCertificate);
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }

        private string GetMerchantIdentifier(X509Certificate2 certificate)
        {
            // This OID returns the ASN.1 encoded merchant identifier
            var extension = certificate.Extensions["1.2.840.113635.100.6.32"];

            if (extension == null)
            {
                return string.Empty;
            }

            // Convert the raw ASN.1 data to a string containing the ID
            return Encoding.ASCII.GetString(extension.RawData).Substring(2);
        }
    }
}
