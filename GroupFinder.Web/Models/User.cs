using GroupFinder.Common;
using System;

namespace GroupFinder.Web.Models
{
    public class User : IUser
    {
        public string ObjectId { get; set; }
        public string DisplayName { get; set; }
        public string Mail { get; set; }
        public string JobTitle { get; set; }
        public string UserPrincipalName { get; set; }

        public User()
        {
        }

        public User(IUser value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            this.ObjectId = value.ObjectId;
            this.DisplayName = value.DisplayName;
            this.Mail = value.Mail;
            this.JobTitle = value.JobTitle;
            this.UserPrincipalName = value.UserPrincipalName;
        }
    }
}