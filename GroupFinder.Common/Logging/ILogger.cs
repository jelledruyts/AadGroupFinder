using System.Diagnostics.Tracing;

namespace GroupFinder.Common.Logging
{
    public interface ILogger
    {
        void Log(EventLevel level, string message);
    }
}