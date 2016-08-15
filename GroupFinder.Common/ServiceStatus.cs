using System;

namespace GroupFinder.Common
{
    public class ServiceStatus
    {
        public SearchServiceStatistics SearchServiceStatistics { get; private set; }
        public DateTimeOffset? LastGroupSyncCompletedTime { get; private set; }

        public ServiceStatus(SearchServiceStatistics searchServiceStatistics, DateTimeOffset? lastGroupSyncCompletedTime)
        {
            this.SearchServiceStatistics = searchServiceStatistics;
            this.LastGroupSyncCompletedTime = lastGroupSyncCompletedTime;
        }
    }
}