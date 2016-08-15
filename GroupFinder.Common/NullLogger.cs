﻿using System.Diagnostics.Tracing;

namespace GroupFinder.Common
{
    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger()
        {
        }

        public void Log(EventLevel level, string message)
        {
            // Do nothing.
        }
    }
}