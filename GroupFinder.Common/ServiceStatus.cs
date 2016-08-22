using System;

namespace GroupFinder.Common
{
    public class ServiceStatus
    {
        public long GroupCount { get; private set; }
        public long GroupSearchIndexSizeBytes { get; private set; }
        public DateTimeOffset? LastGroupSyncStartedTime { get; private set; }
        public DateTimeOffset? LastGroupSyncCompletedTime { get; private set; }

        public ServiceStatus(long groupCount, long groupSearchIndexSizeBytes, DateTimeOffset? lastGroupSyncStartedTime, DateTimeOffset? lastGroupSyncCompletedTime)
        {
            this.GroupCount = groupCount;
            this.GroupSearchIndexSizeBytes = groupSearchIndexSizeBytes;
            this.LastGroupSyncStartedTime = lastGroupSyncStartedTime;
            this.LastGroupSyncCompletedTime = lastGroupSyncCompletedTime;
        }
    }
}