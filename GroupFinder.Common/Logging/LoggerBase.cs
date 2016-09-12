using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace GroupFinder.Common.Logging
{
    public abstract class LoggerBase : ILogger
    {
        private readonly EventLevel minimumLogLevel;

        protected LoggerBase()
            : this(EventLevel.Verbose)
        {
        }

        protected LoggerBase(EventLevel minimumLogLevel)
        {
            this.minimumLogLevel = minimumLogLevel;
        }

        public Task LogAsync(EventLevel level, string message)
        {
            if (level <= this.minimumLogLevel)
            {
                return LogCoreAsync(level, message);
            }
            return Task.FromResult(0);
        }

        protected abstract Task LogCoreAsync(EventLevel level, string message);

        protected string GetFormattedMessage(EventLevel level, string message)
        {
            return string.Format(CultureInfo.CurrentCulture, "[{0}] [T{1:D2}] [{2,-13}] {3}", DateTime.Now.ToString(), Thread.CurrentThread.ManagedThreadId, level.ToString(), message);
        }
    }
}