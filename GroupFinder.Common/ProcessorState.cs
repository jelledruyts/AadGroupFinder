using System;

namespace GroupFinder.Common
{
    public class ProcessorState
    {
        public string GroupSyncContinuationUrl { get; set; }
        public DateTimeOffset? LastGroupSyncStartedTime { get; set; }
        public DateTimeOffset? LastGroupSyncCompletedTime { get; set; }
    }
}