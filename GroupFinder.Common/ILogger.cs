using System.Diagnostics.Tracing;

namespace GroupFinder.Common
{
    public interface ILogger
    {
        void Log(EventLevel level, string message);
    }
}