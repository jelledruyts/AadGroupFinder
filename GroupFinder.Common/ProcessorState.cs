using System;

namespace GroupFinder.Common
{
    public class ProcessorState
    {
        public string GroupSyncContinuationUrl { get; set; }
        public bool LastGroupSyncCompleted { get; set; }
        public DateTimeOffset? LastGroupSyncCompletedTime { get; set; }
        public string StatusMessage { get; set; }
    }
}