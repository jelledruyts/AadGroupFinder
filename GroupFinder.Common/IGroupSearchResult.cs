using System.Collections.Generic;

namespace GroupFinder.Common
{
    public interface IGroupSearchResult : IGroup
    {
        double Score { get; set; }
        IList<string> Tags { get; set; }
    }
}