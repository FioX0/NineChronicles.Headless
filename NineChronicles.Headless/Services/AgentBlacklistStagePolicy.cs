using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Serilog;

namespace NineChronicles.Headless.Services
{
    /// <summary>
    /// Local staging filter only. It never changes block validation or execution.
    /// </summary>
    public sealed class AgentBlacklistStagePolicy : IStagePolicy
    {
        private readonly IStagePolicy _inner;
        private readonly IAgentBlacklist _blacklist;

        public AgentBlacklistStagePolicy(IStagePolicy inner, IAgentBlacklist blacklist)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _blacklist = blacklist ?? throw new ArgumentNullException(nameof(blacklist));
        }

        public bool Stage(BlockChain blockChain, Transaction transaction)
        {
            bool blocked;
            try
            {
                blocked = _blacklist.IsBlocked(transaction.Signer);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Agent blacklist lookup failed; staging refused.");
                return false;
            }

            return !blocked && _inner.Stage(blockChain, transaction);
        }

        public bool Unstage(BlockChain blockChain, TxId id) => _inner.Unstage(blockChain, id);

        public void Ignore(BlockChain blockChain, TxId id) => _inner.Ignore(blockChain, id);

        public bool Ignores(BlockChain blockChain, TxId id) => _inner.Ignores(blockChain, id);

        public Transaction? Get(BlockChain blockChain, TxId id, bool filtered = true)
        {
            var tx = _inner.Get(blockChain, id, filtered);
            if (tx is null)
            {
                return null;
            }

            try
            {
                if (!_blacklist.IsBlocked(tx.Signer))
                {
                    return tx;
                }

                _inner.Unstage(blockChain, id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Agent blacklist lookup failed; staged transaction hidden.");
            }

            return null;
        }

        public IEnumerable<Transaction> Iterate(BlockChain blockChain, bool filtered = true)
        {
            var visible = new List<Transaction>();
            try
            {
                var blockedSigners = _blacklist.List().ToHashSet();
                foreach (var tx in _inner.Iterate(blockChain, filtered).ToArray())
                {
                    if (blockedSigners.Contains(tx.Signer))
                    {
                        _inner.Unstage(blockChain, tx.Id);
                    }
                    else
                    {
                        visible.Add(tx);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Agent blacklist lookup failed; staged transactions hidden.");
                return Array.Empty<Transaction>();
            }

            return visible;
        }

        public long GetNextTxNonce(BlockChain blockChain, Address address) =>
            _inner.GetNextTxNonce(blockChain, address);
    }
}
