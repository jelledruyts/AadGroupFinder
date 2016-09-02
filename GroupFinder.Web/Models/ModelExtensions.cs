using GroupFinder.Common;
using System.Collections.Generic;
using System.Linq;

namespace GroupFinder.Web.Models
{
    public static class ModelExtensions
    {
        #region Mapping

        // Some mapping framework would probably be nicer here, but it's small and easy enough here to avoid external dependencies.

        public static GroupSearchResult Map(this IGroupSearchResult value)
        {
            return new GroupSearchResult(value);
        }

        public static IEnumerable<GroupSearchResult> Map(this IList<IGroupSearchResult> value)
        {
            return value?.Select(g => g.Map());
        }

        public static AnnotatedGroup Map(this IAnnotatedGroup value)
        {
            return new AnnotatedGroup(value);
        }

        public static Group Map(this IGroup value)
        {
            return new Group(value);
        }

        public static IEnumerable<Group> Map(this IEnumerable<IGroup> value)
        {
            return value?.Select(g => g.Map());
        }

        public static RecommendedGroup Map(this GroupFinder.Common.RecommendedGroup value)
        {
            return new RecommendedGroup(value);
        }

        public static IEnumerable<RecommendedGroup> Map(this IEnumerable<GroupFinder.Common.RecommendedGroup> value)
        {
            return value?.Select(r => r.Map());
        }

        public static SharedGroupMembership Map(this GroupFinder.Common.SharedGroupMembership value)
        {
            return new SharedGroupMembership(value);
        }

        public static IEnumerable<SharedGroupMembership> Map(this IList<GroupFinder.Common.SharedGroupMembership> value)
        {
            return value?.Select(s => s.Map());
        }

        public static User Map(this IUser value)
        {
            return new User(value);
        }

        public static IEnumerable<User> Map(this IEnumerable<IUser> value)
        {
            return value?.Select(u => u.Map());
        }

        #endregion
    }
}