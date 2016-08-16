using System.Collections.Generic;
using System.Threading.Tasks;

namespace GroupFinder.Common.Search
{
    public interface ISearchService
    {
        Task InitializeAsync();
        Task<SearchServiceStatistics> GetStatisticsAsync();
        Task UpsertGroupsAsync(IEnumerable<IGroup> groups);
        Task DeleteGroupsAsync(IEnumerable<string> objectId);
        Task<IList<IGroupSearchResult>> FindGroupsAsync(string searchText, int pageSize, int pageIndex);
        Task UpdateGroupAsync(string objectId, IList<string> tags);
    }
}