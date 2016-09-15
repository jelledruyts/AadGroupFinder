using System.Collections.Generic;

namespace GroupFinder.Common.Models
{
    public class SharedGroupMembership
    {
        public IGroup Group { get; private set; }
        public IList<string> UserIds { get; private set; }
        public SharedGroupMembershipType Type { get; private set; }
        public double PercentMatch { get; private set; }

        public SharedGroupMembership(IGroup group, IList<string> userIds, int requestedUserCount)
        {
            this.Group = group;
            this.UserIds = userIds ?? new string[0];
            this.Type = (this.UserIds.Count == requestedUserCount ? SharedGroupMembershipType.All : (this.UserIds.Count == 1 ? SharedGroupMembershipType.Single : SharedGroupMembershipType.Multiple));
            this.PercentMatch = (double)this.UserIds.Count / requestedUserCount;
        }
    }
}