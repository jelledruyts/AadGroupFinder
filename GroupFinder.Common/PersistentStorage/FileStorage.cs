using GroupFinder.Common.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GroupFinder.Common.PersistentStorage
{
    public class FileStorage : PersistentStorageBase
    {
        private string basePath;

        public FileStorage(ILogger logger)
            : this(logger, null)
        {
        }

        public FileStorage(ILogger logger, string basePath)
            : base(logger)
        {
            this.basePath = string.IsNullOrWhiteSpace(basePath) ? Environment.CurrentDirectory : basePath;
        }

        protected override Task<byte[]> LoadCoreAsync(string fileName)
        {
            fileName = Path.Combine(basePath, fileName);
            var fileContents = default(byte[]);
            if (File.Exists(fileName))
            {
                fileContents = File.ReadAllBytes(fileName);
            }
            return Task.FromResult(fileContents);
        }

        protected override Task SaveCoreAsync(string fileName, byte[] fileContents)
        {
            fileName = Path.Combine(basePath, fileName);
            File.WriteAllBytes(fileName, fileContents);
            return Task.FromResult(0);
        }

        protected override Task DeleteCoreAsync(string fileName)
        {
            fileName = Path.Combine(basePath, fileName);
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            return Task.FromResult(0);
        }
    }
}