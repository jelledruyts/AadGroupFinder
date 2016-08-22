using System.Collections.Generic;

namespace GroupFinder.Common
{
    public interface IGroupAnnotation
    {
        IList<string> Tags { get; set; }
        string Notes { get; set; }
    }
}