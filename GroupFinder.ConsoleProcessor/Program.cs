using GroupFinder.Common;
using GroupFinder.Common.Aad;
using GroupFinder.Common.Configuration;
using GroupFinder.Common.Logging;
using GroupFinder.Common.PersistentStorage;
using GroupFinder.Common.Search;
using GroupFinder.Common.Security;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace GroupFinder.ConsoleProcessor
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Run().Wait();
                return 0;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                return -1;
            }
        }

        public static async Task Run()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();
#if DEBUG
            builder.AddUserSecrets("GroupFinder");
#endif
            var configuration = builder.Build();
            var appConfig = new AppConfiguration();
            configuration.Bind(appConfig);
            configuration.GetSection("App").Bind(appConfig);

            var logger = new AggregateLogger(new ILogger[] { new ConsoleLogger(EventLevel.Informational), new DebugLogger(EventLevel.Verbose), new TraceLogger(EventLevel.Verbose) });
            var persistentStorage = new AzureBlobStorage(logger, appConfig.AzureStorage.Account, appConfig.AzureStorage.Container, appConfig.AzureStorage.AdminKey);
            var searchService = new AzureSearchService(logger, appConfig.AzureSearch.Service, appConfig.AzureSearch.Index, appConfig.AzureSearch.AdminKey);

            var cache = new PersistentStorageTokenCache(logger, persistentStorage, appConfig.AzureAD.TokenCacheFileName);
            await cache.LoadAsync();
            var tokenProvider = new AdalSilentTokenProvider(appConfig.AzureAD.Tenant, appConfig.AzureAD.ClientId, cache);
            var graphClient = new AadGraphClient(logger, appConfig.AzureAD.Tenant, tokenProvider);

            var processor = new Processor(logger, persistentStorage, graphClient, searchService);

            while (true)
            {
                await processor.SynchronizeGroupsAsync();
                var waitTime = appConfig.Processor.GroupSyncWaitTime;
                logger.Log(EventLevel.Informational, $"Waiting {waitTime} to start next group synchronization");
                await Task.Delay(waitTime);
            }
        }
    }
}