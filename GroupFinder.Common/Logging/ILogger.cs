using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace GroupFinder.Common.Logging
{
    public interface ILogger
    {
        Task LogAsync(EventLevel level, string message);
    }
}