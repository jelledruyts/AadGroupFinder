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

        protected override Task<string> LoadCoreAsync(string fileName)
        {
            var fileContents = default(string);
            if (File.Exists(fileName))
            {
                fileContents = File.ReadAllText(fileName);
            }
            return Task.FromResult(fileContents);
        }

        protected override Task SaveCoreAsync(string fileName, string fileContents)
        {
            File.WriteAllText(fileName, fileContents);
            return Task.FromResult(0);
        }
    }
}