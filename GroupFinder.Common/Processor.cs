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
        private readonly IPersistentStorage persistentStorage;
        private readonly AadGraphClient graphClient;
        private readonly ISearchService searchService;

        #endregion

        #region Constructors

        public Processor(ILogger logger, IPersistentStorage persistentStorage, AadGraphClient graphClient, ISearchService searchService)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (persistentStorage == null)
            {
                throw new ArgumentNullException(nameof(persistentStorage));
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
            this.persistentStorage = persistentStorage;
            this.graphClient = graphClient;
            this.searchService = searchService;
        }

        #endregion

        #region Users

        public Task<IList<IUser>> FindUsersAsync(string searchText)
        {
            return this.graphClient.FindUsersAsync(searchText, false);
        }

        #endregion

        #region Groups

        public async Task<IList<SharedGroupMembership>> FindSharedGroupMembershipsAsync(IList<string> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                throw new ArgumentException($"The \"{nameof(userIds)}\" parameter is required and needs to have at least one item.", nameof(userIds));
            }
            this.logger.Log(EventLevel.Informational, $"Retrieving all group memberships for {userIds.Count} user(s)");
            var userTasks = userIds.Select(u => this.graphClient.GetDirectGroupMembershipsAsync(u));
            var userGroupLists = await Task.WhenAll(userTasks);

            this.logger.Log(EventLevel.Informational, "Processing shared group memberships");
            var sharedGroupMemberships = new Dictionary<IGroup, IList<string>>();
            for (var i = 0; i < userIds.Count; i++)
            {
                var userId = userIds[i];
                var userGroups = userGroupLists[i];
                foreach (var userGroup in userGroups)
                {
                    var groupUserIds = default(IList<string>);
                    if (!sharedGroupMemberships.TryGetValue(userGroup, out groupUserIds))
                    {
                        groupUserIds = new List<string>();
                        sharedGroupMemberships[userGroup] = groupUserIds;
                    }
                    groupUserIds.Add(userId);
                }
            }
            return sharedGroupMemberships.Select(g => new SharedGroupMembership(g.Key, g.Value, userIds.Count)).OrderByDescending(s => s.PercentMatch).ToList();
        }

        public async Task SynchronizeGroupsAsync()
        {
            var processorState = await this.persistentStorage.LoadAsync<ProcessorState>(ProcessorStateFileName);
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
            Func<IList<AadGroup>, PagingState, Task> pageHandler = async (groups, state) =>
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
                await this.persistentStorage.SaveAsync(ProcessorStateFileName, processorState);
            };
            await this.graphClient.VisitGroupsAsync(pageHandler, null, continuationUrl);
        }

        public Task<IList<IGroupSearchResult>> FindGroupsAsync(string searchText, int pageSize, int pageIndex)
        {
            return this.searchService.FindGroupsAsync(searchText, pageSize, pageIndex);
        }

        public Task UpdateGroupAsync(string objectId, IList<string> tags)
        {
            return this.searchService.UpdateGroupAsync(objectId, tags);
        }

        #endregion

        #region Service Status

        public async Task<ServiceStatus> GetServiceStatusAsync()
        {
            var searchStatistics = await this.searchService.GetStatisticsAsync();
            var processorState = await this.persistentStorage.LoadAsync<ProcessorState>(ProcessorStateFileName);
            return new ServiceStatus(searchStatistics, processorState.LastGroupSyncStartedTime, processorState.LastGroupSyncCompletedTime);
        }

        #endregion
    }
}