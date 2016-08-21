using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace GroupFinder.Common.Logging
{
    public class DebugLogger : LoggerBase
    {
        public DebugLogger(EventLevel level)
            : base(level)
        {
        }

        protected override void LogCore(EventLevel level, string message)
        {
            Debug.WriteLine(GetFormattedMessage(level, message));
        }
    }
}