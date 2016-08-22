namespace GroupFinder.Common.Configuration
{
    public class AzureStorageConfiguration
    {
        public string Account { get; set; }
        public string StateContainer { get; set; }
        public string BackupContainer { get; set; }
        public string AdminKey { get; set; }
    }
}