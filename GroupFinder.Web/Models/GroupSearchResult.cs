using GroupFinder.Common.Models;

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
            this.Score = value.Score;
        }
    }
}