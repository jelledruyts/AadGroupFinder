using GroupFinder.Common;

namespace GroupFinder.Web.Models
{
    public class GroupSearchResult : AnnotatedGroup, IGroupSearchResult
    {
        public double Score { get; set; }

        public GroupSearchResult()
        {
        }

        public GroupSearchResult(IGroupSearchResult value)
            : base(value)
        {
            this.Tags = value.Tags;
            this.Notes = value.Notes;
            this.IsDiscussionList = value.IsDiscussionList;
            this.Score = value.Score;
        }
    }
}