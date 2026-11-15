using System;
using System.Collections.Generic;
using System.IO;
using Libplanet.Crypto;
using Microsoft.Data.Sqlite;

namespace NineChronicles.Headless.Services
{
    public sealed class DisabledAgentBlacklist : IAgentBlacklist
    {
        public static readonly DisabledAgentBlacklist Instance = new DisabledAgentBlacklist();

        private DisabledAgentBlacklist()
        {
        }

        public bool IsEnabled => false;

        public bool IsBlocked(Address address) => false;

        public bool Add(Address address) => throw new InvalidOperationException("Agent blacklist is not configured.");

        public bool Remove(Address address) => throw new InvalidOperationException("Agent blacklist is not configured.");

        public IReadOnlyList<Address> List() => throw new InvalidOperationException("Agent blacklist is not configured.");
    }
}
