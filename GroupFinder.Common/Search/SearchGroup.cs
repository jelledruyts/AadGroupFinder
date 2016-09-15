using GroupFinder.Common.Models;
using System;
using System.Collections.Generic;

namespace GroupFinder.Common.Search
{
    internal class SearchGroup : IGroupSearchResult
    {
        // Search-specific properties.
        public double Score { get; set; }
        public IList<string> Tags { get; set; }
        public string Notes { get; set; }
        public bool IsDiscussionList { get; set; }

        // Base properties.
        public string ObjectId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Mail { get; set; }
        public bool MailEnabled { get; set; }
        public string MailNickname { get; set; }
        public bool SecurityEnabled { get; set; }

        public SearchGroup(double score, IDictionary<string, object> properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            this.Score = score;
            this.Tags = (IList<string>)properties[AzureSearchService.FieldNameTags];
            this.Notes = (string)properties[AzureSearchService.FieldNameNotes];
            this.IsDiscussionList = (bool)(properties[AzureSearchService.FieldNameIsDiscussionList] ?? false);
            this.ObjectId = (string)properties[AzureSearchService.FieldNameObjectId];
            this.DisplayName = (string)properties[AzureSearchService.FieldNameDisplayName];
            this.Description = (string)properties[AzureSearchService.FieldNameDescription];
            this.Mail = (string)properties[AzureSearchService.FieldNameMail];
            this.MailEnabled = (bool)properties[AzureSearchService.FieldNameMailEnabled];
            this.MailNickname = (string)properties[AzureSearchService.FieldNameMailNickname];
            this.SecurityEnabled = (bool)properties[AzureSearchService.FieldNameSecurityEnabled];
        }
    }
}