// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace JustEat.ApplePayJS.Extensions;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// A class containing extension methods for the <see cref=""/> class. This class cannot be inherited.
/// </summary>
public static class IUrlHelperExtensions
{
    /// <summary>
    /// Converts a virtual (relative) path to an application absolute URI.
    /// </summary>
    /// <param name="value">The <see cref="IUrlHelper"/>.</param>
    /// <param name="contentPath">The virtual path of the content.</param>
    /// <returns>The application absolute URI.</returns>
    public static string AbsoluteContent(this IUrlHelper value, string contentPath)
    {
        var request = value.ActionContext.HttpContext.Request;
        return new Uri(new Uri(request.Scheme + "://" + request.Host.Value), value.Content(contentPath)).ToString();
    }
}
