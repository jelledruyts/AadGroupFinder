using GroupFinder.Common.Logging;
using System.IO;
using System.Threading.Tasks;

namespace GroupFinder.Common.PersistentStorage
{
    public class FileStorage : PersistentStorageBase
    {
        public FileStorage(ILogger logger)
            : base(logger)
        {
        }

        protected override Task<byte[]> LoadCoreAsync(string fileName)
        {
            var fileContents = default(byte[]);
            if (File.Exists(fileName))
            {
                fileContents = File.ReadAllBytes(fileName);
            }
            return Task.FromResult(fileContents);
        }

        protected override Task SaveCoreAsync(string fileName, byte[] fileContents)
        {
            File.WriteAllBytes(fileName, fileContents);
            return Task.FromResult(0);
        }

        protected override Task DeleteCoreAsync(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            return Task.FromResult(0);
        }
    }
}