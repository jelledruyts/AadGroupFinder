using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace GroupFinder.Common.Logging
{
    public class AggregateLogger : ILogger
    {
        private readonly IList<ILogger> loggers;

        public AggregateLogger(IList<ILogger> loggers)
        {
            this.loggers = loggers ?? new ILogger[0];
        }

        public Task LogAsync(EventLevel level, string message)
        {
            var tasks = this.loggers.Select(l => l.LogAsync(level, message));
            return Task.WhenAll(tasks);
        }
    }
}