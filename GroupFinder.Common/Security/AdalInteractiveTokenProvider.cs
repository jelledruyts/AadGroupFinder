using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace GroupFinder.Common.Security
{
    public class AdalInteractiveTokenProvider : AdalTokenProviderBase
    {
        private Uri redirectUri;

        public AdalInteractiveTokenProvider(string tenant, string clientId, TokenCache cache, Uri redirectUri)
            : base(tenant, clientId, cache)
        {
            if (redirectUri == null)
            {
                throw new ArgumentNullException(nameof(redirectUri));
            }
            this.redirectUri = redirectUri;
        }

        protected override async Task<string> GetAccessTokenCoreAsync()
        {
            var authenticationResult = await this.AuthenticationContext.AcquireTokenAsync(Constants.AadGraphApiEndpoint, this.ClientId, this.redirectUri, new PlatformParameters(PromptBehavior.Auto));
            return authenticationResult.AccessToken;
        }
    }
}