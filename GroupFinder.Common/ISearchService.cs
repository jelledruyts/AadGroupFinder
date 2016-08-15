using System.Collections.Generic;
using System.Threading.Tasks;

namespace GroupFinder.Common
{
    public interface ISearchService
    {
        Task InitializeAsync();
        Task<SearchServiceStatistics> GetStatisticsAsync();
        Task UpsertGroupsAsync(IEnumerable<AadGroup> groups);
        Task DeleteGroupsAsync(IEnumerable<string> objectId);
        Task<IList<SearchGroup>> FindGroupsAsync(string searchText, int pageSize, int pageIndex);
        Task UpdateGroupAsync(string objectId, IList<string> tags);
    }
}