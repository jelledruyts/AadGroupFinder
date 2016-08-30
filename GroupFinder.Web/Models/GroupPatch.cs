using System.Collections.Generic;

namespace GroupFinder.Web.Models
{
    public class GroupPatch
    {
        public string Notes { get; set; }
        public IList<string> Tags { get; set; }
        public bool IsDiscussionList { get; set; }
    }
}