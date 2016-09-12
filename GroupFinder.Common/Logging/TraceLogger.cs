using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace GroupFinder.Common.Logging
{
    public class TraceLogger : LoggerBase
    {
        public TraceLogger(EventLevel minimumLogLevel)
            : base(minimumLogLevel)
        {
        }

        protected override Task LogCoreAsync(EventLevel level, string message)
        {
            var formattedMessage = GetFormattedMessage(level, message);
            switch (level)
            {
                case EventLevel.LogAlways:
                case EventLevel.Critical:
                case EventLevel.Error:
                    Trace.TraceError(formattedMessage);
                    break;
                case EventLevel.Warning:
                    Trace.TraceWarning(formattedMessage);
                    break;
                case EventLevel.Informational:
                case EventLevel.Verbose:
                default:
                    Trace.TraceInformation(formattedMessage);
                    break;
            }
            return Task.FromResult(0);
        }
    }
}