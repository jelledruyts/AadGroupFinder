using System;

namespace GroupFinder.Common.Models
{
    public class Group : IGroup
    {
        public string ObjectId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Mail { get; set; }
        public bool MailEnabled { get; set; }
        public string MailNickname { get; set; }
        public bool SecurityEnabled { get; set; }

        public Group()
        {
        }

        public Group(IPartialGroup partialGroup)
        {
            if (partialGroup == null)
            {
                throw new ArgumentNullException(nameof(partialGroup));
            }
            // Creating a group based on a partial group definition assumes that all required properties are present.
            if (partialGroup.ObjectId == null)
            {
                throw new ArgumentNullException(nameof(partialGroup.ObjectId));
            }
            if (!partialGroup.DisplayName.HasValue)
            {
                throw new ArgumentNullException(nameof(partialGroup.DisplayName));
            }
            if (!partialGroup.Description.HasValue)
            {
                throw new ArgumentNullException(nameof(partialGroup.Description));
            }
            if (!partialGroup.Mail.HasValue)
            {
                throw new ArgumentNullException(nameof(partialGroup.Mail));
            }
            if (partialGroup.MailEnabled == null)
            {
                throw new ArgumentNullException(nameof(partialGroup.MailEnabled));
            }
            if (!partialGroup.MailNickname.HasValue)
            {
                throw new ArgumentNullException(nameof(partialGroup.MailNickname));
            }
            if (partialGroup.SecurityEnabled == null)
            {
                throw new ArgumentNullException(nameof(partialGroup.SecurityEnabled));
            }
            this.ObjectId = partialGroup.ObjectId;
            this.DisplayName = partialGroup.DisplayName.Value;
            this.Description = partialGroup.Description.Value;
            this.Mail = partialGroup.Mail.Value;
            this.MailEnabled = partialGroup.MailEnabled.Value;
            this.MailNickname = partialGroup.MailNickname.Value;
            this.SecurityEnabled = partialGroup.SecurityEnabled.Value;
        }
    }
}
