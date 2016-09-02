using System;
using System.Collections.Generic;

namespace GroupFinder.Web.Models
{
    public class RecommendedGroup
    {
        public Group Group { get; set; }
        public double Score { get; set; }
        public IEnumerable<string> Reasons { get; set; }

        public RecommendedGroup()
        {
        }

        public RecommendedGroup(GroupFinder.Common.RecommendedGroup value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            this.Group = value.Group.Map();
            this.Score = value.Score;
            this.Reasons = value.Reasons.ToString().Split(',');
        }
    }
}