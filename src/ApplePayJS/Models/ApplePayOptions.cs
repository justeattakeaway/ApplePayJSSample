// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace JustEat.ApplePayJS.Models;

public class ApplePayOptions
{
    public string? StoreName { get; set; }

    public bool UseCertificateStore { get; set; }

    public string? MerchantCertificate { get; set; }

    public string? MerchantCertificateFileName { get; set; }

    public string? MerchantCertificatePassword { get; set; }

    public string? MerchantCertificateThumbprint { get; set; }
}
