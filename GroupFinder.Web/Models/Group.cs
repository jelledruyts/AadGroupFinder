using GroupFinder.Common.Models;
using System;

namespace GroupFinder.Web.Models
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

        public Group(IGroup value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            this.ObjectId = value.ObjectId;
            this.DisplayName = value.DisplayName;
            this.Description = value.Description;
            this.Mail = value.Mail;
            this.MailEnabled = value.MailEnabled;
            this.MailNickname = value.MailNickname;
            this.SecurityEnabled = value.SecurityEnabled;
        }
    }
}