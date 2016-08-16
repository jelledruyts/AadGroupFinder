using GroupFinder.Common;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace GroupFinder.ConsoleClient
{
    public class TraceLogger : LoggerBase
    {
        public TraceLogger(EventLevel minimumLogLevel)
            : base(minimumLogLevel)
        {
        }

        protected override void LogCore(EventLevel level, string message)
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
        }
    }
}