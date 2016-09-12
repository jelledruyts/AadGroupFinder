using GroupFinder.Common.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace GroupFinder.Common.Security
{
    public abstract class AdalTokenProviderBase : ITokenProvider
    {
        protected string Tenant { get; private set; }
        protected string ClientId { get; private set; }
        protected AuthenticationContext AuthenticationContext { get; private set; }

        protected AdalTokenProviderBase(ILogger logger, string tenant, string clientId, TokenCache cache)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
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
            LoggerCallbackHandler.Callback = new AdalLogAdapter(logger);
        }

        public Task<string> GetAccessTokenAsync()
        {
            return GetAccessTokenCoreAsync();
        }

        protected abstract Task<string> GetAccessTokenCoreAsync();

        private class AdalLogAdapter : IAdalLogCallback
        {
            private readonly ILogger logger;

            public AdalLogAdapter(ILogger logger)
            {
                this.logger = logger;
            }

            public void Log(LogLevel level, string message)
            {
                // Do not "await" the logging to complete.
                this.logger.LogAsync(GetEventLevel(level), $"[ADAL:{level.ToString()}] {message}");
            }

            private static EventLevel GetEventLevel(LogLevel level)
            {
                switch (level)
                {
                    case LogLevel.Information:
                        // Output Information messages from ADAL as Verbose application messages.
                        return EventLevel.Verbose;
                    case LogLevel.Verbose:
                        return EventLevel.Verbose;
                    case LogLevel.Warning:
                        return EventLevel.Warning;
                    case LogLevel.Error:
                        return EventLevel.Error;
                    default:
                        return EventLevel.Informational;
                }
            }
        }
    }
}