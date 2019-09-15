// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace JustEat.ApplePayJS
{
    using System.Net.Http;
    using JustEat.ApplePayJS.Clients;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Models;

    public class Startup
    {
        public Startup(IHostEnvironment environment, IConfiguration configuration)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<ApplePayOptions>(Configuration.GetSection("ApplePay"));

            services.AddAntiforgery((options) =>
            {
                options.Cookie.Name = "antiforgerytoken";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.HeaderName = "x-antiforgery-token";
            });

            services.AddMvc(
                (options) =>
                {
                    // Apple Pay JS requires pages to be served over HTTPS
                    if (Environment.IsProduction())
                    {
                        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                        options.Filters.Add(new RequireHttpsAttribute());
                    }
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            // Register class for managing the application's use of the Apple Pay merchant certificate
            services.AddSingleton<MerchantCertificate>();

            // Create a typed HTTP client with the merchant certificate for two-way TLS authentication over HTTPS.
            services
                .AddHttpClient<ApplePayClient>("ApplePay")
                .ConfigurePrimaryHttpMessageHandler(
                    (serviceProvider) =>
                    {
                        var merchantCertificate = serviceProvider.GetRequiredService<MerchantCertificate>();
                        var certificate = merchantCertificate.GetCertificate();

                        var handler = new HttpClientHandler();
                        handler.ClientCertificates.Add(certificate);

                        // Apple Pay JS requires the use of at least TLS 1.2 to generate a merchange session:
                        // https://developer.apple.com/documentation/applepayjs/setting_up_server_requirements
                        // If you run an older operating system that does not negotiate this by default, uncomment the line below.
                        // handler.SslProtocols = SslProtocols.Tls12;

                        return handler;
                    });
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error")
                   .UseStatusCodePages();
            }

            app.UseHsts()
               .UseHttpsRedirection();

            app.UseStaticFiles(
                new StaticFileOptions()
                {
                    ServeUnknownFileTypes = true, // Required to serve the files in the .well-known folder
                });

            app.UseRouting();
            app.UseEndpoints((endpoints) => endpoints.MapDefaultControllerRoute());
        }
    }
}
