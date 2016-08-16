using System.Collections.Generic;

namespace GroupFinder.Common.Search
{
    internal class SearchGroup : IGroupSearchResult
    {
        // Search-specific properties.
        public double Score { get; set; }
        public IList<string> Tags { get; set; }

        // Base properties.
        public string ObjectId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Mail { get; set; }
        public bool MailEnabled { get; set; }
        public string MailNickname { get; set; }
        public bool SecurityEnabled { get; set; }
    }
}