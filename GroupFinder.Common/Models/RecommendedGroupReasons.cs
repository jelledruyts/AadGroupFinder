using System;

namespace GroupFinder.Common.Models
{
    [Flags]
    public enum RecommendedGroupReasons
    {
        /// <summary>
        /// The group is recommended because a number of the user's peers are member but the user is not.
        /// </summary>
        SharedGroupMembershipOfPeers = 1
    }
}