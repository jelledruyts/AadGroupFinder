namespace GroupFinder.Common
{
    public class RecommendedGroup
    {
        public IGroup Group { get; private set; }
        public double Score { get; private set; }
        public RecommendedGroupReasons Reasons { get; private set; }

        public RecommendedGroup(IGroup group, double score, RecommendedGroupReasons reasons)
        {
            this.Group = group;
            this.Score = score;
            this.Reasons = reasons;
        }
    }
}