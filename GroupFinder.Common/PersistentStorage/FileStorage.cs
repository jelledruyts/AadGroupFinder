using GroupFinder.Common.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading.Tasks;

namespace GroupFinder.Common.PersistentStorage
{
    public class FileStorage : IPersistentStorage
    {
        private readonly ILogger logger;

        public FileStorage(ILogger logger)
        {
            this.logger = logger ?? NullLogger.Instance;
        }

        public Task<T> LoadAsync<T>(string fileName) where T : class, new()
        {
            var value = default(T);
            if (File.Exists(fileName))
            {
                this.logger.Log(EventLevel.Verbose, $"Loading \"{fileName}\" from disk");
                value = JsonConvert.DeserializeObject<T>(File.ReadAllText(fileName));
            }
            else
            {
                this.logger.Log(EventLevel.Verbose, $"File not found at \"{fileName}\"; initializing new value");
                value = new T();
            }
            return Task.FromResult(value);

        }

        public Task SaveAsync<T>(string fileName, T value) where T : class, new()
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            this.logger.Log(EventLevel.Verbose, $"Saving \"{fileName}\" to disk");
            File.WriteAllText(fileName, JsonConvert.SerializeObject(value));
            return Task.FromResult(0);
        }
    }
}