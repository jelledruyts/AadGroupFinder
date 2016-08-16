namespace GroupFinder.Common
{
    public interface IGroup
    {
        string ObjectId { get; set; }
        string DisplayName { get; set; }
        string Description { get; set; }
        string Mail { get; set; }
        bool MailEnabled { get; set; }
        string MailNickname { get; set; }
        bool SecurityEnabled { get; set; }
    }
}