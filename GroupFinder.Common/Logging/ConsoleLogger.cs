using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace GroupFinder.Common.Logging
{
    public class ConsoleLogger : LoggerBase
    {
        public ConsoleLogger(EventLevel minimumLogLevel)
            : base(minimumLogLevel)
        {
        }

        protected override Task LogCoreAsync(EventLevel level, string message)
        {
            Write(GetConsoleColor(level), GetFormattedMessage(level, message) + Environment.NewLine);
            return Task.FromResult(0);
        }

        private static void Write(ConsoleColor color, string message)
        {
            var oldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.Write(message);
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }

        private static ConsoleColor GetConsoleColor(EventLevel level)
        {
            switch (level)
            {
                case EventLevel.Verbose:
                    return ConsoleColor.DarkGray;
                case EventLevel.Warning:
                    return ConsoleColor.Yellow;
                case EventLevel.Error:
                case EventLevel.Critical:
                case EventLevel.LogAlways:
                    return ConsoleColor.Red;
                default:
                    return ConsoleColor.Gray;
            }
        }
    }
}