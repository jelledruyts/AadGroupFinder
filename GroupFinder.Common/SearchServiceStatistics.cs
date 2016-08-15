namespace GroupFinder.Common
{
    public class SearchServiceStatistics
    {
        public long DocumentCount { get; private set; }
        public long IndexSizeBytes { get; private set; }

        public SearchServiceStatistics(long documentCount, long indexSizeBytes)
        {
            this.DocumentCount = documentCount;
            this.IndexSizeBytes = indexSizeBytes;
        }
    }
}