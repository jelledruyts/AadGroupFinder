namespace GroupFinder.Common.Models
{
    public interface IGroupSearchResult : IAnnotatedGroup
    {
        double Score { get; set; }
    }
}