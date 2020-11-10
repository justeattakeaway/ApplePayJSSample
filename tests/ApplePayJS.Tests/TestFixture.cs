// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using JustEat.ApplePayJS;
using JustEat.HttpClientInterception;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ApplePayJS.Tests
{
    public class TestFixture : WebApplicationFactory<Startup>, ITestOutputHelperAccessor
    {
        private IHost? _host;
        private bool _disposed;

        public TestFixture()
            : base()
        {
            ClientOptions.AllowAutoRedirect = false;
            ClientOptions.BaseAddress = new Uri("https://localhost");
            Interceptor = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();
        }

        public HttpClientInterceptorOptions Interceptor { get; }

        public ITestOutputHelper? OutputHelper { get; set; }

        public Uri ServerAddress => ClientOptions.BaseAddress;

        public async Task StartServerAsync()
        {
            if (_host == null)
            {
                await CreateHttpServer();
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(
                (services) => services.AddSingleton<IHttpMessageHandlerBuilderFilter, HttpRequestInterceptionFilter>(
                    (_) => new HttpRequestInterceptionFilter(Interceptor)));

            builder.ConfigureAppConfiguration(ConfigureTests)
                   .ConfigureLogging((loggingBuilder) => loggingBuilder.ClearProviders().AddXUnit(this).AddDebug())
                   .UseContentRoot(GetContentRootPath());

            builder.ConfigureKestrel(
                (kestrelOptions) => kestrelOptions.ConfigureHttpsDefaults(
                    (connectionOptions) => connectionOptions.ServerCertificate = new X509Certificate2("localhost-dev.pfx", "Pa55w0rd!")));

            builder.UseUrls(ServerAddress.ToString());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    _host?.Dispose();
                }

                _disposed = true;
            }
        }

        private static void ConfigureTests(IConfigurationBuilder builder)
        {
            string? directory = Path.GetDirectoryName(typeof(TestFixture).Assembly.Location);
            string fullPath = Path.Combine(directory ?? ".", "testsettings.json");

            builder.AddJsonFile(fullPath);
        }

        private static Uri FindFreeServerAddress()
        {
            int port = GetFreePortNumber();

            return new UriBuilder()
            {
                Scheme = "https",
                Host = "localhost",
                Port = port,
            }.Uri;
        }

        private static int GetFreePortNumber()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        private async Task CreateHttpServer()
        {
            // Configure the server address for the server to listen on for HTTP requests
            ClientOptions.BaseAddress = FindFreeServerAddress();

            var builder = CreateHostBuilder().ConfigureWebHost(ConfigureWebHost);

            _host = builder.Build();

            // Force creation of the Kestrel server and start it
            var hostedService = _host.Services.GetRequiredService<IHostedService>();
            await hostedService.StartAsync(default);
        }

        private string GetContentRootPath()
        {
            var attribute = GetTestAssemblies()
                .SelectMany((p) => p.GetCustomAttributes<WebApplicationFactoryContentRootAttribute>())
                .Where((p) => string.Equals(p.Key, "JustEat.ApplePayJS", StringComparison.OrdinalIgnoreCase))
                .OrderBy((p) => p.Priority)
                .First();

            return attribute.ContentRootPath;
        }

        private sealed class HttpRequestInterceptionFilter : IHttpMessageHandlerBuilderFilter
        {
            internal HttpRequestInterceptionFilter(HttpClientInterceptorOptions options)
            {
                Options = options;
            }

            private HttpClientInterceptorOptions Options { get; }

            public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
            {
                return (builder) =>
                {
                    next(builder);
                    builder.AdditionalHandlers.Add(Options.CreateHttpMessageHandler());
                };
            }
        }
    }
}
