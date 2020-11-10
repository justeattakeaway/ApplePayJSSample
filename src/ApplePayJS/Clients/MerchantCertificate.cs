// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using JustEat.ApplePayJS.Models;
using Microsoft.Extensions.Options;

namespace JustEat.ApplePayJS.Clients
{
    public class MerchantCertificate
    {
        private readonly ApplePayOptions _options;

        public MerchantCertificate(IOptions<ApplePayOptions> options)
        {
            _options = options.Value;
        }

        public X509Certificate2 GetCertificate()
        {
            // Get the merchant certificate for two-way TLS authentication with the Apple Pay server.
            if (_options.UseCertificateStore)
            {
                return LoadCertificateFromStore();
            }
            else
            {
                return LoadCertificateFromDisk();
            }
        }

        public string GetMerchantIdentifier()
        {
            try
            {
                using var merchantCertificate = GetCertificate();
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

        private X509Certificate2 LoadCertificateFromDisk()
        {
            try
            {
                return new X509Certificate2(
                    _options.MerchantCertificateFileName ?? string.Empty,
                    _options.MerchantCertificatePassword);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load Apple Pay merchant certificate file from '{_options.MerchantCertificateFileName}'.", ex);
            }
        }

        private X509Certificate2 LoadCertificateFromStore()
        {
            // Load the certificate from the current user's certificate store. This
            // is useful if you do not want to publish the merchant certificate with
            // your application, but it is also required to be able to use an X.509
            // certificate with a private key if the user profile is not available,
            // such as when using IIS hosting in an environment such as Microsoft Azure.
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                _options.MerchantCertificateThumbprint?.Trim() ?? string.Empty,
                validOnly: false);

            if (certificates.Count < 1)
            {
                throw new InvalidOperationException(
                    $"Could not find Apple Pay merchant certificate with thumbprint '{_options.MerchantCertificateThumbprint}' from store '{store.Name}' in location '{store.Location}'.");
            }

            return certificates[0];
        }
    }
}
