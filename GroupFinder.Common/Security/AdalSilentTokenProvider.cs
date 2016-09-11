using GroupFinder.Common.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;

namespace GroupFinder.Common.Security
{
    public class AdalSilentTokenProvider : AdalTokenProviderBase
    {
        public AdalSilentTokenProvider(ILogger logger, string tenant, string clientId, TokenCache cache)
            : base(logger, tenant, clientId, cache)
        {
        }

        protected override async Task<string> GetAccessTokenCoreAsync()
        {
            var authenticationResult = await this.AuthenticationContext.AcquireTokenSilentAsync(Constants.AadGraphApiEndpoint, this.ClientId, UserIdentifier.AnyUser);
            return authenticationResult.AccessToken;
        }
    }
}