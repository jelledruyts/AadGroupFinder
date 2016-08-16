using System;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace GroupFinder.Common
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
            return string.Format(CultureInfo.CurrentCulture, "[{0}] [{1,-13}] {2}", DateTime.Now.ToString(), level.ToString(), message);
        }
    }
}