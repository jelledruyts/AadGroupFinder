using GroupFinder.Common;
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
            var processor = GetProcessor();
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
                    Console.WriteLine("Synchronization Status: " + (status.LastGroupSyncCompletedTime.HasValue ? "Last synchronized on " + status.LastGroupSyncCompletedTime.Value : "Incomplete"));
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

        private static Processor GetProcessor()
        {
            var aadTenant = ConfigurationManager.AppSettings["AadTenant"];
            var aadClientId = ConfigurationManager.AppSettings["AadClientId"];
            var aadClientRedirectUri = new Uri(ConfigurationManager.AppSettings["AadClientRedirectUri"]);
            var aadClientSecret = ConfigurationManager.AppSettings["AadClientSecret"];
            var azureSearchService = ConfigurationManager.AppSettings["AzureSearchService"];
            var azureSearchIndex = ConfigurationManager.AppSettings["AzureSearchIndex"];
            var azureSearchAdminKey = ConfigurationManager.AppSettings["AzureSearchAdminKey"];

            var host = new ConsoleHost(EventLevel.Informational);
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
                    host.Log(EventLevel.Verbose, $"Acquiring access token from client credentials grant");
                    authenticationResult = await authenticationContext.AcquireTokenAsync(Constants.AadGraphApiEndpoint, clientCredential);
                }
                else
                {
                    host.Log(EventLevel.Verbose, $"Acquiring access token from end user grant");
                    authenticationResult = await authenticationContext.AcquireTokenAsync(Constants.AadGraphApiEndpoint, aadClientId, aadClientRedirectUri, new PlatformParameters(PromptBehavior.Auto));
                }
                return authenticationResult.AccessToken;
            };

            var graphClient = new AadGraphClient(host, aadTenant, accessTokenFactory);
            var processor = new Processor(host, graphClient, new AzureSearchService(host, azureSearchService, azureSearchIndex, azureSearchAdminKey));
            return processor;
        }
    }
}