using GroupFinder.Common;
using GroupFinder.Common.Aad;
using GroupFinder.Common.Configuration;
using GroupFinder.Common.Logging;
using GroupFinder.Common.PersistentStorage;
using GroupFinder.Common.Search;
using GroupFinder.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace GroupFinder.Web
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public AppConfiguration AppConfiguration { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            if (env.IsDevelopment())
            {
                builder.AddUserSecrets("GroupFinder");
            }
            this.AppConfiguration = new AppConfiguration();
            this.Configuration = builder.Build();
            this.Configuration.GetSection("App").Bind(this.AppConfiguration);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC framework services.
            services.AddMvc()
                .AddMvcOptions(options =>
                {
                    // Force authenticated users globally.
                    var authenticatedUserPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                    options.Filters.Add(new AuthorizeFilter(authenticatedUserPolicy));

                    // Disable caching.
                    options.Filters.Add(new ResponseCacheAttribute { NoStore = true });
                })
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });

            // Add authentication services.
            services.AddAuthentication();

            // Add routing.
            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });

            // Create and inject the Processor singleton.
            var processor = GetProcessorAsync(this.AppConfiguration).Result;
            services.AddSingleton(processor);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Configure logging.
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            // This must be placed before UseStaticFiles because it only rewrites the URL, which is then served by UseStaticFiles.
            app.UseDefaultFiles();

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Authority = GroupFinder.Common.Constants.AadEndpoint + this.AppConfiguration.AzureAD.Tenant,
                Audience = this.AppConfiguration.AzureAD.Audience,
                SaveToken = true // This makes the JWT token available through "this.HttpContext.Authentication.GetTokenAsync(...)".
            });

            app.UseMvc();
        }

        private static async Task<Processor> GetProcessorAsync(AppConfiguration appConfig)
        {
            var logger = new AggregateLogger(new GroupFinder.Common.Logging.ILogger[] { new ConsoleLogger(EventLevel.Informational), new DebugLogger(EventLevel.Verbose), new TraceLogger(EventLevel.Verbose) });
            var persistentStorageForState = new AzureBlobStorage(logger, appConfig.AzureStorage.Account, appConfig.AzureStorage.StateContainer, appConfig.AzureStorage.AdminKey);
            var persistentStorageForBackups = new AzureBlobStorage(logger, appConfig.AzureStorage.Account, appConfig.AzureStorage.BackupContainer, appConfig.AzureStorage.AdminKey);
            var searchService = new AzureSearchService(logger, appConfig.AzureSearch.Service, appConfig.AzureSearch.Index, appConfig.AzureSearch.AdminKey, true);

            var cache = new PersistentStorageTokenCache(logger, persistentStorageForState, appConfig.AzureAD.TokenCacheFileName);
            await cache.LoadAsync();
            var tokenProvider = new AdalSilentTokenProvider(appConfig.AzureAD.Tenant, appConfig.AzureAD.ClientId, cache);
            var graphClient = new AadGraphClient(logger, appConfig.AzureAD.Tenant, tokenProvider);

            return new Processor(logger, persistentStorageForState, persistentStorageForBackups, graphClient, searchService);
        }
    }
}