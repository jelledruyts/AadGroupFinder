using GroupFinder.Common;
using GroupFinder.Common.Aad;
using GroupFinder.Common.Configuration;
using GroupFinder.Common.Logging;
using GroupFinder.Common.PersistentStorage;
using GroupFinder.Common.Search;
using GroupFinder.Common.Security;
using GroupFinder.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics.Tracing;
using System.Text;
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
                builder.AddJsonFile(@"Properties/launchSettings.json", optional: false, reloadOnChange: true); // This is for reading out the local SSL port.
                builder.AddUserSecrets("GroupFinder");
                builder.AddApplicationInsightsSettings(developerMode: true);
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
                    // Force HTTPS for Web API (this doesn't affect static files though).
                    options.Filters.Add(new RequireHttpsAttribute());

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

            // Add Application Insights.
            services.AddApplicationInsightsTelemetry(this.Configuration);

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

            // Force HTTPS for all requests (including non-MVC requests).
            app.Use(async (context, next) =>
            {
                if (context.Request.IsHttps)
                {
                    await next();
                }
                else
                {
                    var host = new HostString(context.Request.Host.Host);
                    if (env.IsDevelopment())
                    {
                        var sslPort = Configuration.GetValue<int>("iisSettings:iisExpress:sslPort");
                        host = new HostString(context.Request.Host.Host, sslPort);
                    }
                    var httpsUrl = $"https://{host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
                    context.Response.Redirect(httpsUrl, true);
                }
            });

            // Add Application Insights monitoring to the request pipeline as a very first middleware.
            app.UseApplicationInsightsRequestTelemetry();

            // Return unhandled exceptions as JSON errors in the format defined at https://github.com/Microsoft/api-guidelines/blob/master/Guidelines.md#710-response-formats.
            var errorJsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), NullValueHandling = NullValueHandling.Ignore };
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = Constants.JsonContentType;
                    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (exceptionHandlerFeature != null)
                    {
                        var exception = exceptionHandlerFeature.Error;
                        var error = default(Error);
                        var apiException = exception as ApiException;
                        if (apiException != null)
                        {
                            error = new Error(apiException.Code, apiException.Message);
                        }
                        else
                        {
                            error = new Error("Unexpected Error", exception.Message);
                        }
                        var errorResponse = new ErrorResponse(error);
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse, Formatting.None, errorJsonSerializerSettings), Encoding.UTF8);
                    }
                });
            });

            // Add Application Insights exceptions handling to the request pipeline.
            // NOTE: Exception middleware should be added after error page and any other error handling middleware.
            app.UseApplicationInsightsExceptionTelemetry();

            // This must be placed before UseStaticFiles because it only rewrites the URL, which is then served by UseStaticFiles.
            app.UseDefaultFiles();

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Require authentication using OAuth 2.0 bearer tokens.
            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Authority = GroupFinder.Common.Constants.AadEndpoint + this.AppConfiguration.AzureAD.Tenant,
                Audience = this.AppConfiguration.AzureAD.Audience,
                SaveToken = true // This makes the JWT token available through "this.HttpContext.Authentication.GetTokenAsync(...)".
            });

            // Use MVC to enable Web API.
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
            var tokenProvider = new AdalSilentTokenProvider(logger, appConfig.AzureAD.Tenant, appConfig.AzureAD.ClientId, cache);
            var graphClient = new AadGraphClient(logger, appConfig.AzureAD.Tenant, tokenProvider);

            return new Processor(logger, persistentStorageForState, persistentStorageForBackups, graphClient, searchService);
        }
    }
}