using System;
using System.Collections.Generic;

namespace GroupFinder.Web.Models
{
    public class SharedGroupMembership
    {
        public Group Group { get; set; }
        public IList<string> UserIds { get; set; }
        public string Type { get; set; }
        public double PercentMatch { get; set; }

        public SharedGroupMembership()
        {
        }

        public SharedGroupMembership(GroupFinder.Common.Models.SharedGroupMembership value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            this.Group = value.Group.Map();
            this.UserIds = value.UserIds;
            this.Type = value.Type.ToString();
            this.PercentMatch = value.PercentMatch;
        }
    }
}