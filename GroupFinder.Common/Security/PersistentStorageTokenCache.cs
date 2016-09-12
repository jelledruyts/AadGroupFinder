using GroupFinder.Common.Logging;
using GroupFinder.Common.PersistentStorage;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace GroupFinder.Common.Security
{
    public class PersistentStorageTokenCache : TokenCache
    {
        private readonly string fileName;
        private readonly ILogger logger;
        private readonly IPersistentStorage persistentStorage;

        public PersistentStorageTokenCache(ILogger logger, IPersistentStorage persistentStorage, string fileName)
        {
            if (persistentStorage == null)
            {
                throw new ArgumentNullException(nameof(persistentStorage));
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException($"The \"{nameof(fileName)}\" parameter is required.", nameof(fileName));
            }
            this.logger = logger ?? NullLogger.Instance;
            this.persistentStorage = persistentStorage;
            this.fileName = fileName;
            this.AfterAccess = AfterAccessNotification;
        }

        public async Task LoadAsync()
        {
            await this.logger.LogAsync(EventLevel.Verbose, $"Loading token cache from \"{this.fileName}\" file in persistent storage");
            var cacheData = await this.persistentStorage.LoadAsync(this.fileName);
            this.Deserialize(cacheData);
        }

        public override void Clear()
        {
            base.Clear();
            // Do not "await" the logging to complete.
            this.logger.LogAsync(EventLevel.Verbose, $"Deleting token cache from \"{this.fileName}\" file in persistent storage");
            this.persistentStorage.DeleteAsync(this.fileName).Wait();
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (this.HasStateChanged)
            {
                this.logger.LogAsync(EventLevel.Verbose, $"Saving token cache to \"{this.fileName}\" file in persistent storage");
                var state = this.Serialize();
                this.persistentStorage.SaveAsync(this.fileName, state).Wait();
            }
        }
    }
}