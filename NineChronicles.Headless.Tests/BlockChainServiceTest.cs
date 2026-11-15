using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using Lib9c.Renderers;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Headless.Hosting;
using Libplanet.Mocks;
using Libplanet.Types.Tx;
using Moq;
using Nekoyume.Action;
using NineChronicles.Headless.Repositories.BlockChain;
using NineChronicles.Headless.Repositories.Swarm;
using NineChronicles.Headless.Repositories.Transaction;
using NineChronicles.Headless.Repositories.WorldState;
using NineChronicles.Headless.Services;
using Xunit;

namespace NineChronicles.Headless.Tests
{
    public class BlockChainServiceTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PutTransaction(bool stageResult)
        {
            // Arrange
            var mockBlockChain = new Mock<IBlockChainRepository>();
            var mockTransaction = new Mock<ITransactionRepository>();
            var mockWorld = new Mock<IWorldStateRepository>();
            var mockSwarm = new Mock<ISwarmRepository>();
            var mockRpcContext = new Mock<RpcContext>();
            var mockProperties = new Mock<LibplanetNodeServiceProperties>();
            var mockCache = new Mock<StateMemoryCache>();
            var mockPolicy = new Mock<IBlockPolicy>();

            // Mocking dependencies
            mockPolicy
                .Setup(bc => bc.ValidateNextBlockTx(It.IsAny<BlockChain>(), It.IsAny<Transaction>()))
                .Returns((TxPolicyViolationException?)null);
            mockBlockChain
                .Setup(bc => bc.StageTransaction(It.IsAny<Transaction>()))
                .Returns(stageResult);
            mockBlockChain
                .Setup(bc => bc.GetStagedTransactionIds())
                .Returns(ImmutableHashSet<TxId>.Empty);

            var service = new BlockChainService(
                mockBlockChain.Object,
                mockTransaction.Object,
                mockWorld.Object,
                mockSwarm.Object,
                mockRpcContext.Object,
                mockProperties.Object,
                new ActionEvaluationPublisher(
                    new BlockRenderer(),
                    new ActionRenderer(),
                    new ExceptionRenderer(),
                    new NodeStatusRenderer(),
                    new MockBlockChainStates(),
                    "",
                    0,
                    new RpcContext(),
                    new StateMemoryCache()
                ),
                mockCache.Object);

            var tx = Transaction.Create(0, new PrivateKey(), null, new List<IValue>
            {
                new DailyReward
                {
                    avatarAddress = new Address(),
                }.PlainValue,
            }); // Create a valid transaction

            // Act
            var result = await service.PutTransaction(tx.Serialize());

            // Assert
            Assert.Equal(stageResult, result);
            mockBlockChain.Verify(bc => bc.StageTransaction(It.IsAny<Transaction>()), Times.Once);
            mockSwarm.Verify(s => s.BroadcastTxs(It.IsAny<Transaction[]>()), stageResult ? Times.Once : Times.Never);
        }

        [Fact]
        public async Task PutTransactionHonorsSignerBlacklistAndDoesNotBroadcastRejection()
        {
            var directory = Path.Combine(Path.GetTempPath(), "agent-rpc-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "agents.db");
                AgentBlacklistPersistenceTest.CreateDatabase(path);
                var store = new SqliteAgentBlacklist(path);
                var chain = GraphQLTestUtils.CreateStandaloneContext().BlockChain;
                chain.StagePolicy = new AgentBlacklistStagePolicy(new VolatileStagePolicy(), store);
                var repository = new Mock<IBlockChainRepository>();
                repository.Setup(r => r.StageTransaction(It.IsAny<Transaction>()))
                    .Returns((Transaction tx) => chain.StageTransaction(tx));
                repository.Setup(r => r.GetStagedTransactionIds()).Returns(() => chain.GetStagedTransactionIds());
                var swarm = new Mock<ISwarmRepository>();
                var service = new BlockChainService(
                    repository.Object, new Mock<ITransactionRepository>().Object,
                    new Mock<IWorldStateRepository>().Object, swarm.Object, new RpcContext(),
                    new Mock<LibplanetNodeServiceProperties>().Object,
                    new ActionEvaluationPublisher(new BlockRenderer(), new ActionRenderer(),
                        new ExceptionRenderer(), new NodeStatusRenderer(), new MockBlockChainStates(),
                        "", 0, new RpcContext(), new StateMemoryCache()), new StateMemoryCache());
                var blocked = new PrivateKey();
                var allowed = new PrivateKey();
                store.Add(blocked.Address);
                Transaction Make(PrivateKey signer) => Transaction.Create(0, signer, chain.Genesis.Hash,
                    new[] { new DailyReward { avatarAddress = new PrivateKey().Address }.PlainValue });

                Assert.False(await service.PutTransaction(Make(blocked).Serialize()));
                swarm.Verify(s => s.BroadcastTxs(It.IsAny<Transaction[]>()), Times.Never);
                Assert.True(await service.PutTransaction(Make(allowed).Serialize()));
                swarm.Verify(s => s.BroadcastTxs(It.IsAny<Transaction[]>()), Times.Once);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void Compress()
        {
            var codec = new Codec();
            var compressed = BlockChainService.CompressState(codec, Null.Value);
            IValue result = null;
            using (var cp = new MemoryStream(compressed))
            {
                using (var decompressed = new MemoryStream())
                {
                    using (var df = new DeflateStream(cp, CompressionMode.Decompress))
                    {
                        df.CopyTo(decompressed);
                        decompressed.Seek(0, SeekOrigin.Begin);
                        var dec = decompressed.ToArray();
                        result = codec.Decode(dec);
                    }
                }
            }
            Assert.Equal(Null.Value, result);
        }
    }
}
