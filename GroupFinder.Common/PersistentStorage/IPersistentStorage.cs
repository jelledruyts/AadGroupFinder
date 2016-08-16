using System.Threading.Tasks;

namespace GroupFinder.Common.PersistentStorage
{
    public interface IPersistentStorage
    {
        Task<T> LoadAsync<T>(string fileName) where T : class, new();
        Task SaveAsync<T>(string fileName, T value) where T : class, new();
    }
}