using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NineChronicles.Headless.Tests
{
    public class AgentBlacklistAdminAuthenticationTest
    {
        [Fact]
        public async Task DedicatedTokenIsRequiredAndAbsentOrWrongTokensAreDenied()
        {
            var type = typeof(StateMemoryCache).Assembly.GetType(
                "NineChronicles.Headless.Middleware.AgentBlacklistAdminAuthenticationMiddleware");
            Assert.NotNull(type);
            var directory = Path.Combine(Path.GetTempPath(), "agent-admin-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "token");
                var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                File.WriteAllText(path, token);
                var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
                config["Headless:AgentBlacklistAdminTokenPath"] = path;
                var middleware = (IMiddleware)Activator.CreateInstance(type!, config)!;

                async Task<bool> Authorized(string? supplied)
                {
                    var context = new DefaultHttpContext();
                    if (supplied is not null)
                    {
                        context.Request.Headers.Add("X-Agent-Blacklist-Token", supplied);
                    }

                    await middleware.InvokeAsync(context, _ => Task.CompletedTask);
                    return context.User.HasClaim("agent-blacklist-admin", "true");
                }

                Assert.False(await Authorized(null));
                Assert.False(await Authorized("wrong"));
                Assert.True(await Authorized(token));
                File.Delete(path);
                Assert.False(await Authorized(token));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
