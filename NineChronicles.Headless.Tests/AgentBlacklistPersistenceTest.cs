using System;
using System.IO;
using System.Reflection;
using Libplanet.Crypto;
using Microsoft.Data.Sqlite;
using Xunit;

namespace NineChronicles.Headless.Tests
{
    public class AgentBlacklistPersistenceTest
    {
        internal static void CreateDatabase(string path)
        {
            using var connection = new SqliteConnection($"Data Source={path}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE blocked_agents (address TEXT PRIMARY KEY NOT NULL)";
            command.ExecuteNonQuery();
        }

        [Fact]
        public void MissingDatabaseFailsClosedInsteadOfSilentlyCreatingEmptyBlacklist()
        {
            var type = typeof(StateMemoryCache).Assembly.GetType(
                "NineChronicles.Headless.Services.SqliteAgentBlacklist");
            Assert.NotNull(type);
            var directory = Path.Combine(Path.GetTempPath(), "agent-blacklist-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "missing.db");
                Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(type!, path));
                Assert.False(File.Exists(path));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void AddRemovePersistsAndIsVisibleAcrossIndependentInstances()
        {
            var type = typeof(StateMemoryCache).Assembly.GetType(
                "NineChronicles.Headless.Services.SqliteAgentBlacklist");
            Assert.NotNull(type);
            var directory = Path.Combine(Path.GetTempPath(), "agent-blacklist-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "agents.db");
                CreateDatabase(path);
                var first = Activator.CreateInstance(type!, path)!;
                var second = Activator.CreateInstance(type!, path)!;
                var address = new PrivateKey().Address;
                var other = new PrivateKey().Address;
                bool Call(object instance, string method, Address value) =>
                    (bool)type!.GetMethod(method)!.Invoke(instance, new object[] { value })!;

                Assert.False(Call(first, "IsBlocked", address));
                Assert.True(Call(first, "Add", address));
                Assert.True(Call(second, "IsBlocked", address));
                Assert.False(Call(second, "IsBlocked", other));
                var reopened = Activator.CreateInstance(type!, path)!;
                Assert.True(Call(reopened, "IsBlocked", address));
                Assert.True(Call(second, "Remove", address));
                Assert.False(Call(first, "IsBlocked", address));
                Assert.False(Call(reopened, "IsBlocked", address));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
