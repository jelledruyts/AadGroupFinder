using System.Collections.Generic;

namespace GroupFinder.Common
{
    public class SharedGroupMembership
    {
        public AadGroup Group { get; private set; }
        public IList<string> UserIds { get; private set; }
        public double PercentMatch { get; private set; }

        public SharedGroupMembership(AadGroup group, IList<string> memberUsers, int requestedUserCount)
        {
            this.Group = group;
            this.UserIds = memberUsers;
            this.PercentMatch = (double)memberUsers.Count / requestedUserCount;
        }
    }
}