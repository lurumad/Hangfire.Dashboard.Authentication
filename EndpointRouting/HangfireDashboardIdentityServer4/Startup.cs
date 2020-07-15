using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace HangfireDashboardIdentityServer4
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddAuthorization(cfg =>
                {
                    cfg.AddPolicy("Hangfire", cfgPolicy =>
                    {
                        cfgPolicy.AddRequirements().RequireAuthenticatedUser();
                        cfgPolicy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
                    });
                })
                .AddAuthentication(cfg =>
                {
                    cfg.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    cfg.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddOpenIdConnect(cfg =>
                {
                    cfg.Authority = "https://demo.identityserver.io";
                    cfg.ClientId = "interactive.confidential";
                    cfg.ClientSecret = "secret";
                    cfg.ResponseType = "code";
                    cfg.UsePkce = true;

                    cfg.Scope.Clear();
                    cfg.Scope.Add("openid");
                    cfg.Scope.Add("profile");

                    cfg.SaveTokens = true;
                });

            services.AddHangfire(cfg =>
            {
                cfg.UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                });
            });

            services.AddHangfireServer();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            const string HangfireDashboardPath = "/hangfire";

            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Equals(HangfireDashboardPath, StringComparison.OrdinalIgnoreCase)
                    && !context.User.Identity.IsAuthenticated)
                {
                    await context.ChallengeAsync();
                    return;
                }

                await next();
            });

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHangfireDashboard().RequireAuthorization("Hangfire");
            });
        }
    }
}
