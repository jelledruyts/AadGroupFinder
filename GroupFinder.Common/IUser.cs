namespace GroupFinder.Common
{
    public interface IUser
    {
        string ObjectId { get; set; }
        string DisplayName { get; set; }
        string Mail { get; set; }
        string JobTitle { get; set; }
        string UserPrincipalName { get; set; }
    }
}