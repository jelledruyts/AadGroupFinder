using System.Threading.Tasks;

namespace GroupFinder.Common.PersistentStorage
{
    public interface IPersistentStorage
    {
        Task<T> LoadAsync<T>(string fileName) where T : class, new();
        Task SaveAsync<T>(string fileName, T fileContents) where T : class, new();
        Task<byte[]> LoadAsync(string fileName);
        Task SaveAsync(string fileName, byte[] fileContents);
        Task DeleteAsync(string fileName);
    }
}