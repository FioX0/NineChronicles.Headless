using System.IO;
using System.Threading.Tasks;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NineChronicles.Headless.Middleware
{
    public class CustomRateLimitMiddleware : IpRateLimitMiddleware
    {
        private readonly ILogger<CustomRateLimitMiddleware> _logger;
        private readonly IRateLimitConfiguration _config;

        public CustomRateLimitMiddleware(RequestDelegate next,
            IProcessingStrategy processingStrategy,
            IOptions<IpRateLimitOptions> options,
            IIpPolicyStore policyStore,
            IRateLimitConfiguration config,
            ILogger<CustomRateLimitMiddleware> logger)
            : base(next, processingStrategy, options, policyStore, config, logger)
        {
            _config = config;
            _logger = logger;
        }

        public override async Task<ClientRequestIdentity> ResolveIdentityAsync(HttpContext httpContext)
        {
            var identity = await base.ResolveIdentityAsync(httpContext);

            if (httpContext.Request.Protocol == "HTTP/2")
            {
                return identity;
            }

            if (httpContext.Request.Protocol == "HTTP/1.1")
            {
                httpContext.Request.EnableBuffering();
                var body = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
                httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                if (body.Contains("stageTransaction"))
                {
                    return new ClientRequestIdentity
                    {
                        ClientIp = identity.ClientIp,
                        Path = "/graphql/stagetransaction",
                        HttpVerb = identity.HttpVerb,
                        ClientId = identity.ClientId
                    };
                }

                return identity;
            }

            return new ClientRequestIdentity
            {
                ClientIp = "127.0.0.1",
                Path = "/",
                HttpVerb = httpContext.Request.Method.ToLowerInvariant(),
                ClientId = "anon"
            };
        }
    }
}
