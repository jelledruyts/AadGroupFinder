using System.Threading.Tasks;

namespace GroupFinder.Common.Security
{
    public interface ITokenProvider
    {
        Task<string> GetAccessTokenAsync();
    }
}