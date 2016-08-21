﻿using GroupFinder.Common.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics.Tracing;
using System.Text;
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
            var fileContentsRaw = await LoadAsync(fileName);
            if (fileContentsRaw != null)
            {
                var fileContentsJson = Encoding.UTF8.GetString(fileContentsRaw);
                return JsonConvert.DeserializeObject<T>(fileContentsJson);
            }
            else
            {
                this.Logger.Log(EventLevel.Verbose, $"File not found at \"{fileName}\", creating new instance");
                return new T();
            }
        }

        public Task SaveAsync<T>(string fileName, T fileContents) where T : class, new()
        {
            var fileContentsJson = JsonConvert.SerializeObject(fileContents);
            var fileContentsRaw = Encoding.UTF8.GetBytes(fileContentsJson);
            return SaveAsync(fileName, fileContentsRaw);
        }

        public Task<byte[]> LoadAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException($"The \"{nameof(fileName)}\" parameter is required.", nameof(fileName));
            }
            this.Logger.Log(EventLevel.Verbose, $"Loading \"{fileName}\" from persistent storage");
            return LoadCoreAsync(fileName);
        }

        public Task SaveAsync(string fileName, byte[] fileContents)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException($"The \"{nameof(fileName)}\" parameter is required.", nameof(fileName));
            }
            if (fileContents == null)
            {
                throw new ArgumentNullException(nameof(fileContents));
            }
            this.Logger.Log(EventLevel.Verbose, $"Saving \"{fileName}\" to persistent storage");
            return SaveCoreAsync(fileName, fileContents);
        }

        public Task DeleteAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException($"The \"{nameof(fileName)}\" parameter is required.", nameof(fileName));
            }
            this.Logger.Log(EventLevel.Verbose, $"Deleting \"{fileName}\" from persistent storage");
            return DeleteCoreAsync(fileName);
        }

        #endregion

        #region Abstract Methods

        protected abstract Task<byte[]> LoadCoreAsync(string fileName);
        protected abstract Task SaveCoreAsync(string fileName, byte[] fileContents);
        protected abstract Task DeleteCoreAsync(string fileName);

        #endregion
    }
}