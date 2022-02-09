using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MachinaTrader.Hubs;
using Serilog;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.Identity.LiteDB;
using AspNetCore.Identity.LiteDB.Data;
using LazyCache;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Data;
using MachinaTrader.Globals.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Linq;
using LazyCache.Providers;
using MachinaTrader.Globals.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;

namespace MachinaTrader
{
    public static class ConfigurationExtensions
    {
        private static readonly MethodInfo MapHubMethod = typeof(HubRouteBuilder).GetMethod("MapHub", new[] { typeof(PathString) });

        public static HubRouteBuilder MapSignalrRoutes(this HubRouteBuilder hubRouteBuilder)
        {
            hubRouteBuilder.MapHub<HubMainIndex>("/signalr/HubMainIndex");
            hubRouteBuilder.MapHub<HubTraders>("/signalr/HubTraders");
            hubRouteBuilder.MapHub<HubStatistics>("/signalr/HubStatistics");
            hubRouteBuilder.MapHub<HubBacktest>("/signalr/HubBacktest");
            hubRouteBuilder.MapHub<HubExchangeAccounts>("/signalr/HubExchangeAccounts");
            //Hub Log is located in Globals because we need to wire up with serilog
            hubRouteBuilder.MapHub<HubLogs>("/signalr/HubLogs");
            return hubRouteBuilder;
        }
    }


    public class Startup
    {
        public static IServiceScope ServiceScope { get; private set; }
        public IHostingEnvironment HostingEnvironment { get; set; }
        public static IConfiguration Configuration { get; set; }
        public IContainer ApplicationContainer { get; private set; }
        public static readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Global.Configuration.SystemOptions.RsaPrivateKey));
        public static readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();

        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddConnections();

            services.AddSignalR();

            services.AddCors(o =>
            {
                o.AddPolicy("Everything", p =>
                {
                    p.AllowAnyHeader();
                    p.AllowAnyMethod();
                    p.AllowAnyOrigin();
                    p.AllowCredentials();
                });
            });

            //services.AddLazyCache();


            // Add LiteDB Dependency
            string authDbPath = Global.DataPath + "/MachinaTraderUsers.db";
            services.AddSingleton<ILiteDbContext, LiteDbContext>(serviceProvider => new LiteDbContext(HostingEnvironment, authDbPath));

            services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Optimal);
            services.AddResponseCompression();

            services.AddIdentity<AspNetCore.Identity.LiteDB.Models.ApplicationUser, AspNetCore.Identity.LiteDB.IdentityRole>(options => options.Stores.MaxLengthForKeys = 128)
                .AddUserStore<LiteDbUserStore<AspNetCore.Identity.LiteDB.Models.ApplicationUser>>()
                .AddRoleStore<LiteDbRoleStore<AspNetCore.Identity.LiteDB.IdentityRole>>()
                .AddDefaultTokenProviders();

            //Override Password Policy
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 1;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            });

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            // Add Database Initializer
            services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                });
            });

            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.FromHours(24);
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                //options.RequireHttpsMetadata = false;
                //options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    ValidIssuer = "MachinaTrader",
                    ValidAudience = "MachinaTrader",
                    IssuerSigningKey = SecurityKey
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        //Check for Tokenheader -> Needed if we call signalr from external program
                        var signalRTokenHeader = context.Request.Query["signalrtoken"];

                        if (!string.IsNullOrEmpty(signalRTokenHeader) &&
                            (context.HttpContext.WebSockets.IsWebSocketRequest || context.Request.Headers["Accept"] == "text/event-stream"))
                        {
                            context.Token = context.Request.Query["signalrtoken"];
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.ConfigureApplicationCookie(options => {
                options.Cookie.Name = "MachinaTraderIdentity";
            });

            services.AddAntiforgery(options => {
                options.Cookie.Name = "MachinaTraderAntiforgery";
            });

            services.AddLogging(b => { b.AddSerilog(Globals.Global.Logger); });


            services.AddMvc().AddRazorPagesOptions(options =>
            {
                options.Conventions.AuthorizePage("/");
                options.Conventions.AuthorizeFolder("/");
                //options.Conventions.AllowAnonymousToPage("/Account");
                //options.Conventions.AllowAnonymousToFolder("/Account");
            });

            services.AddControllers().AddNewtonsoftJson();

            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule(new AppCacheModule());

            containerBuilder.Populate(services);
            ApplicationContainer = containerBuilder.Build();
            return ApplicationContainer.Resolve<IServiceProvider>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment hostingEnvironment, IAppCache cache, ILiteDbContext liteDbContext, IDatabaseInitializer databaseInitializer)
        {
            Global.ServiceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            Global.ApplicationBuilder = app;
            Global.AppCache = cache;

            app.UseStaticFiles();

            if (hostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseWebSockets();

            app.UseRouting();

            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
            app.UseSignalR(r => r.MapSignalrRoutes());

            // Init Database
            databaseInitializer.Initialize();

            // DI is ready - Init 
            RuntimeSettings.Init();

            Global.WebServerReady = true;
        }

        public static void RunWebHost()
        {
            IWebHostBuilder webHostBuilder = WebHost.CreateDefaultBuilder()
                .UseKestrel(options => { options.Listen(IPAddress.Any, Global.Configuration.SystemOptions.WebPort); })
                .UseStartup<Startup>()
                .UseContentRoot(Global.AppPath)
                .ConfigureAppConfiguration(i => i.AddJsonFile(Global.DataPath + "/Logging.json", true));

            IWebHost webHost = webHostBuilder.Build();
            webHost.Run();
        }
    }

    public class AppCacheModule : Autofac.Module
    {
       protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))))
               .As<IAppCache>()
               .InstancePerLifetimeScope();
        }
    }
}
