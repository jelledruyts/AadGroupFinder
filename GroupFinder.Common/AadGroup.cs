using System.Diagnostics;

namespace GroupFinder.Common
{
    [DebuggerDisplay("Group: {DisplayName}")]
    public class AadGroup : AadEntity
    {
        public const string ObjectTypeName = "Group";

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Mail { get; set; }
        public bool MailEnabled { get; set; }
        public string MailNickname { get; set; }
        public bool SecurityEnabled { get; set; }
    }
}