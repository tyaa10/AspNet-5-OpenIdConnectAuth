using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OpenIdConnectAuth
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddAuthentication(
                options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "GoogleOpenID";
                }
            ).AddCookie(
                options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/denied";
                    options.Events = new CookieAuthenticationEvents()
                    {
                        OnSigningIn = async context =>
                        {
                            var principal = context.Principal;
                            if (principal != null && principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                            {
                                Console.WriteLine(principal.Claims
                                    .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
                                if (principal.Claims
                                    .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value == "John")
                                {
                                    var claimsIdentity = principal.Identity as ClaimsIdentity;
                                    Console.WriteLine(claimsIdentity);
                                    claimsIdentity?.AddClaim(new(ClaimTypes.Role, "Admin"));
                                }
                            }
                            await Task.CompletedTask;
                        }
                    };
                }
            ).AddOpenIdConnect("GoogleOpenID", options =>
                {
                    options.Authority = "https://accounts.google.com";
                    options.ClientId = "824259445368-7f4dim7eap6c1321n7vv7n728ot3elek.apps.googleusercontent.com";
                    options.ClientSecret = "NPw8O8pZ0ybyLwufcNtD5JKt";
                    options.CallbackPath = "/auth";
                }
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}