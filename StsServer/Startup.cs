using StsServer.Data;
using StsServer.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.IISIntegration;
using IdentityServer4;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging;

namespace StsServer
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            string clientId = "f59d5739-1ec9-46fc-961d-b01ef6fb3c51";
            string tenantId = "36946883-9d0b-4229-82f9-316f0ae71b20";
            //tenantId = "https://ignatandreiyahoo.onmicrosoft.com";
            services
                   
                .AddAuthentication(IISDefaults.AuthenticationScheme)
                .AddOpenIdConnect("aad", "Sign-in with Azure AD", options =>
                {
                    //options.Authority = $"https://login.microsoftonline.com/common/v2.0/";
                    //options.Authority = $"https://ignatandreiyahoo.onmicrosoft.com";
                    options.Authority = $"https://login.windows.net/{tenantId}";
                    options.ClientId = $"{clientId}";
                    //options.RequireHttpsMetadata = true;
                    

                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;

                    options.ResponseType = "id_token";
                    options.CallbackPath = "/signin-aad";
                    options.SignedOutCallbackPath = "/signout-callback-aad";
                    options.RemoteSignOutPath = "/signout-aad";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        //ValidAudience = "f59d5739-1ec9-46fc-961d-b01ef6fb3c51",

                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                    options.Events.OnRemoteFailure = (context) =>
                    {
                        string s = context.ToString();
                        return Task.CompletedTask;
                    };
                })

                ;
                services.AddOidcStateDataFormatterCache("aad");
            //services.Configure<OpenIdConnectOptions>("AzureADOpenID", options =>
            //{
            //    options.Authority = options.Authority + "/v2.0/";
            //});
            //services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
            //        .AddNegotiate(); 
            services.Configure<IISOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = true;
            });

            var builder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
            })
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryApiResources(Config.GetApis())
                .AddInMemoryClients(Config.GetClients())
                .AddAspNetIdentity<ApplicationUser>();

            builder.AddDeveloperSigningCredential();

            services.AddControllersWithViews()
                 .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseIdentityServer();

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
