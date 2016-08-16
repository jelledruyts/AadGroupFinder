using GroupFinder.Common;
using GroupFinder.Common.Aad;
using GroupFinder.Common.Logging;
using GroupFinder.Common.PersistentStorage;
using GroupFinder.Common.Search;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace GroupFinder.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                RunAsync().Wait();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }
        }

        public static async Task RunAsync()
        {
            var logger = new AggregateLogger(new ILogger[] { new ConsoleLogger(EventLevel.Informational), new TraceLogger(EventLevel.Verbose) });
            try
            {
                var processor = GetProcessor(logger);
                while (true)
                {
                    Console.WriteLine("What do you want to do?");
                    Console.WriteLine("  1 - Display Status");
                    Console.WriteLine("  2 - Synchronize Groups");
                    Console.WriteLine("  3 - Find Users");
                    Console.WriteLine("  4 - Find Groups");
                    Console.WriteLine("  5 - Find Shared Group Memberships");
                    Console.WriteLine("  Q - Quit");
                    var command = Console.ReadLine().ToUpperInvariant();
                    if (command == "1")
                    {
                        // Display Status.
                        var status = await processor.GetServiceStatusAsync();
                        if (status.LastGroupSyncStartedTime.HasValue)
                        {
                            Console.WriteLine($"Synchronization Status: Incomplete - synchronization started {status.LastGroupSyncStartedTime.Value}");
                        }
                        else
                        {
                            Console.WriteLine("Synchronization Status: Not started");
                        }
                        if (status.LastGroupSyncCompletedTime.HasValue)
                        {
                            Console.WriteLine($"Synchronization Status: Last group synchronization completed {status.LastGroupSyncCompletedTime.Value}");
                        }
                        Console.WriteLine($"Search Service Status: {status.SearchServiceStatistics.DocumentCount} groups indexed ({status.SearchServiceStatistics.IndexSizeBytes / (1024 * 1024)} MB)");
                    }
                    else if (command == "2")
                    {
                        // Synchronize Groups.
                        await processor.SynchronizeGroupsAsync();
                    }
                    else if (command == "3")
                    {
                        // Find Users.
                        Console.WriteLine("Enter search text:");
                        var searchText = Console.ReadLine();
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        var users = await processor.FindUsersAsync(searchText);
                        stopwatch.Stop();

                        var index = 0;
                        foreach (var user in users)
                        {
                            Console.WriteLine($"  {++index}: {user.DisplayName} ({user.UserPrincipalName})");
                        }
                        Console.WriteLine($"Found {users.Count} user(s) in {stopwatch.ElapsedMilliseconds} ms");
                    }
                    else if (command == "4")
                    {
                        // Find Groups.
                        Console.WriteLine("Enter search text:");
                        var searchText = Console.ReadLine();
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        var groups = await processor.FindGroupsAsync(searchText, 10, 0);
                        stopwatch.Stop();

                        var index = 0;
                        foreach (var group in groups)
                        {
                            Console.WriteLine($"  {++index} ({group.Score}): {group.DisplayName} ({group.Mail})");
                        }
                        Console.WriteLine($"Found {groups.Count} group(s) in {stopwatch.ElapsedMilliseconds} ms");
                    }
                    else if (command == "5")
                    {
                        // Find Shared Group Membership.
                        Console.WriteLine("Enter user UPN's separated with semicolons (e.g. 'john@example.org; jane@example.org'):");
                        var upns = Console.ReadLine().Split(';').Select(u => u.Trim()).ToArray();
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        var sharedGroups = await processor.FindSharedGroupMembershipsAsync(upns);
                        stopwatch.Stop();

                        foreach (var sharedGroup in sharedGroups.Where(s => s.UserIds.Count > 1))
                        {
                            Console.WriteLine($"  {sharedGroup.PercentMatch.ToString("P0")}: \"{sharedGroup.Group.DisplayName}\" ({string.Join(";", sharedGroup.UserIds)})");
                        }
                        Console.WriteLine($"Found {sharedGroups.Count} shared group membership(s) in {stopwatch.ElapsedMilliseconds} ms");
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception exc)
            {
                logger.Log(EventLevel.Critical, exc.ToString());
            }
        }

        private static Processor GetProcessor(ILogger logger)
        {
            var aadTenant = ConfigurationManager.AppSettings["AadTenant"];
            var aadClientId = ConfigurationManager.AppSettings["AadClientId"];
            var aadClientRedirectUri = new Uri(ConfigurationManager.AppSettings["AadClientRedirectUri"]);
            var aadClientSecret = ConfigurationManager.AppSettings["AadClientSecret"];
            var azureSearchService = ConfigurationManager.AppSettings["AzureSearchService"];
            var azureSearchIndex = ConfigurationManager.AppSettings["AzureSearchIndex"];
            var azureSearchAdminKey = ConfigurationManager.AppSettings["AzureSearchAdminKey"];

            var authenticationContext = new AuthenticationContext(Constants.AadEndpoint + aadTenant, false);
            var clientCredential = default(ClientCredential);
            if (!string.IsNullOrWhiteSpace(aadClientSecret))
            {
                clientCredential = new ClientCredential(aadClientId, aadClientSecret);
            }
            Func<Task<string>> accessTokenFactory = async () =>
            {
                var authenticationResult = default(AuthenticationResult);
                if (clientCredential != null)
                {
                    logger.Log(EventLevel.Verbose, $"Acquiring access token from client credentials grant");
                    authenticationResult = await authenticationContext.AcquireTokenAsync(Constants.AadGraphApiEndpoint, clientCredential);
                }
                else
                {
                    logger.Log(EventLevel.Verbose, $"Acquiring access token from end user grant");
                    authenticationResult = await authenticationContext.AcquireTokenAsync(Constants.AadGraphApiEndpoint, aadClientId, aadClientRedirectUri, new PlatformParameters(PromptBehavior.Auto));
                }
                return authenticationResult.AccessToken;
            };

            var graphClient = new AadGraphClient(logger, aadTenant, accessTokenFactory);
            var persistentStorage = new FileStorage(logger);
            var searchService = new AzureSearchService(logger, azureSearchService, azureSearchIndex, azureSearchAdminKey);
            return new Processor(logger, persistentStorage, graphClient, searchService);
        }
    }
}