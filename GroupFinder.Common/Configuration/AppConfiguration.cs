namespace GroupFinder.Common.Configuration
{
    public class AppConfiguration
    {
        public ProcessorConfiguration Processor { get; set; }
        public AzureADConfiguration AzureAD { get; set; }
        public AzureSearchConfiguration AzureSearch { get; set; }
        public AzureStorageConfiguration AzureStorage { get; set; }
    }
}