using System.Collections.Generic;

namespace GroupFinder.Common
{
    public class SharedGroupMembership
    {
        public IGroup Group { get; private set; }
        public IList<string> UserIds { get; private set; }
        public double PercentMatch { get; private set; }

        public SharedGroupMembership(IGroup group, IList<string> memberUsers, int requestedUserCount)
        {
            this.Group = group;
            this.UserIds = memberUsers;
            this.PercentMatch = (double)memberUsers.Count / requestedUserCount;
        }
    }
}