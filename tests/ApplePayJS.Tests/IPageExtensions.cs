// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.Playwright;

namespace ApplePayJS.Tests;

internal static class IPageExtensions
{
    public static async Task ClearTextAsync(this IPage page, string selector)
    {
        await page.FocusAsync(selector);
        await page.Keyboard.PressAsync(OperatingSystem.IsMacOS() ? "Meta+A" : "Control+A");
        await page.Keyboard.PressAsync("Delete");
    }
}
