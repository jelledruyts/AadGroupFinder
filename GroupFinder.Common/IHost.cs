using System.Threading.Tasks;

namespace GroupFinder.Common
{
    public interface IHost : ILogger
    {
        Task<ProcessorState> GetProcessorStateAsync();
        Task SaveProcessorStateAsync(ProcessorState state);
    }
}