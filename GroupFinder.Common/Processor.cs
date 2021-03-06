﻿using GroupFinder.Common.Aad;
using GroupFinder.Common.Logging;
using GroupFinder.Common.Models;
using GroupFinder.Common.PersistentStorage;
using GroupFinder.Common.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace GroupFinder.Common
{
    public class Processor
    {
        #region Fields

        private const string ProcessorStateFileName = "GroupFinder.ProcessorState.json";
        private readonly ILogger logger;
        private readonly IPersistentStorage persistentStorageForState;
        private readonly IPersistentStorage persistentStorageForBackups;
        private readonly AadGraphClient graphClient;
        private readonly ISearchService searchService;

        #endregion

        #region Constructors

        public Processor(ILogger logger, IPersistentStorage persistentStorageForState, IPersistentStorage persistentStorageForBackups, AadGraphClient graphClient, ISearchService searchService)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (persistentStorageForState == null)
            {
                throw new ArgumentNullException(nameof(persistentStorageForState));
            }
            if (graphClient == null)
            {
                throw new ArgumentNullException(nameof(graphClient));
            }
            if (searchService == null)
            {
                throw new ArgumentNullException(nameof(searchService));
            }
            this.logger = logger;
            this.persistentStorageForState = persistentStorageForState;
            this.persistentStorageForBackups = persistentStorageForBackups;
            this.graphClient = graphClient;
            this.searchService = searchService;
        }

        #endregion

        #region Users

        public Task<IList<IUser>> FindUsersAsync(string searchText, int? top)
        {
            return this.graphClient.FindUsersAsync(searchText, top, false);
        }

        public Task<IList<IGroup>> GetUserGroupsAsync(string userId)
        {
            return this.graphClient.GetDirectGroupMembershipsAsync(userId, true);
        }

        #endregion

        #region Groups

        public async Task<IList<SharedGroupMembership>> GetSharedGroupMembershipsAsync(IList<string> userIds, SharedGroupMembershipType minimumType, bool mailEnabledOnly)
        {
            if (userIds == null || !userIds.Any())
            {
                throw new ArgumentException($"The \"{nameof(userIds)}\" parameter is required and needs to have at least one item.", nameof(userIds));
            }
            await this.logger.LogAsync(EventLevel.Informational, $"Retrieving all group memberships for {userIds.Count} user(s)");
            var userTasks = userIds.Select(u => this.graphClient.GetDirectGroupMembershipsAsync(u, mailEnabledOnly));
            var userGroupLists = await Task.WhenAll(userTasks);

            await this.logger.LogAsync(EventLevel.Informational, "Processing shared group memberships");
            var sharedGroupMembershipsDictionary = new Dictionary<IGroup, IList<string>>();
            for (var i = 0; i < userIds.Count; i++)
            {
                var userId = userIds[i];
                var userGroups = userGroupLists[i];
                foreach (var userGroup in userGroups)
                {
                    var groupUserIds = default(IList<string>);
                    if (!sharedGroupMembershipsDictionary.TryGetValue(userGroup, out groupUserIds))
                    {
                        groupUserIds = new List<string>();
                        sharedGroupMembershipsDictionary[userGroup] = groupUserIds;
                    }
                    groupUserIds.Add(userId);
                }
            }
            return sharedGroupMembershipsDictionary.Select(g => new SharedGroupMembership(g.Key, g.Value, userIds.Count)).Where(g => g.Type >= minimumType).OrderByDescending(g => g.PercentMatch).ThenBy(g => g.Group.DisplayName).ToList();
        }

        public async Task<IList<RecommendedGroup>> GetRecommendedGroupsAsync(string userId)
        {
            var recommendedGroups = new List<RecommendedGroup>();

            // Get recommended groups based on the user's peers (i.e. who share the same manager).
            await this.logger.LogAsync(EventLevel.Informational, $"Getting manager for user \"{userId}\"");
            var manager = await this.graphClient.GetUserManagerAsync(userId);
            if (manager != null)
            {
                // Get the user's peers.
                await this.logger.LogAsync(EventLevel.Informational, $"Getting direct reports for manager \"{manager.UserPrincipalName}\"");
                var peers = await this.graphClient.GetDirectReportsAsync(manager.UserPrincipalName);
                var user = peers.SingleOrDefault(p => string.Equals(p.UserPrincipalName, userId, StringComparison.OrdinalIgnoreCase) || string.Equals(p.ObjectId, userId, StringComparison.OrdinalIgnoreCase));
                var peerUserIds = peers.Select(p => p.UserPrincipalName).ToArray();

                // Get shared group memberships and exclude groups that the user is already a member of.
                var sharedGroupMemberships = await GetSharedGroupMembershipsAsync(peerUserIds, SharedGroupMembershipType.Multiple, true);
                recommendedGroups.AddRange(sharedGroupMemberships
                    .Where(g => !g.UserIds.Contains(user.UserPrincipalName))
                    .Select(g => new RecommendedGroup(g.Group, g.PercentMatch, RecommendedGroupReasons.SharedGroupMembershipOfPeers))
                    .ToList());
            }
            else
            {
                var ceoRecommendedGroup = new Group
                {
                    ObjectId = "00000000-0000-0000-0000-000000000000",
                    DisplayName = "CEO Self-Help",
                    Description = "It seems you're on top of the foodchain. Whow. We could recommend reaching out to your peers in other companies - but honestly, you wouldn't be here if you hadn't already. Congratulations and keep up the good work :-)",
                    Mail = "ceo-selfhelp",
                    MailEnabled = true,
                    MailNickname = "ceo-selfhelp",
                    SecurityEnabled = false
                };
                recommendedGroups.Add(new RecommendedGroup(ceoRecommendedGroup, 1.0, RecommendedGroupReasons.SharedGroupMembershipOfPeers));
            }
            return recommendedGroups;
        }

        public async Task SynchronizeGroupsAsync()
        {
            var processorState = await this.persistentStorageForState.LoadAsync<ProcessorState>(ProcessorStateFileName);
            var continuationUrl = processorState.GroupSyncContinuationUrl;
            bool retrieveOnlyChangedProperties;
            if (processorState.LastGroupSyncStartedTime == null && processorState.LastGroupSyncCompletedTime != null)
            {
                await this.logger.LogAsync(EventLevel.Informational, $"Starting new group synchronization; last synchronization completed at {processorState.LastGroupSyncCompletedTime}");
                processorState.LastGroupSyncStartedTime = DateTimeOffset.UtcNow;
                retrieveOnlyChangedProperties = true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(continuationUrl))
                {
                    await this.logger.LogAsync(EventLevel.Informational, "Starting initial group synchronization");
                    processorState.LastGroupSyncStartedTime = DateTimeOffset.UtcNow;
                    retrieveOnlyChangedProperties = false;
                }
                else
                {
                    await this.logger.LogAsync(EventLevel.Informational, $"Continuing incomplete group synchronization started at {processorState.LastGroupSyncStartedTime}");
                    retrieveOnlyChangedProperties = processorState.LastGroupSyncCompletedTime != null;
                }
            }
            Func<IList<AadGroup>, PagingState, Task<bool>> pageHandler = async (groups, state) =>
            {
                // Handle the data in the current page.
                var groupsToProcess = groups.Where(g => g.MailEnabled.HasValue && g.MailEnabled.Value == true);
                var groupsToUpsert = groupsToProcess.Where(g => !g.IsDeleted);
                if (groupsToUpsert.Any())
                {
                    await this.searchService.UpsertGroupsAsync(groupsToUpsert);
                }
                var groupObjectIdsToDelete = groupsToProcess.Where(g => g.IsDeleted).Select(g => g.ObjectId);
                if (groupObjectIdsToDelete.Any())
                {
                    await this.searchService.DeleteGroupsAsync(groupObjectIdsToDelete);
                }

                // Update the status of the synchronization process.
                if (!string.IsNullOrWhiteSpace(state.AadNextLink))
                {
                    processorState.GroupSyncContinuationUrl = state.AadNextLink;
                }
                else if (!string.IsNullOrWhiteSpace(state.AadDeltaLink))
                {
                    await this.logger.LogAsync(EventLevel.Informational, "Synchronization of groups complete");
                    processorState.GroupSyncContinuationUrl = state.AadDeltaLink;
                    processorState.LastGroupSyncStartedTime = null;
                    processorState.LastGroupSyncCompletedTime = DateTimeOffset.UtcNow;
                }
                await this.persistentStorageForState.SaveAsync(ProcessorStateFileName, processorState);
                return true;
            };
            Action retryingHandler = () =>
            {
                // When a paging operation needs to be retried, there isn't much extra we can do.
                // Processing will re-run but eveything is idempotent so no state needs to be reset.
            };
            var additionalHeaders = new Dictionary<string, string>();
            if (retrieveOnlyChangedProperties)
            {
                additionalHeaders["ocp-aad-dq-include-only-changed-properties"] = "true";
            }
            await this.graphClient.VisitGroupsAsync(pageHandler, null, retryingHandler, continuationUrl, additionalHeaders);
        }

        public Task<IAnnotatedGroup> GetGroupAsync(string objectId)
        {
            return this.searchService.GetGroupAsync(objectId);
        }

        public Task<IAnnotatedGroup> GetGroupByMailAsync(string mail)
        {
            return this.searchService.GetGroupByMailAsync(mail);
        }

        public Task<IList<IGroupSearchResult>> FindGroupsAsync(string searchText, int top, int skip)
        {
            return this.searchService.FindGroupsAsync(searchText, top, skip);
        }

        public async Task UpdateGroupAsync(string objectId, IList<string> tags, string notes, bool isDiscussionList)
        {
            await this.searchService.UpdateGroupAsync(objectId, tags, notes, isDiscussionList);
            if (this.persistentStorageForBackups != null)
            {
                var backupData = new GroupBackup(objectId, tags, notes, isDiscussionList);
                await this.persistentStorageForBackups.SaveAsync($"groups/{objectId}.json", backupData);
            }
        }

        public Task<IList<IUser>> GetGroupMembersAsync(string objectId)
        {
            return this.graphClient.GetGroupMembersAsync(objectId);
        }

        #endregion

        #region Service Status

        public async Task<ServiceStatus> GetServiceStatusAsync()
        {
            var searchStatistics = await this.searchService.GetStatisticsAsync();
            var processorState = await this.persistentStorageForState.LoadAsync<ProcessorState>(ProcessorStateFileName);
            return new ServiceStatus(searchStatistics.DocumentCount, searchStatistics.IndexSizeBytes, processorState.LastGroupSyncStartedTime, processorState.LastGroupSyncCompletedTime);
        }

        #endregion

        #region GroupBackup Class

        private class GroupBackup : IGroupAnnotation
        {
            public string ObjectId { get; set; }
            public IList<string> Tags { get; set; }
            public string Notes { get; set; }
            public bool IsDiscussionList { get; set; }

            public GroupBackup()
            {
            }

            public GroupBackup(string objectId, IList<string> tags, string notes, bool isDiscussionList)
            {
                this.ObjectId = objectId;
                this.Tags = tags;
                this.Notes = notes;
                this.IsDiscussionList = isDiscussionList;
            }
        }

        #endregion
    }
}