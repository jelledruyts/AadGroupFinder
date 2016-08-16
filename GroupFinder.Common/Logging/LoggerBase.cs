using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Threading;

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

        public void Log(EventLevel level, string message)
        {
            if (level <= this.minimumLogLevel)
            {
                LogCore(level, message);
            }
        }

        protected abstract void LogCore(EventLevel level, string message);

        protected string GetFormattedMessage(EventLevel level, string message)
        {
            return string.Format(CultureInfo.CurrentCulture, "[{0}] [T{1:D2}] [{2,-13}] {3}", DateTime.Now.ToString(), Thread.CurrentThread.ManagedThreadId, level.ToString(), message);
        }
    }
}