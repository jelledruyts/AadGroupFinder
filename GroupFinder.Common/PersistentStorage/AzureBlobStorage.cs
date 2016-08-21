using GroupFinder.Common.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GroupFinder.Common.PersistentStorage
{
    public class AzureBlobStorage : PersistentStorageBase
    {
        #region Fields

        private readonly string containerName;
        private readonly CloudBlobClient blobClient;
        private CloudBlobContainer blobContainer;

        #endregion

        #region Constructors

        public AzureBlobStorage(ILogger logger, string accountName, string containerName, string accessKey)
            : base(logger)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException($"The \"{nameof(accountName)}\" parameter is required.", nameof(accountName));
            }
            if (string.IsNullOrWhiteSpace(accessKey))
            {
                throw new ArgumentException($"The \"{nameof(accessKey)}\" parameter is required.", nameof(accessKey));
            }
            this.containerName = containerName;
            var credentials = new StorageCredentials(accountName, accessKey);
            var account = new CloudStorageAccount(credentials, true);
            this.blobClient = account.CreateCloudBlobClient();
        }

        #endregion

        #region Overrides

        protected override async Task<byte[]> LoadCoreAsync(string fileName)
        {
            await EnsureInitialized();
            var blob = this.blobContainer.GetBlockBlobReference(fileName);
            var exists = await blob.ExistsAsync();
            if (exists)
            {
                using (var stream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(stream);
                    return stream.GetBuffer();
                }
            }
            return null;
        }

        protected override async Task SaveCoreAsync(string fileName, byte[] fileContents)
        {
            await EnsureInitialized();
            var blob = this.blobContainer.GetBlockBlobReference(fileName);
            await blob.UploadFromByteArrayAsync(fileContents, 0, fileContents.Length);
        }

        protected override async Task DeleteCoreAsync(string fileName)
        {
            await EnsureInitialized();
            var blob = this.blobContainer.GetBlockBlobReference(fileName);
            await blob.DeleteIfExistsAsync();
        }

        #endregion

        #region Helper Methods

        private async Task EnsureInitialized()
        {
            if (this.blobContainer == null)
            {
                this.blobContainer = this.blobClient.GetContainerReference(this.containerName);
                await this.blobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null);
            }
        }

        #endregion
    }
}