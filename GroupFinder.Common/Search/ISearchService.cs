using GroupFinder.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GroupFinder.Common.Search
{
    public interface ISearchService
    {
        Task InitializeAsync();
        Task<SearchServiceStatistics> GetStatisticsAsync();
        Task UpsertGroupsAsync(IEnumerable<IPartialGroup> groups);
        Task DeleteGroupsAsync(IEnumerable<string> objectId);
        Task<IAnnotatedGroup> GetGroupAsync(string objectId);
        Task<IList<IGroupSearchResult>> FindGroupsAsync(string searchText, int top, int skip);
        Task UpdateGroupAsync(string objectId, IList<string> tags, string notes, bool isDiscussionList);
    }
}