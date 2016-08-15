using GroupFinder.Common;
using System;
using System.Diagnostics.Tracing;

namespace GroupFinder.ConsoleClient
{
    public class ConsoleLogger : ILogger
    {
        private readonly EventLevel minimumLevel;

        public ConsoleLogger()
            : this(EventLevel.Verbose)
        {
        }

        public ConsoleLogger(EventLevel minimumLevel)
        {
            this.minimumLevel = minimumLevel;
        }

        public void Log(EventLevel level, string message)
        {
            if (level <= this.minimumLevel)
            {
                Write(GetConsoleColor(level), message + Environment.NewLine);
            }
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