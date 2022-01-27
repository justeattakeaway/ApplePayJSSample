// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using JustEat.HttpClientInterception;
using Microsoft.Playwright;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace ApplePayJS.Tests;

public class IntegrationTests : IAsyncLifetime
{
    public IntegrationTests(ITestOutputHelper outputHelper)
    {
        Fixture = new TestFixture()
        {
            OutputHelper = outputHelper,
        };
    }

    private TestFixture Fixture { get; }

    public Task InitializeAsync()
    {
        InstallPlaywright();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (Fixture != null)
        {
            await Fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Can_Pay_With_Apple_Pay()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .Requests()
            .ForPost()
            .ForUrl("https://apple-pay-gateway-cert.apple.com/paymentservices/startSession")
            .Responds()
            .WithJsonContent(new { })
            .RegisterWith(Fixture.Interceptor);

        var fixture = new BrowserFixture(Fixture.OutputHelper);

        await fixture.WithPageAsync(async (page) =>
        {
            await page.GotoAsync(Fixture.ServerAddress.ToString());
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                // Act
                await page.ClearTextAsync(Selectors.Amount);
            await page.TypeAsync(Selectors.Amount, "1.23");

            await page.ClickAsync(Selectors.Pay);

                // Assert
                await page.WaitForSelectorAsync(Selectors.CardName);
            await page.InnerTextAsync(Selectors.CardName).ShouldBe("American Express");

            foreach (string selector in new[] { Selectors.BillingContact, Selectors.ShipingContact })
            {
                var contact = await page.QuerySelectorAsync(selector);
                contact.ShouldNotBeNull();

                var contactName = await contact.QuerySelectorAsync(Selectors.ContactName);
                contactName.ShouldNotBeNull();

                await contactName.InnerTextAsync().ShouldContain("John Smith");
            }
        });
    }

    private static void InstallPlaywright()
    {
        int exitCode = Program.Main(new[] { "install" });

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright exited with code {exitCode}");
        }
    }

    private static class Selectors
    {
        internal const string Amount = "id=amount";
        internal const string BillingContact = "id=billing-contact";
        internal const string CardName = ".card-name";
        internal const string ContactName = ".contact-name";
        internal const string Pay = "id=apple-pay-button";
        internal const string ShipingContact = "id=shipping-contact";
    }
}
