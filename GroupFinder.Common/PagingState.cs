namespace GroupFinder.Common
{
    public class PagingState
    {
        public string LastVisitedUrl { get; set; }
        public int TotalObjectCount { get; set; }
        public int MatchedObjectCount { get; set; }
        public int PageCount { get; set; }
        public string ODataNextLink { get; set; }
        public string AadNextLink { get; set; }
        public string AadDeltaLink { get; set; }
    }
}