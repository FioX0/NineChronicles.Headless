using System;
using System.IO;
using System.Linq;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using NineChronicles.Headless.Services;
using Xunit;

namespace NineChronicles.Headless.Tests
{
    public class AgentBlacklistStagePolicyTest
    {
        [Fact]
        public void BlockedSignerCannotStageAnyAvatarAndRevocationRestoresAccess()
        {
            var type = typeof(StateMemoryCache).Assembly.GetType(
                "NineChronicles.Headless.Services.AgentBlacklistStagePolicy");
            Assert.NotNull(type);
            var directory = Path.Combine(Path.GetTempPath(), "agent-policy-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "agents.db");
                AgentBlacklistPersistenceTest.CreateDatabase(path);
                var store = new SqliteAgentBlacklist(path);
                var otherNode = new SqliteAgentBlacklist(path);
                var inner = new VolatileStagePolicy(TimeSpan.FromHours(1));
                var policy = (IStagePolicy)Activator.CreateInstance(type!, inner, store)!;
                var chain = GraphQLTestUtils.CreateStandaloneContext().BlockChain;
                chain.StagePolicy = policy;
                var blocked = new PrivateKey();
                var allowed = new PrivateKey();
                Transaction Make(PrivateKey signer, int nonce, Address avatar) => Transaction.Create(
                    nonce, signer, chain.Genesis.Hash,
                    new[] { new DailyReward { avatarAddress = avatar }.PlainValue });
                var avatar1 = new PrivateKey().Address;
                var avatar2 = new PrivateKey().Address;
                var tx1 = Make(blocked, 0, avatar1);
                var tx2 = Make(blocked, 1, avatar2);
                var tx3 = Make(allowed, 0, avatar1);

                Assert.True(chain.StageTransaction(tx1));
                Assert.True(otherNode.Add(blocked.Address));
                Assert.False(chain.StageTransaction(tx2));
                Assert.Null(policy.Get(chain, tx1.Id, filtered: false));
                Assert.DoesNotContain(policy.Iterate(chain, filtered: false), tx => tx.Signer == blocked.Address);
                Assert.True(chain.StageTransaction(tx3));
                Assert.True(otherNode.Remove(blocked.Address));
                Assert.True(chain.StageTransaction(tx2));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void MissingDatabaseDoesNotPermitStaging()
        {
            var type = typeof(StateMemoryCache).Assembly.GetType(
                "NineChronicles.Headless.Services.AgentBlacklistStagePolicy");
            Assert.NotNull(type);
            var directory = Path.Combine(Path.GetTempPath(), "agent-policy-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "agents.db");
                AgentBlacklistPersistenceTest.CreateDatabase(path);
                var store = new SqliteAgentBlacklist(path);
                var policy = (IStagePolicy)Activator.CreateInstance(type!, new VolatileStagePolicy(), store)!;
                var chain = GraphQLTestUtils.CreateStandaloneContext().BlockChain;
                chain.StagePolicy = policy;
                var tx = Transaction.Create(0, new PrivateKey(), chain.Genesis.Hash,
                    new[] { new DailyReward { avatarAddress = new PrivateKey().Address }.PlainValue });
                File.Delete(path);
                Assert.False(chain.StageTransaction(tx));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
