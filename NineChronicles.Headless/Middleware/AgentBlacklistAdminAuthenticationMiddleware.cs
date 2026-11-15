using System;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace NineChronicles.Headless.Middleware
{
    /// <summary>
    /// Authenticates only blacklist administration, separately from ordinary transaction access.
    /// The token is supplied in a header, never in a GraphQL document or URL.
    /// </summary>
    public sealed class AgentBlacklistAdminAuthenticationMiddleware : IMiddleware
    {
        public const string AuthenticationType = "AgentBlacklistAdminToken";
        public const string ClaimType = "agent-blacklist-admin";
        public const string HeaderName = "X-Agent-Blacklist-Token";
        private readonly IConfiguration _configuration;

        public AgentBlacklistAdminAuthenticationMiddleware(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            string? path = _configuration["Headless:AgentBlacklistAdminTokenPath"];
            if (!string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path) &&
                context.Request.Headers.TryGetValue(HeaderName, out var provided) && provided.Count == 1)
            {
                try
                {
                    string secret = (await File.ReadAllTextAsync(path)).Trim();
                    string presented = provided[0] ?? string.Empty;
                    if (secret.Length >= 32 && presented.Length > 0 &&
                        CryptographicOperations.FixedTimeEquals(
                            SHA256.HashData(Encoding.UTF8.GetBytes(secret)),
                            SHA256.HashData(Encoding.UTF8.GetBytes(presented))))
                    {
                        context.User.AddIdentity(new ClaimsIdentity(
                            new[] { new Claim(ClaimType, "true") }, AuthenticationType));
                    }
                }
                catch (IOException)
                {
                    // Missing/unreadable token must never grant administrative access.
                }
                catch (UnauthorizedAccessException)
                {
                    // Missing/unreadable token must never grant administrative access.
                }
            }

            await next(context);
        }
    }
}
