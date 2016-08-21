using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace GroupFinder.Common.Security
{
    public abstract class AdalTokenProviderBase : ITokenProvider
    {
        protected string Tenant { get; private set; }
        protected string ClientId { get; private set; }
        protected AuthenticationContext AuthenticationContext { get; private set; }

        public AdalTokenProviderBase(string tenant, string clientId, TokenCache cache)
        {
            if (string.IsNullOrWhiteSpace(tenant))
            {
                throw new ArgumentException($"The \"{nameof(tenant)}\" parameter is required.", nameof(tenant));
            }
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException($"The \"{nameof(clientId)}\" parameter is required.", nameof(clientId));
            }
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }
            this.Tenant = tenant;
            this.ClientId = clientId;
            this.AuthenticationContext = new AuthenticationContext(Constants.AadEndpoint + this.Tenant, true, cache);
        }

        public Task<string> GetAccessTokenAsync()
        {
            return GetAccessTokenCoreAsync();
        }

        protected abstract Task<string> GetAccessTokenCoreAsync();
    }
}