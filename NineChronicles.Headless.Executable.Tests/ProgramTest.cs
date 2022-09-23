using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Libplanet.Crypto;
using Libplanet.Net;
using MagicOnion.Client;
using Nekoyume.Shared.Services;
using Xunit;

namespace NineChronicles.Headless.Executable.Tests
{
    public class ProgramTest
    {
        private readonly string _apvString;
        private readonly string _genesisBlockPath;
        private readonly string _storePath;

        public ProgramTest()
        {
            var privateKey = new PrivateKey();
            _apvString = AppProtocolVersion.Sign(privateKey, 1000).Token;

            _genesisBlockPath = "https://download.nine-chronicles.com/pos-genesis/genesis-block-pos-20220923-01";
            _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [Fact]
        public async Task Run()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var program = new Program().Run(
                _apvString,
                _genesisBlockPath,
                noMiner: true,
                host: "localhost",
                consensusPort: 6000,
                rpcServer: true,
                rpcListenHost: "localhost",
                rpcListenPort: 31234,
                graphQLServer: true,
                graphQLHost: "localhost",
                graphQLPort: 31238,
                storePath: _storePath,
                storeType: "rocksdb",
                validatorStrings: new[] { new PrivateKey().PublicKey.ToString() },
                skipPreload: true,
                noCors: true,
                cancellationToken: cancellationTokenSource.Token
            );

            try
            {
                // Wait until server start.
                // It can be flaky.
                await Task.Delay(10000).ConfigureAwait(false);

                using var client = new HttpClient();
                var queryString = "{\"query\":\"{chainQuery{blockQuery{block(index: 0) {hash}}}}\"}";
                var content = new StringContent(queryString);
                content.Headers.ContentLength = queryString.Length;
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync("http://localhost:31238/graphql", content);
                var responseString = await response.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("\"data\":{\"chainQuery\":{\"blockQuery\":{\"block\":{\"hash\":\"2c47e40a3d18d2457d65b2d4d8cd42a5ac9bb47e434341eb5ce7d217355eb0c1\"}}}}", responseString);

                var channel = new Channel(
                    "localhost:31234",
                    ChannelCredentials.Insecure,
                    new[]
                    {
                        new ChannelOption("grpc.max_receive_message_length", -1),
                        new ChannelOption("grpc.keepalive_permit_without_calls", 1),
                        new ChannelOption("grpc.keepalive_time_ms", 2000),
                    }
                );

                var service = MagicOnionClient.Create<IBlockChainService>(channel, Array.Empty<IClientFilter>())
                    .WithCancellationToken(channel.ShutdownToken);
                Assert.Equal(4246612, (await service.GetTip()).Length);
            }
            finally
            {
                cancellationTokenSource.Cancel();
            }
        }
    }
}
