using System;
using System.Collections.Generic;
using System.IO;
using Libplanet.Crypto;
using Microsoft.Data.Sqlite;

namespace NineChronicles.Headless.Services
{
    public interface IAgentBlacklist
    {
        bool IsEnabled { get; }

        bool IsBlocked(Address address);

        bool Add(Address address);

        bool Remove(Address address);

        IReadOnlyList<Address> List();
    }
}
