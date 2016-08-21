namespace GroupFinder.Common.Configuration
{
    public class AzureADConfiguration
    {
        public string Tenant { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TokenCacheFileName { get; set; }
    }
}