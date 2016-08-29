namespace GroupFinder.Common.Aad
{
    public class PagingState
    {
        public int TotalObjectCount { get; set; }
        public int ProcessedObjectCount { get; set; }
        public string ODataNextLink { get; set; }
        public string AadNextLink { get; set; }
        public string AadDeltaLink { get; set; }
    }
}