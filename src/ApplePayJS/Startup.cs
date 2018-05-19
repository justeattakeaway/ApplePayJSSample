// Copyright (c) Just Eat, 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace JustEat.ApplePayJS
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Models;

    public class Startup
    {
        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }

        public IHostingEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<ApplePayOptions>(Configuration.GetSection("ApplePay"));

            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "antiforgerytoken";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.HeaderName = "x-antiforgery-token";
            });

            services.AddMvc(
                options =>
                {
                    // Apple Pay JS requires pages to be served over HTTPS
                    if (Environment.IsProduction())
                    {
                        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                        options.Filters.Add(new RequireHttpsAttribute());
                    }
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSingleton<IConfiguration>(Configuration);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
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

            app.UseMvcWithDefaultRoute();
        }
    }
}
