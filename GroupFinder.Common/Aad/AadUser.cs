using GroupFinder.Common.Models;
using System.Diagnostics;

namespace GroupFinder.Common.Aad
{
    [DebuggerDisplay("User {DisplayName}")]
    public class AadUser : AadEntity, IUser
    {
        public const string ObjectTypeName = "User";
        public const string UserTypeGuest = "Guest";
        public const string UserTypeMember = "Member";

        public string DisplayName { get; set; }
        public string Mail { get; set; }
        public string JobTitle { get; set; }
        public string UserPrincipalName { get; set; }
        public string UserType { get; set; } // "Guest" | "Member" | null
    }
}