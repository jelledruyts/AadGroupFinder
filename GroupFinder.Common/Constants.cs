namespace GroupFinder.Common
{
    public static class Constants
    {
        public const string AadEndpoint = "https://login.microsoftonline.com/";
        public const string AadGraphApiEndpoint = "https://graph.windows.net/";

        internal const string AadGraphApiVersionParameterName = "api-version";
        internal const string AadGraphApiVersionNumber = "1.6";

        internal const int RetryAttemptsOnTransientError = 5;
    }
}