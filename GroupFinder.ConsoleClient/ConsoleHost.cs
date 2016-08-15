using GroupFinder.Common;
using Newtonsoft.Json;
using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GroupFinder.ConsoleClient
{
    public class ConsoleHost : IHost
    {
        private readonly ConsoleLogger consoleLogger;
        private readonly TraceLogger traceLogger;
        private readonly string processorStateFilePath = "GroupFinder.ConsoleClient.ProcessorState.json";

        public ConsoleHost(EventLevel minimumConsoleLevel)
        {
            this.consoleLogger = new ConsoleLogger(minimumConsoleLevel);
            this.traceLogger = new TraceLogger();
        }

        public void Log(EventLevel level, string message)
        {
            var formattedMessage = GetFormattedMessage(level, message);
            this.consoleLogger.Log(level, formattedMessage);
            this.traceLogger.Log(level, formattedMessage);
        }

        private static string GetFormattedMessage(EventLevel level, string message)
        {
            return string.Format(CultureInfo.CurrentCulture, "[{0}] [T{1:D2}] [{2,-13}] {3}", DateTime.Now.ToString(), Thread.CurrentThread.ManagedThreadId, level.ToString(), message);
        }

        public Task<ProcessorState> GetProcessorStateAsync()
        {
            var state = default(ProcessorState);
            if (File.Exists(this.processorStateFilePath))
            {
                this.Log(EventLevel.Verbose, $"Loading processor state from \"{this.processorStateFilePath}\"");
                state = JsonConvert.DeserializeObject<ProcessorState>(File.ReadAllText(this.processorStateFilePath));
            }
            else
            {
                this.Log(EventLevel.Verbose, $"Processor state file not found at \"{this.processorStateFilePath}\"; initializing new processor state");
                state = new ProcessorState();
            }
            return Task.FromResult(state);
        }

        public Task SaveProcessorStateAsync(ProcessorState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            this.Log(EventLevel.Verbose, $"Saving processor state to \"{this.processorStateFilePath}\"");
            File.WriteAllText(this.processorStateFilePath, JsonConvert.SerializeObject(state));
            return Task.FromResult(0);
        }
    }
}