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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MachinaTrader.Helpers;
using LazyCache;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Data;
using MachinaTrader.Globals.Models;
using MachinaTrader.Globals.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Linq;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MachinaTrader
{
    public static class ConfigurationExtensions
    {
        private static readonly MethodInfo MapHubMethod = typeof(HubRouteBuilder).GetMethod("MapHub", new[] { typeof(PathString) });

        public static HubRouteBuilder MapSignalrRoutes(this HubRouteBuilder hubRouteBuilder)
        {
            IEnumerable<Assembly> assembliesPlugins = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "MachinaCore.Plugin.*.dll", SearchOption.TopDirectoryOnly)
                .Select(Assembly.LoadFrom);

            foreach (var assembly in assembliesPlugins)
            {
                IEnumerable<Type> pluginHubTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Hub)) && !t.IsAbstract);

                foreach (var pluginHubType in pluginHubTypes)
                {
                    //Console.WriteLine("Assembly Name: " + assembly.GetName().Name);
                    //Console.WriteLine("HubName: " + pluginHubType);
                    string hubRoute = pluginHubType.ToString().Replace(assembly.GetName().Name, "").Replace(".Hubs.", "").Replace("MyntUI", "");
                    Global.Logger.Information(assembly.GetName().Name + " - Hub Route " + hubRoute);
                    MapHubMethod.MakeGenericMethod(pluginHubType).Invoke(hubRouteBuilder, new object[] { new PathString("/signalr/" + hubRoute) });
                }
            }
            //Add Global Hubs -> No plugin
            hubRouteBuilder.MapHub<HubMainIndex>("/signalr/HubMainIndex");
            hubRouteBuilder.MapHub<HubTraders>("/signalr/HubTraders");
            hubRouteBuilder.MapHub<HubStatistics>("/signalr/HubStatistics");
            hubRouteBuilder.MapHub<HubLogs>("/signalr/HubLogs");
            hubRouteBuilder.MapHub<HubBacktest>("/signalr/HubBacktest");
            return hubRouteBuilder;
        }
    }


    public class Startup
    {
        public static IServiceScope ServiceScope { get; private set; }
        public static IConfiguration Configuration { get; set; }
        public IContainer ApplicationContainer { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
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
            
            // Add Database Initializer
            services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Filename=" + Global.DataPath + "/MachinaTraderAuth.db"));

            services.AddIdentity<ApplicationUser, IdentityRole>(options => options.Stores.MaxLengthForKeys = 128)
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

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            services.AddAuthorization();

            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.FromHours(24);
            });

            services.AddLogging(b => { b.AddSerilog(Globals.Global.Logger); });

            var mvcBuilder = services.AddMvc().AddRazorPagesOptions(options =>
            {
                options.Conventions.AuthorizePage("/");
                options.Conventions.AuthorizeFolder("/");
                //options.Conventions.AllowAnonymousToPage("/Account");
                //options.Conventions.AllowAnonymousToFolder("/Account");
            });

            var containerBuilder = new ContainerBuilder();

            //Register Plugins
            IEnumerable<Assembly> assembliesPlugins = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "MachinaTrader.Plugin.*.dll", SearchOption.TopDirectoryOnly)
                //.Where(filePath => Path.GetFileName(filePath).StartsWith("your name space"))
                .Select(Assembly.LoadFrom);

            foreach (var assembly in assembliesPlugins)
            {
                AssemblyName pluginName = AssemblyName.GetAssemblyName(assembly.Location);
                if ((bool)Global.CoreRuntime["Plugins"][pluginName.Name]["Enabled"])
                {
                    Console.WriteLine(assembly.ToString());
                    mvcBuilder.AddApplicationPart(assembly);
                    containerBuilder.RegisterAssemblyModules(assembly);
                }
            }

            containerBuilder.RegisterModule(new AppCacheModule());

            containerBuilder.Populate(services);
            ApplicationContainer = containerBuilder.Build();
            return ApplicationContainer.Resolve<IServiceProvider>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment hostingEnvironment, IDatabaseInitializer databaseInitializer, IAppCache cache)
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

            // Init Plugins
            foreach (JProperty plugin in Global.CoreRuntime["Plugins"])
            {
                if ((bool)Global.CoreRuntime["Plugins"][plugin.Name]["Enabled"] == false)
                {
                    continue;
                }

                if ((string)Global.CoreRuntime["Plugins"][plugin.Name]["WwwRootDataFolder"] != null)
                {
                    app.UseStaticFiles(new StaticFileOptions()
                    {
                        FileProvider = new PhysicalFileProvider(Global.DataPath + "/" + plugin.Name + "/wwwroot"),
                        RequestPath = new PathString("/" + plugin.Name)
                    });
                }

                if ((string)Global.CoreRuntime["Plugins"][plugin.Name]["WwwRoot"] != null)
                {
                    app.UseStaticFiles(new StaticFileOptions()
                    {
                        FileProvider = new PhysicalFileProvider((string)Global.CoreRuntime["Plugins"][plugin.Name]["WwwRoot"]),
                        RequestPath = new PathString("/" + plugin.Name)
                    });
                }
            }

            app.UseWebSockets();

            app.UseSignalR(route => route.MapSignalrRoutes());

            app.UseMvc();

            // Init Database
            databaseInitializer.Initialize();

            // DI is ready - Init 
            RuntimeSettings.Init();

            Global.WebServerReady = true;
        }

        public static void RunWebHost()
        {
            IWebHostBuilder webHostBuilder = WebHost.CreateDefaultBuilder()
                .UseKestrel(options => { options.Listen(IPAddress.Any, Runtime.Configuration.SystemOptions.WebPort); })
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
