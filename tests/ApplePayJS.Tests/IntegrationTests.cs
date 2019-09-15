// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using JustEat.HttpClientInterception;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace ApplePayJS.Tests
{
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

        public async Task InitializeAsync()
        {
            await Fixture.StartServerAsync();
        }

        public Task DisposeAsync()
        {
            if (Fixture != null)
            {
                Fixture.Dispose();
            }

            return Task.CompletedTask;
        }

        [Fact]
        public void Can_Pay_With_Apple_Pay()
        {
            // Arrange
            var builder = new HttpRequestInterceptionBuilder()
                .Requests()
                .ForPost()
                .ForUrl("https://apple-pay-gateway-cert.apple.com/paymentservices/startSession")
                .Responds()
                .WithJsonContent(new { })
                .RegisterWith(Fixture.Interceptor);

            using var driver = CreateWebDriver();
            driver.Navigate().GoToUrl(Fixture.ServerAddress);

            // Act
            driver.FindElement(By.Id("amount")).Clear();
            driver.FindElement(By.Id("amount")).SendKeys("1.23");
            driver.FindElement(By.Id("apple-pay-button")).Click();

            // Assert
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
            wait.Until((p) => p.FindElement(By.ClassName("card-name")).Displayed);

            driver.FindElement(By.ClassName("card-name")).Text.ShouldBe("American Express");
            driver.FindElement(By.Id("billing-contact")).FindElement(By.ClassName("contact-name")).Text.ShouldBe("John Smith");
            driver.FindElement(By.Id("shipping-contact")).FindElement(By.ClassName("contact-name")).Text.ShouldBe("John Smith");
        }

        private static IWebDriver CreateWebDriver()
        {
            string chromeDriverDirectory = Path.GetDirectoryName(typeof(IntegrationTests).Assembly.Location) ?? ".";

            var options = new ChromeOptions()
            {
                AcceptInsecureCertificates = true,
            };

            if (!System.Diagnostics.Debugger.IsAttached)
            {
                options.AddArgument("--headless");
            }

            return new ChromeDriver(chromeDriverDirectory, options);
        }
    }
}
