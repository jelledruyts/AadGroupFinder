using GroupFinder.Common;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace GroupFinder.ConsoleClient
{
    public class TraceLogger : ILogger
    {
        public void Log(EventLevel level, string message)
        {
            switch (level)
            {
                case EventLevel.LogAlways:
                case EventLevel.Critical:
                case EventLevel.Error:
                    Trace.TraceError(message);
                    break;
                case EventLevel.Warning:
                    Trace.TraceWarning(message);
                    break;
                case EventLevel.Informational:
                case EventLevel.Verbose:
                default:
                    Trace.TraceInformation(message);
                    break;
            }
        }
    }
}