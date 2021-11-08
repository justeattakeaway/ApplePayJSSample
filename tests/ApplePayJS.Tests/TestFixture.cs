// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using JustEat.ApplePayJS;
using JustEat.HttpClientInterception;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ApplePayJS.Tests;

public class TestFixture : WebApplicationFactory<Program>, ITestOutputHelperAccessor
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

    public Uri ServerAddress
    {
        get
        {
            EnsureServer();
            return ClientOptions.BaseAddress;
        }
    }

    public override IServiceProvider Services
    {
        get
        {
            EnsureServer();
            return _host!.Services!;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(
            (services) => services.AddSingleton<IHttpMessageHandlerBuilderFilter, HttpRequestInterceptionFilter>(
                (_) => new HttpRequestInterceptionFilter(Interceptor)));

        builder.ConfigureAppConfiguration(ConfigureTests)
               .ConfigureLogging((loggingBuilder) => loggingBuilder.ClearProviders().AddXUnit(this).AddDebug());

        builder.ConfigureKestrel(
            (kestrelOptions) => kestrelOptions.ConfigureHttpsDefaults(
                (connectionOptions) => connectionOptions.ServerCertificate = new X509Certificate2("localhost-dev.pfx", "Pa55w0rd!")));

        // Configure the server address for the server to listen on for HTTP requests
        builder.UseUrls("https://127.0.0.1:0");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());

        _host = builder.Build();
        _host.Start();

        var server = _host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();

        ClientOptions.BaseAddress = addresses!.Addresses
            .Select((p) => new Uri(p))
            .Last();

        testHost.Start();

        return testHost;
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

    private void EnsureServer()
    {
        if (_host is null)
        {
            // Force creation of the Kestrel server
            using (CreateDefaultClient())
            {
            }
        }
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
