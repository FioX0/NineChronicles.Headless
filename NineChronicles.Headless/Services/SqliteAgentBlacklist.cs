using System;
using System.Collections.Generic;
using System.IO;
using Libplanet.Crypto;
using Microsoft.Data.Sqlite;

namespace NineChronicles.Headless.Services
{
    public sealed class SqliteAgentBlacklist : IAgentBlacklist
    {
        private readonly string _connectionString;

        public SqliteAgentBlacklist(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            {
                throw new ArgumentException("Agent blacklist database path must be absolute.", nameof(path));
            }

            string? parent = Path.GetDirectoryName(path);
            if (parent is null || !Directory.Exists(parent))
            {
                throw new DirectoryNotFoundException("Agent blacklist database parent directory is missing.");
            }

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                // Provision explicitly. A deleted database must never become an empty allow-all list.
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false,
                DefaultTimeout = 2,
            };
            using (var connection = new SqliteConnection(builder.ConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT address FROM blocked_agents LIMIT 1";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    _ = new Address(reader.GetString(0));
                }
            }

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            _connectionString = builder.ConnectionString;
        }

        public bool IsEnabled => true;

        public bool IsBlocked(Address address)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT EXISTS(SELECT 1 FROM blocked_agents WHERE address = $address)";
            command.Parameters.AddWithValue("$address", Canonical(address));
            return Convert.ToInt64(command.ExecuteScalar()) == 1;
        }

        public bool Add(Address address) => Change(
            "INSERT OR IGNORE INTO blocked_agents(address) VALUES ($address)", address);

        public bool Remove(Address address) => Change(
            "DELETE FROM blocked_agents WHERE address = $address", address);

        public IReadOnlyList<Address> List()
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT address FROM blocked_agents ORDER BY address";
            using var reader = command.ExecuteReader();
            var result = new List<Address>();
            while (reader.Read())
            {
                result.Add(new Address(reader.GetString(0)));
            }

            return result;
        }

        private static string Canonical(Address address) => address.ToString().ToLowerInvariant();

        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(_connectionString);
            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private bool Change(string sql, Address address)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("$address", Canonical(address));
            return command.ExecuteNonQuery() > 0;
        }
    }
}
