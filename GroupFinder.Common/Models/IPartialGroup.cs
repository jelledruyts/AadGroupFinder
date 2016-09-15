namespace GroupFinder.Common.Models
{
    public interface IPartialGroup
    {
        string ObjectId { get; set; }
        Optional<string> DisplayName { get; set; }
        Optional<string> Description { get; set; }
        Optional<string> Mail { get; set; }
        bool? MailEnabled { get; set; }
        Optional<string> MailNickname { get; set; }
        bool? SecurityEnabled { get; set; }
    }
}