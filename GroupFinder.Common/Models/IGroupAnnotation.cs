using System.Collections.Generic;

namespace GroupFinder.Common.Models
{
    public interface IGroupAnnotation
    {
        IList<string> Tags { get; set; }
        string Notes { get; set; }
        bool IsDiscussionList { get; set; }
    }
}