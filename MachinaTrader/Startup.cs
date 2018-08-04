using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MachinaTrader.Hubs;
using Serilog;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MachinaTrader.Data;
using System;
using MachinaTrader.Services;
using MachinaTrader.Helpers;
using LazyCache;

namespace MachinaTrader
{
    public class Startup
    {
        public static IServiceScope ServiceScope { get; private set; }
        public static IConfiguration Configuration { get; set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConnections();

            services.AddSignalR();

            services.AddCors(o =>
            {
                o.AddPolicy("Everything", p =>
             {
              p.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin()
              .AllowCredentials();
              });
            });

            services.AddLazyCache();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite("Filename=MachinaTraderAuth.db")
            );

            services.AddIdentity<IdentityUser, IdentityRole>(options => options.Stores.MaxLengthForKeys = 128)
                .AddEntityFrameworkStores<ApplicationDbContext>()
                // .AddDefaultUI()
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

            // Add Database Initializer
            services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            services.AddAuthorization();

            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.FromHours(24);
            });


            // Configure serilog from appsettings.json
            var serilogger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .WriteTo.SignalRLogEventSink()
                .CreateLogger();

            services.AddLogging(b => { b.AddSerilog(serilogger); });

            services.AddMvc().AddRazorPagesOptions(options =>
            {
                options.Conventions.AuthorizePage("/");
                options.Conventions.AuthorizeFolder("/");
                //options.Conventions.AllowAnonymousToPage("/Account");
                //options.Conventions.AllowAnonymousToFolder("/Account");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment hostingEnvironment, ILoggerFactory loggerFactory, IDatabaseInitializer databaseInitializer, IAppCache cache)
        {
            ServiceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            Runtime.GlobalServiceScope = ServiceScope;
            Runtime.GlobalLoggerFactory = loggerFactory;
            Runtime.GlobalApplicationBuilder = app;
            Runtime.AppCache = cache;

            app.UseStaticFiles();

            if (hostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseWebSockets();

            app.UseSignalR(routes =>
            {
                routes.MapHub<HubMainIndex>("/signalr/HubMainIndex");
                routes.MapHub<HubTraders>("/signalr/HubMyntTraders");
                routes.MapHub<HubStatistics>("/signalr/HubMyntStatistics");
                routes.MapHub<HubLogs>("/signalr/HubMyntLogs");
                routes.MapHub<HubBacktest>("/signalr/HubMyntBacktest");
            });

            app.UseMvc();

            // Init Database
            databaseInitializer.Initialize();

            // DI is ready - Init 
            RuntimeSettings.Init();
        }

        public static void RunWebHost()
        {
            IWebHostBuilder webHostBuilder = WebHost.CreateDefaultBuilder()
                .UseKestrel(options => { options.Listen(IPAddress.Any, Runtime.Configuration.SystemOptions.WebPort); })
                .UseStartup<Startup>()
                .ConfigureAppConfiguration(i => i.AddJsonFile("appsettings.overrides.json", true));

            IWebHost webHost = webHostBuilder.Build();
            webHost.Run();
        }
    }
}
