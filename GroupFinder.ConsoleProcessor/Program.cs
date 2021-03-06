﻿using GroupFinder.Common;
using GroupFinder.Common.Aad;
using GroupFinder.Common.Configuration;
using GroupFinder.Common.Logging;
using GroupFinder.Common.Models;
using GroupFinder.Common.PersistentStorage;
using GroupFinder.Common.Search;
using GroupFinder.Common.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace GroupFinder.ConsoleProcessor
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var interactive = (args != null && args.Any(a => string.Equals(a, "/interactive", StringComparison.OrdinalIgnoreCase)));
                Run(interactive).Wait();
                return 0;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                return -1;
            }
        }

        public static async Task Run(bool interactive)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();
#if DEBUG
            builder.AddUserSecrets("GroupFinder");
#endif
            var configuration = builder.Build();
            var appConfig = new AppConfiguration();
            configuration.GetSection("App").Bind(appConfig);

            var logger = new AggregateLogger(new ILogger[] { new ConsoleLogger(EventLevel.Informational), new DebugLogger(EventLevel.Verbose), new TraceLogger(EventLevel.Verbose) });
            var persistentStorageForState = new AzureBlobStorage(logger, appConfig.AzureStorage.Account, appConfig.AzureStorage.StateContainer, appConfig.AzureStorage.AdminKey);
            var persistentStorageForBackups = new AzureBlobStorage(logger, appConfig.AzureStorage.Account, appConfig.AzureStorage.BackupContainer, appConfig.AzureStorage.AdminKey);
            // When running as a non-interactive service, do not force index initialization.
            var forceIndexInitialization = interactive;
            var searchService = new AzureSearchService(logger, appConfig.AzureSearch.Service, appConfig.AzureSearch.Index, appConfig.AzureSearch.AdminKey, forceIndexInitialization);

            var cache = new PersistentStorageTokenCache(logger, persistentStorageForState, appConfig.AzureAD.TokenCacheFileName);
            await cache.LoadAsync();
            var tokenProvider = new AdalSilentTokenProvider(logger, appConfig.AzureAD.Tenant, appConfig.AzureAD.ClientId, cache);
            var graphClient = new AadGraphClient(logger, appConfig.AzureAD.Tenant, tokenProvider);

            var processor = new Processor(logger, persistentStorageForState, persistentStorageForBackups, graphClient, searchService);

            if (interactive)
            {
                // Interactive mode: prompt for action.
                while (true)
                {
                    try
                    {
                        Console.WriteLine("What do you want to do?");
                        Console.WriteLine("  1 - Display Status");
                        Console.WriteLine("  2 - Find Users");
                        Console.WriteLine("  3 - Get Groups Of A User");
                        Console.WriteLine("  4 - Find Groups");
                        Console.WriteLine("  5 - Find Shared Group Memberships");
                        Console.WriteLine("  6 - Get Recommended Groups");
                        Console.WriteLine("  7 - Get Group By Mail");
                        Console.WriteLine("  8 - Get Group Members");
                        Console.WriteLine("  A - Synchronize Groups");
                        Console.WriteLine("  B - Prime ADAL Token Cache");
                        var command = Console.ReadLine().ToUpperInvariant();
                        if (command == "1")
                        {
                            await DisplayStatusAsync(processor);
                        }
                        else if (command == "2")
                        {
                            await FindUsersAsync(processor);
                        }
                        else if (command == "3")
                        {
                            await GetUserGroupsAsync(processor);
                        }
                        else if (command == "4")
                        {
                            await FindGroupsAsync(processor);
                        }
                        else if (command == "5")
                        {
                            await GetSharedGroupMembershipsAsync(processor);
                        }
                        else if (command == "6")
                        {
                            await GetRecommendedGroupsAsync(processor);
                        }
                        else if (command == "7")
                        {
                            await GetGroupByMailAsync(processor);
                        }
                        else if (command == "8")
                        {
                            await GetGroupMembersAsync(processor);
                        }
                        else if (command == "A")
                        {
                            await SynchronizeGroupsOnceAsync(processor);
                        }
                        else if (command == "B")
                        {
                            await PrimeAdalCacheAsync(logger, persistentStorageForState, appConfig.AzureAD.Tenant, appConfig.AzureAD.ClientId, appConfig.AzureAD.RedirectUri);
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine($"Error: {exc.ToString()}");
                    }
                }
            }
            else
            {
                // Non-interactive mode: continuously synchronize groups.
                await SynchronizeGroupsContinuouslyAsync(logger, processor, appConfig.Processor.GroupSyncWaitTime);
            }
        }

        #region Commands

        private static async Task DisplayStatusAsync(Processor processor)
        {
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
            Console.WriteLine($"Search Service Status: {status.GroupCount} groups indexed ({status.GroupSearchIndexSizeBytes / (1024 * 1024)} MB)");
        }

        private static async Task FindUsersAsync(Processor processor)
        {
            Console.WriteLine("Enter search text:");
            var searchText = Console.ReadLine();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var users = await processor.FindUsersAsync(searchText, null);
            stopwatch.Stop();

            var index = 0;
            foreach (var user in users)
            {
                Console.WriteLine($"  {++index}: {user.DisplayName} ({user.UserPrincipalName})");
            }
            Console.WriteLine($"Found {users.Count} user(s) [{stopwatch.ElapsedMilliseconds} ms]");
        }

        private static async Task GetUserGroupsAsync(Processor processor)
        {
            Console.WriteLine("Enter user UPN:");
            var userId = Console.ReadLine();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var groups = await processor.GetUserGroupsAsync(userId);
            stopwatch.Stop();

            var index = 0;
            foreach (var group in groups)
            {
                Console.WriteLine($"  {++index}: {group.DisplayName} ({group.Mail})");
            }
            Console.WriteLine($"Found {groups.Count} group(s) [{stopwatch.ElapsedMilliseconds} ms]");
        }

        private static async Task FindGroupsAsync(Processor processor)
        {
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
            Console.WriteLine($"Found {groups.Count} group(s) [{stopwatch.ElapsedMilliseconds} ms]");
        }

        private static async Task GetSharedGroupMembershipsAsync(Processor processor)
        {
            Console.WriteLine("Enter user UPN's separated with semicolons (e.g. 'john@example.org; jane@example.org'):");
            var upns = Console.ReadLine().Split(';').Select(u => u.Trim()).ToArray();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var sharedGroups = await processor.GetSharedGroupMembershipsAsync(upns, SharedGroupMembershipType.Multiple, true);
            stopwatch.Stop();

            foreach (var sharedGroup in sharedGroups)
            {
                Console.WriteLine($"  {sharedGroup.PercentMatch.ToString("P0")}: \"{sharedGroup.Group.DisplayName}\" ({sharedGroup.Type.ToString()}: {string.Join(";", sharedGroup.UserIds)})");
            }
            Console.WriteLine($"Found {sharedGroups.Count} shared group membership(s) [{stopwatch.ElapsedMilliseconds} ms]");
        }

        private static async Task GetRecommendedGroupsAsync(Processor processor)
        {
            Console.WriteLine("Enter user UPN:");
            var userId = Console.ReadLine();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var recommendedGroups = await processor.GetRecommendedGroupsAsync(userId);
            stopwatch.Stop();

            foreach (var recommendedGroup in recommendedGroups)
            {
                Console.WriteLine($"  {recommendedGroup.Score.ToString("P0")}: \"{recommendedGroup.Group.DisplayName}\" ({recommendedGroup.Reasons.ToString()})");
            }
            Console.WriteLine($"Found {recommendedGroups.Count} recommended group(s) [{stopwatch.ElapsedMilliseconds} ms]");
        }

        private static async Task GetGroupByMailAsync(Processor processor)
        {
            Console.WriteLine("Enter group mail:");
            var mail = Console.ReadLine();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var group = await processor.GetGroupByMailAsync(mail);
            stopwatch.Stop();

            if (group == null)
            {
                Console.WriteLine($"Could not find group [{stopwatch.ElapsedMilliseconds} ms]");
            }
            else
            {
                Console.WriteLine($"Object ID            : {group.ObjectId}");
                Console.WriteLine($"Display Name         : {group.DisplayName}");
                Console.WriteLine($"Mail                 : {group.Mail}");
                Console.WriteLine($"Mail Nickname        : {group.MailNickname}");
                Console.WriteLine($"Mail Enabled         : {group.MailEnabled}");
                Console.WriteLine($"Security Enabled     : {group.SecurityEnabled}");
                Console.WriteLine($"Description          : {group.Description}");
                Console.WriteLine($"Open Discussion List : {group.IsDiscussionList}");
                Console.WriteLine($"Tags                 : {string.Join(", ", group.Tags)}");
                Console.WriteLine($"Notes                : {group.Notes}");
                Console.WriteLine($"Found group [{stopwatch.ElapsedMilliseconds} ms]");
            }
        }

        private static async Task GetGroupMembersAsync(Processor processor)
        {
            Console.WriteLine("Enter group object id:");
            var groupId = Console.ReadLine();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var groupMembers = await processor.GetGroupMembersAsync(groupId);
            stopwatch.Stop();

            var index = 0;
            foreach (var groupMember in groupMembers)
            {
                Console.WriteLine($"  {++index}: {groupMember.DisplayName} ({groupMember.UserPrincipalName})");
            }
            Console.WriteLine($"Found {groupMembers.Count} group member(s) [{stopwatch.ElapsedMilliseconds} ms]");
        }

        private static async Task SynchronizeGroupsOnceAsync(Processor processor)
        {
            await processor.SynchronizeGroupsAsync();
        }

        private static async Task SynchronizeGroupsContinuouslyAsync(ILogger logger, Processor processor, TimeSpan waitTime)
        {
            while (true)
            {
                await SynchronizeGroupsOnceAsync(processor);
                await logger.LogAsync(EventLevel.Informational, $"Waiting {waitTime} to start next group synchronization");
                await Task.Delay(waitTime);
            }
        }

        private static async Task PrimeAdalCacheAsync(ILogger logger, IPersistentStorage persistentStorage, string tenant, string clientId, Uri redirectUri)
        {
            Console.WriteLine("Enter the file name of the cache:");
            var fileName = Console.ReadLine();
            await persistentStorage.DeleteAsync(fileName);
            var cache = new PersistentStorageTokenCache(logger, persistentStorage, fileName);
            var authenticationContext = new AuthenticationContext(Constants.AadEndpoint + tenant, true, cache);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(Constants.AadGraphApiEndpoint, clientId, redirectUri, new PlatformParameters(PromptBehavior.Always));
            var token = authenticationResult.AccessToken;
        }

        #endregion
    }
}