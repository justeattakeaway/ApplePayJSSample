// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace ApplePayJS.Tests;

public static class MerchantCertificateGenerator
{
    [Fact(Skip = "Enable this test to generate a new dummy Apple Pay merchant certificate to use for the tests.")]
    [SupportedOSPlatform("windows")]
    public static async Task Generate_Fake_Apple_Pay_Merchant_Certificate()
    {
        // Arrange
        string certificateName = "applepay.local";
        string certificatePassword = "Pa55w0rd!";
        string certificateFileName = "CHANGE_ME";

        var builder = new SubjectAlternativeNameBuilder();
        builder.AddIpAddress(IPAddress.Loopback);
        builder.AddIpAddress(IPAddress.IPv6Loopback);
        builder.AddDnsName("localhost");

        var distinguishedName = new X500DistinguishedName($"CN={certificateName}");

        using var key = RSA.Create(2048);
        var request = new CertificateRequest(distinguishedName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyAgreement | X509KeyUsageFlags.KeyEncipherment, false));
        request.CertificateExtensions.Add(new X509Extension("1.2.840.113635.100.6.32", Guid.NewGuid().ToByteArray(), false));
        request.CertificateExtensions.Add(builder.Build());

        var utcNow = DateTimeOffset.UtcNow;

        X509Certificate2 certificate = request.CreateSelfSigned(utcNow.AddDays(-1), utcNow.AddDays(3650));
        certificate.FriendlyName = certificateName;

        byte[] pfxBytes = certificate.Export(X509ContentType.Pfx, certificatePassword);

        File.Delete(certificateFileName);
        await File.WriteAllBytesAsync(certificateFileName, pfxBytes);
    }
}
