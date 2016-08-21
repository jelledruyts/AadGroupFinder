using GroupFinder.Common;
using GroupFinder.Common.Aad;
using GroupFinder.Common.Logging;
using GroupFinder.Common.PersistentStorage;
using GroupFinder.Common.Search;
using GroupFinder.Common.Security;
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
                var aadTenant = ConfigurationManager.AppSettings["AadTenant"];
                var aadClientId = ConfigurationManager.AppSettings["AadClientId"];
                var aadClientRedirectUri = new Uri(ConfigurationManager.AppSettings["AadClientRedirectUri"]);
                var azureSearchService = ConfigurationManager.AppSettings["AzureSearchService"];
                var azureSearchIndex = ConfigurationManager.AppSettings["AzureSearchIndex"];
                var azureSearchAdminKey = ConfigurationManager.AppSettings["AzureSearchAdminKey"];
                var azureStorageAccount = ConfigurationManager.AppSettings["AzureStorageAccount"];
                var azureStorageContainer = ConfigurationManager.AppSettings["AzureStorageContainer"];
                var azureStorageKey = ConfigurationManager.AppSettings["AzureStorageKey"];

                var tokenProvider = new AdalInteractiveTokenProvider(aadTenant, aadClientId, TokenCache.DefaultShared, aadClientRedirectUri);
                var graphClient = new AadGraphClient(logger, aadTenant, tokenProvider);
                var persistentStorage = new AzureBlobStorage(logger, azureStorageAccount, azureStorageContainer, azureStorageKey);
                var searchService = new AzureSearchService(logger, azureSearchService, azureSearchIndex, azureSearchAdminKey);
                var processor = new Processor(logger, persistentStorage, graphClient, searchService);

                while (true)
                {
                    Console.WriteLine("What do you want to do?");
                    Console.WriteLine("  1 - Display Status");
                    Console.WriteLine("  2 - Synchronize Groups");
                    Console.WriteLine("  3 - Find Users");
                    Console.WriteLine("  4 - Find Groups");
                    Console.WriteLine("  5 - Find Shared Group Memberships");
                    Console.WriteLine("  6 - Prime ADAL Token Cache");
                    Console.WriteLine("  Q - Quit");
                    var command = Console.ReadLine().ToUpperInvariant();
                    if (command == "1")
                    {
                        // Display Status.
                        var status = await processor.GetServiceStatusAsync();
                        var groupSyncStatus = "Group Sync Status: ";
                        if (status.LastGroupSyncStartedTime.HasValue)
                        {
                            groupSyncStatus += $"In progress; sync started {status.LastGroupSyncStartedTime.Value}";
                        }
                        else
                        {
                            groupSyncStatus += "Inactive";
                        }
                        if (status.LastGroupSyncCompletedTime.HasValue)
                        {
                            groupSyncStatus += $"; last sync completed {status.LastGroupSyncCompletedTime.Value}";
                        }
                        Console.WriteLine(groupSyncStatus);
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
                    else if (command == "6")
                    {
                        Console.WriteLine("Enter the file name of the cache:");
                        var fileName = Console.ReadLine();
                        var cache = new PersistentStorageTokenCache(logger, persistentStorage, fileName);
                        var authenticationContext = new AuthenticationContext(Constants.AadEndpoint + aadTenant, true, cache);
                        var authenticationResult = await authenticationContext.AcquireTokenAsync(Constants.AadGraphApiEndpoint, aadClientId, aadClientRedirectUri, new PlatformParameters(PromptBehavior.Auto));
                        var token = authenticationResult.AccessToken;
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
    }
}