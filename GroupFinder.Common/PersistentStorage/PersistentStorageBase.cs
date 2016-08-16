using GroupFinder.Common.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace GroupFinder.Common.PersistentStorage
{
    public abstract class PersistentStorageBase : IPersistentStorage
    {
        #region Properties

        protected ILogger Logger { get; private set; }

        #endregion

        #region Constructors

        protected PersistentStorageBase(ILogger logger)
        {
            this.Logger = logger ?? NullLogger.Instance;
        }

        #endregion

        #region IPersistentStorage Implementation

        public async Task<T> LoadAsync<T>(string fileName) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException($"The \"{nameof(fileName)}\" parameter is required.", nameof(fileName));
            }
            var fileContents = await LoadCoreAsync(fileName);
            if (fileContents != null)
            {
                this.Logger.Log(EventLevel.Verbose, $"Loaded \"{fileName}\" from persistent storage");
                return JsonConvert.DeserializeObject<T>(fileContents);
            }
            else
            {
                this.Logger.Log(EventLevel.Verbose, $"File not found at \"{fileName}\"");
                return new T();
            }
        }

        public async Task SaveAsync<T>(string fileName, T value) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException($"The \"{nameof(fileName)}\" parameter is required.", nameof(fileName));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            this.Logger.Log(EventLevel.Verbose, $"Saving \"{fileName}\" to persistent storage");
            var fileContents = JsonConvert.SerializeObject(value);
            await SaveCoreAsync(fileName, fileContents);
        }

        #endregion

        #region Abstract Methods

        protected abstract Task<string> LoadCoreAsync(string fileName);
        protected abstract Task SaveCoreAsync(string fileName, string fileContents);

        #endregion
    }
}