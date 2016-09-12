using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace GroupFinder.Common.Logging
{
    public class DebugLogger : LoggerBase
    {
        public DebugLogger(EventLevel level)
            : base(level)
        {
        }

        protected override Task LogCoreAsync(EventLevel level, string message)
        {
            Debug.WriteLine(GetFormattedMessage(level, message));
            return Task.FromResult(0);
        }
    }
}