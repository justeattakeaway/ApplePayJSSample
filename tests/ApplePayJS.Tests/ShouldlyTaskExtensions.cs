// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Shouldly;

namespace ApplePayJS.Tests;

internal static class TaskExtensions
{
    public static async Task ShouldBe(this Task<string> task, string expected)
    {
        string actual = await task;
        actual.ShouldBe(expected);
    }

    public static async Task ShouldContain(this Task<string> task, string expected)
    {
        string actual = await task;
        actual.ShouldContain(expected);
    }
}
