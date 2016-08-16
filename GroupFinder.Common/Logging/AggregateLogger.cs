using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace GroupFinder.Common.Logging
{
    public class AggregateLogger : ILogger
    {
        private readonly IList<ILogger> loggers;

        public AggregateLogger(IList<ILogger> loggers)
        {
            this.loggers = loggers ?? new ILogger[0];
        }

        public void Log(EventLevel level, string message)
        {
            foreach (var logger in this.loggers)
            {
                logger.Log(level, message);
            }
        }
    }
}