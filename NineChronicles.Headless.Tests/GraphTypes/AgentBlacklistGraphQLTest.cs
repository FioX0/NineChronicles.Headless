using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Server.Authorization.AspNetCore;
using GraphQL.Validation;
using Lib9c;
using Lib9c.Renderers;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Store;
using Libplanet.Types.Assets;
using Libplanet.Types.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nekoyume;
using Nekoyume.Action;
using NineChronicles.Headless.Middleware;
using NineChronicles.Headless.Repositories.BlockChain;
using NineChronicles.Headless.Repositories.StateTrie;
using NineChronicles.Headless.Repositories.Transaction;
using NineChronicles.Headless.Repositories.WorldState;
using NineChronicles.Headless.Services;
using NineChronicles.Headless.Tests.Common;
using OpenTelemetry.Metrics;
using Xunit;
using Xunit.Abstractions;

namespace NineChronicles.Headless.Tests.GraphTypes
{
    public class AgentBlacklistGraphQLTest : GraphQLTestBase, IDisposable
    {
        private readonly string _directory;
        private readonly string _path;
        private readonly string _token;
        private readonly IConfiguration _config;
        private readonly SqliteAgentBlacklist _store;
        private readonly ServiceProvider _services;

        public AgentBlacklistGraphQLTest(ITestOutputHelper output)
            : base(output)
        {
            _directory = Path.Combine(Path.GetTempPath(), "agent-graphql-" + Guid.NewGuid());
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "agents.db");
            AgentBlacklistPersistenceTest.CreateDatabase(_path);
            _store = new SqliteAgentBlacklist(_path);
            StandaloneContextFx.AgentBlacklist = _store;
            var chain = StandaloneContextFx.BlockChain;
            chain.StagePolicy = new AgentBlacklistStagePolicy(chain.StagePolicy, _store);
            _token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var tokenPath = Path.Combine(_directory, "admin-token");
            File.WriteAllText(tokenPath, _token);
            _config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            _config["Headless:AgentBlacklistAdminTokenPath"] = tokenPath;
            var publisher = new ActionEvaluationPublisher(
                new BlockRenderer(), new ActionRenderer(), new ExceptionRenderer(), new NodeStatusRenderer(),
                chain, "", 0, new RpcContext(), new StateMemoryCache());
            var startup = new GraphQLService.GraphQLStartup(_config, StandaloneContextFx, publisher);
            var services = new ServiceCollection();
            services.AddLogging();
            startup.ConfigureServices(services);
            _services = services.BuildServiceProvider();
        }

        [Fact]
        public async Task ManagementRequiresDedicatedAdminTokenAndPersistsAcrossInstances()
        {
            var address = new PrivateKey().Address;
            string add = $"mutation {{ blacklistAgent(agentAddress: \"{address}\") }}";
            string remove = $"mutation {{ unblacklistAgent(agentAddress: \"{address}\") }}";
            string list = "{ blacklistedAgents }";

            Assert.NotNull((await ExecuteAuthorized(add)).Errors);
            Assert.NotNull((await ExecuteAuthorized(add, "wrong")).Errors);
            Assert.NotNull((await ExecuteAuthorized(add, spoofClaim: true)).Errors);
            Assert.False(_store.IsBlocked(address));
            Assert.NotNull((await ExecuteAuthorized(list)).Errors);
            Assert.Null((await ExecuteAuthorized("{ __typename }")).Errors);

            Assert.Null((await ExecuteAuthorized(add, _token)).Errors);
            Assert.True(new SqliteAgentBlacklist(_path).IsBlocked(address));
            Assert.Null((await ExecuteAuthorized(list, _token)).Errors);
            Assert.NotNull((await ExecuteAuthorized(remove)).Errors);
            Assert.True(_store.IsBlocked(address));
            Assert.Null((await ExecuteAuthorized(remove, _token)).Errors);
            Assert.False(new SqliteAgentBlacklist(_path).IsBlocked(address));
        }

        [Fact]
        public async Task HttpGraphqlPipelineEnforcesAdministrationPolicy()
        {
            var chain = StandaloneContextFx.BlockChain;
            var publisher = new ActionEvaluationPublisher(
                new BlockRenderer(), new ActionRenderer(), new ExceptionRenderer(), new NodeStatusRenderer(),
                chain, "", 0, new RpcContext(), new StateMemoryCache());
            var startup = new GraphQLService.GraphQLStartup(_config, StandaloneContextFx, publisher);
            using var host = new WebHostBuilder()
                .UseKestrel()
                .UseEnvironment("Development")
                .UseUrls("http://127.0.0.1:0")
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConfiguration>(_config);
                    services.AddSingleton(StandaloneContextFx);
                    services.AddSingleton(chain);
                    services.AddSingleton(publisher);
                    services.AddSingleton<IKeyStore>(StandaloneContextFx.KeyStore);
                    services.AddOpenTelemetry().WithMetrics(builder => builder.AddPrometheusExporter());
                    startup.ConfigureServices(services);
                    services.AddSingleton<StateMemoryCache>();
                    services.AddSingleton(StandaloneContextFx.NineChroniclesNodeService!);
                    services.AddSingleton<IStore>(StandaloneContextFx.Store);
                    services.AddSingleton<IWorldStateRepository>(WorldStateRepository.Object);
                    services.AddSingleton<IBlockChainRepository>(BlockChainRepository.Object);
                    services.AddSingleton<IStateTrieRepository>(StateTrieRepository.Object);
                    services.AddSingleton<ITransactionRepository>(TransactionRepository.Object);
                })
                .Configure((context, app) => startup.Configure(app, context.HostingEnvironment))
                .Build();
            await host.StartAsync();
            var url = host.ServerFeatures.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(url) };
            var address = new PrivateKey().Address;
            string mutation = $"mutation {{ blacklistAgent(agentAddress: \"{address}\") }}";

            async Task<JsonDocument> Request(string query, string? token = null)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
                if (token is not null)
                {
                    request.Headers.Add(AgentBlacklistAdminAuthenticationMiddleware.HeaderName, token);
                }

                request.Content = new StringContent(JsonSerializer.Serialize(new { query }), Encoding.UTF8, "application/json");
                using var response = await client.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();
                if ((int)response.StatusCode >= 500 || string.IsNullOrWhiteSpace(body))
                {
                    throw new InvalidOperationException($"GraphQL test response {(int)response.StatusCode}: {body}");
                }

                return JsonDocument.Parse(body);
            }

            using (var denied = await Request(mutation))
            {
                Assert.True(denied.RootElement.TryGetProperty("errors", out _));
            }

            Assert.False(_store.IsBlocked(address));
            using (var publicQuery = await Request("{ __typename }"))
            {
                Assert.False(publicQuery.RootElement.TryGetProperty("errors", out _), publicQuery.RootElement.GetRawText());
            }

            using (var allowed = await Request(mutation, _token))
            {
                Assert.False(allowed.RootElement.TryGetProperty("errors", out _), allowed.RootElement.GetRawText());
            }

            Assert.True(new SqliteAgentBlacklist(_path).IsBlocked(address));
            await host.StopAsync();
        }

        [Fact]
        public void NodeServiceInstallsConfiguredPolicyAndContext()
        {
            using var service = ServiceBuilder.CreateNineChroniclesNodeService(
                StandaloneContextFx.BlockChain.Genesis, ProposerPrivateKey, _store);
            Assert.IsType<AgentBlacklistStagePolicy>(service.BlockChain.StagePolicy);
            var context = new StandaloneContext();
            service.ConfigureContext(context);
            Assert.Same(_store, context.AgentBlacklist);
        }

        [Fact]
        public void BlacklistDoesNotRejectAnAlreadyValidBlockFromPeers()
        {
            var chain = StandaloneContextFx.BlockChain;
            var signer = new PrivateKey();
            var action = new PrepareRewardAssets
            {
                RewardPoolAddress = MeadConfig.PatronAddress,
                Assets = new List<FungibleAssetValue> { 1 * Currencies.Mead },
            };
            var tx = Transaction.Create(0, signer, chain.Genesis.Hash, new[] { action.PlainValue });
            Assert.True(chain.StageTransaction(tx));
            var block = chain.ProposeBlock(ProposerPrivateKey, null);
            Assert.Contains(block.Transactions, included => included.Id == tx.Id);
            _store.Add(signer.Address);

            chain.Append(block, GenerateBlockCommit(block.Index, block.Hash, GenesisValidators));
            Assert.Equal(block.Hash, chain.Tip.Hash);
        }

        [Fact]
        public async Task GraphqlRejectsVerifiedBlockedSignerForMultipleAvatars()
        {
            var signer = new PrivateKey();
            _store.Add(signer.Address);
            foreach (var avatar in new[] { new PrivateKey().Address, new PrivateKey().Address })
            {
                var tx = Transaction.Create(0, signer, StandaloneContextFx.BlockChain.Genesis.Hash,
                    new[] { new DailyReward { avatarAddress = avatar }.PlainValue });
                string mutation = $"mutation {{ stageTransaction(payload: \"{ByteUtil.Hex(tx.Serialize())}\") }}";
                var result = await ExecuteAuthorized(mutation);
                Assert.Contains("blocked", Assert.Single(result.Errors!).Message);
                Assert.DoesNotContain(StandaloneContextFx.BlockChain.GetStagedTransactionIds(), id => id == tx.Id);
            }
        }

        private async Task<ExecutionResult> ExecuteAuthorized(string query, string? token = null, bool spoofClaim = false)
        {
            var http = new DefaultHttpContext();
            if (token is not null)
            {
                http.Request.Headers.Add(AgentBlacklistAdminAuthenticationMiddleware.HeaderName, token);
            }

            if (spoofClaim)
            {
                http.User.AddIdentity(new ClaimsIdentity(
                    new[] { new Claim(AgentBlacklistAdminAuthenticationMiddleware.ClaimType, "true") }, "OrdinaryUser"));
            }

            var middleware = new AgentBlacklistAdminAuthenticationMiddleware(_config);
            await middleware.InvokeAsync(http, _ => Task.CompletedTask);
            var accessor = new DefaultClaimsPrincipalAccessor(new HttpContextAccessor { HttpContext = http });
            var rule = new AuthorizationValidationRule(_services.GetRequiredService<IAuthorizationService>(), accessor);
            return await new DocumentExecuter().ExecuteAsync(new ExecutionOptions
            {
                Schema = Schema,
                Query = query,
                ValidationRules = DocumentValidator.CoreRules.Concat(new[] { rule }),
                RequestServices = _services,
            });
        }

        public void Dispose()
        {
            _services.Dispose();
            Directory.Delete(_directory, recursive: true);
        }
    }
}
