using GroupFinder.Common;
using System.Collections.Generic;

namespace GroupFinder.Web.Models
{
    public class AnnotatedGroup : Group, IAnnotatedGroup
    {
        public IList<string> Tags { get; set; }
        public string Notes { get; set; }
        public bool IsDiscussionList { get; set; }

        public AnnotatedGroup()
        {
        }

        public AnnotatedGroup(IAnnotatedGroup value)
            : base(value)
        {
            this.Tags = value.Tags;
            this.Notes = value.Notes;
            this.IsDiscussionList = value.IsDiscussionList;
        }
    }
}