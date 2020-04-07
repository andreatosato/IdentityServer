// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityExpress.Identity;
using IdentityExpress.Manager.Api;
using IdentityServer4;
using IdentityServer4.Configuration;
using is4admin.Custom;
using is4admin.Data;
using is4admin.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Reflection;
using static is4admin.Custom.AuthorizationCodeReceived;

namespace is4admin
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            // preserve OIDC state in cache (solves problems with AAD and URL lenghts)
            services.AddOidcStateDataFormatterCache();
            // cookie policy to deal with temporary browser incompatibilities
            services.AddSameSiteCookiePolicy();

            // configures IIS out-of-proc settings (see https://github.com/aspnet/AspNetCore/issues/14882)
            services.Configure<IISOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            // configures IIS in-proc settings
            services.Configure<IISServerOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("Users")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options => {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 2;
                options.Lockout.AllowedForNewUsers = true;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var connectionString = Configuration.GetConnectionString("Configuration");
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;


            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;

                    options.UserInteraction = new UserInteractionOptions
                    {
                        LogoutUrl = "/Account/Logout",
                        LoginUrl = "/Account/Login",
                        LoginReturnUrlParameter = "returnUrl"
                    };
                })
                .AddAspNetIdentity<ApplicationUser>()
                // this adds the config data from DB (clients, resources, CORS)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = db =>
                        db.UseSqlServer(connectionString,
                            sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = db =>
                        db.UseSqlServer(connectionString,
                            sql => sql.MigrationsAssembly(migrationsAssembly));

                    // this enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                    // options.TokenCleanupInterval = 15; // interval in seconds. 15 seconds useful for debugging
                });

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();

            services.AddAuthentication()
                //.AddGoogle(options =>
                //{
                //    // register your IdentityServer with Google at https://console.developers.google.com
                //    // enable the Google+ API
                //    // set the redirect URI to http://localhost:5000/signin-google
                //    options.ClientId = "copy client ID from Google here";
                //    options.ClientSecret = "copy client secret from Google here";
                //})
                .AddOpenIdConnect("AAD", "Azure Active Directory", options =>
                {
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                    options.Authority = Globals.Authority;
                    options.ClientId = Globals.ClientId;
                    options.ClientSecret = Globals.ClientSecret;
                    options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };

                    options.Events.OnAuthorizationCodeReceived =
                        async (ctx) => await AuthorizationCodeReceived.CodeRedemptionAsync(ctx);
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                });

            services.UseAdminUI();            
            services.AddScoped<IdentityExpressDbContext, SqlServerIdentityDbContext>(sp => new SqlServerIdentityDbContext(Configuration.GetConnectionString("Users")));
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseAdminUI();

            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }
    }
}
