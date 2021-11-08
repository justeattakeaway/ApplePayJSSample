// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using JustEat.ApplePayJS.Clients;
using JustEat.ApplePayJS.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions();
builder.Services.Configure<ApplePayOptions>(builder.Configuration.GetSection("ApplePay"));

builder.Services.AddAntiforgery((options) =>
{
    options.Cookie.Name = "antiforgerytoken";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "x-antiforgery-token";
});

builder.Services.AddMvc((options) =>
{
            // Apple Pay JS requires pages to be served over HTTPS
    if (builder.Environment.IsProduction())
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        options.Filters.Add(new RequireHttpsAttribute());
    }
});

// Register class for managing the application's use of the Apple Pay merchant certificate
builder.Services.AddSingleton<MerchantCertificate>();

// Create a typed HTTP client with the merchant certificate for two-way TLS authentication over HTTPS.
builder.Services.AddHttpClient<ApplePayClient>("ApplePay")
                .ConfigurePrimaryHttpMessageHandler((serviceProvider) =>
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error")
       .UseStatusCodePages();
}

app.UseHsts()
   .UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions()
{
    ServeUnknownFileTypes = true, // Required to serve the files in the .well-known folder
});

app.UseRouting();
app.MapDefaultControllerRoute();

app.Run();

namespace JustEat.ApplePayJS
{
    public partial class Program
    {
    }
}
