// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace JustEat.ApplePayJS.Models;

using System.ComponentModel.DataAnnotations;

public class ValidateMerchantSessionModel
{
    [DataType(DataType.Url)]
    [Required]
    public string? ValidationUrl { get; set; }
}
