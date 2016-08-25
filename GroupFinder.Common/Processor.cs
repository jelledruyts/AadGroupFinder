using GroupFinder.Common.Aad;
using GroupFinder.Common.Logging;
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

        #endregion

        #region Groups

        public async Task<IList<SharedGroupMembership>> GetSharedGroupMembershipsAsync(IList<string> userIds, SharedGroupMembershipType minimumType, bool mailEnabledOnly)
        {
            if (userIds == null || !userIds.Any())
            {
                throw new ArgumentException($"The \"{nameof(userIds)}\" parameter is required and needs to have at least one item.", nameof(userIds));
            }
            this.logger.Log(EventLevel.Informational, $"Retrieving all group memberships for {userIds.Count} user(s)");
            var userTasks = userIds.Select(u => this.graphClient.GetDirectGroupMembershipsAsync(u, mailEnabledOnly));
            var userGroupLists = await Task.WhenAll(userTasks);

            this.logger.Log(EventLevel.Informational, "Processing shared group memberships");
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
            return sharedGroupMembershipsDictionary.Select(g => new SharedGroupMembership(g.Key, g.Value, userIds.Count)).Where(g => g.Type >= minimumType).OrderByDescending(s => s.PercentMatch).ToList();
        }

        public async Task SynchronizeGroupsAsync()
        {
            var processorState = await this.persistentStorageForState.LoadAsync<ProcessorState>(ProcessorStateFileName);
            var continuationUrl = processorState.GroupSyncContinuationUrl;
            if (processorState.LastGroupSyncStartedTime == null && processorState.LastGroupSyncCompletedTime != null)
            {
                this.logger.Log(EventLevel.Informational, $"Starting new group synchronization; last synchronization completed at {processorState.LastGroupSyncCompletedTime}");
                processorState.LastGroupSyncStartedTime = DateTimeOffset.UtcNow;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(continuationUrl))
                {
                    this.logger.Log(EventLevel.Informational, "Starting initial group synchronization");
                    processorState.LastGroupSyncStartedTime = DateTimeOffset.UtcNow;
                }
                else
                {
                    this.logger.Log(EventLevel.Informational, $"Continuing incomplete group synchronization started at {processorState.LastGroupSyncStartedTime}");
                }
            }
            Func<IList<AadGroup>, PagingState, Task<bool>> pageHandler = async (groups, state) =>
            {
                // Handle the data in the current page.
                var groupsToProcess = groups.Where(g => g.MailEnabled);
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
                    this.logger.Log(EventLevel.Informational, "Synchronization of groups complete");
                    processorState.GroupSyncContinuationUrl = state.AadDeltaLink;
                    processorState.LastGroupSyncStartedTime = null;
                    processorState.LastGroupSyncCompletedTime = DateTimeOffset.UtcNow;
                }
                await this.persistentStorageForState.SaveAsync(ProcessorStateFileName, processorState);
                return true;
            };
            await this.graphClient.VisitGroupsAsync(pageHandler, null, continuationUrl);
        }

        public Task<IAnnotatedGroup> GetGroupAsync(string objectId)
        {
            return this.searchService.GetGroupAsync(objectId);
        }

        public Task<IList<IGroupSearchResult>> FindGroupsAsync(string searchText, int top, int skip)
        {
            return this.searchService.FindGroupsAsync(searchText, top, skip);
        }

        public async Task UpdateGroupAsync(string objectId, IList<string> tags, string notes)
        {
            await this.searchService.UpdateGroupAsync(objectId, tags, notes);
            if (this.persistentStorageForBackups != null)
            {
                var backupData = new GroupBackup(objectId, tags, notes);
                await this.persistentStorageForBackups.SaveAsync($"groups/{objectId}.json", backupData);
            }
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

            public GroupBackup()
            {
            }

            public GroupBackup(string objectId, IList<string> tags, string notes)
            {
                this.ObjectId = objectId;
                this.Tags = tags;
                this.Notes = notes;
            }
        }

        #endregion
    }
}