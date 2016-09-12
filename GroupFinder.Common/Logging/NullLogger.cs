using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace GroupFinder.Common.Logging
{
    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger()
        {
        }

        public Task LogAsync(EventLevel level, string message)
        {
            // Do nothing.
            return Task.FromResult(0);
        }
    }
}