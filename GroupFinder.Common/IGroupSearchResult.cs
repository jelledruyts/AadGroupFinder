namespace GroupFinder.Common
{
    public interface IGroupSearchResult : IAnnotatedGroup
    {
        double Score { get; set; }
    }
}