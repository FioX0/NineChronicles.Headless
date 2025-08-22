#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bencodex;
using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Lib9c;
using Lib9c.ActionEvaluatorCommonComponents;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Blockchain;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Explorer.GraphTypes;
using Libplanet.KeyStore;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Microsoft.Extensions.Configuration;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;
using NineChronicles.Headless.GraphTypes.Diff;
using NineChronicles.Headless.GraphTypes.States;
using NineChronicles.Headless.Repositories.BlockChain;
using NineChronicles.Headless.Repositories.StateTrie;
using NineChronicles.Headless.Repositories.Transaction;
using NineChronicles.Headless.Repositories.WorldState;
using Serilog;
using static NineChronicles.Headless.NCActionUtils;
using Block = NineChronicles.Headless.Domain.Model.BlockChain.Block;

namespace NineChronicles.Headless.GraphTypes
{
    public class StandaloneQuery : ObjectGraphType
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource("NineChronicles.Headless.GraphTypes.StandaloneQuery");

        public StandaloneQuery(StandaloneContext standaloneContext, IKeyStore keyStore, IConfiguration configuration, StateMemoryCache stateMemoryCache, IWorldStateRepository worldStateRepository, IBlockChainRepository blockChainRepository, ITransactionRepository transactionRepository, IStateTrieRepository stateTrieRepository)
        {
            bool useSecretToken = configuration[GraphQLService.SecretTokenKey] is { };
            if (Convert.ToBoolean(configuration.GetSection("Jwt")["EnableJwtAuthentication"]))
            {
                this.AuthorizeWith(GraphQLService.JwtPolicyKey);
            }

            Field<NonNullGraphType<StateQuery>>(name: "stateQuery", arguments: new QueryArguments(
                new QueryArgument<ByteStringType>
                {
                    Name = "hash",
                    Description = "Offset block hash for query.",
                },
                new QueryArgument<LongGraphType>
                {
                    Name = "index",
                    Description = "Offset block index for query."
                }),
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("stateQuery");
                    Block block = (context.GetArgument<byte[]?>("hash"), context.GetArgument<long?>("index")) switch
                    {
                        ({ } bytes, null) => blockChainRepository.GetBlock(new BlockHash(bytes)),
                        (null, { } index) => blockChainRepository.GetBlock(index),
                        (not null, not null) => throw new ArgumentException("Only one of 'hash' and 'index' must be given."),
                        (null, null) => blockChainRepository.GetTip(),
                    };
                    activity?.AddTag("BlockHash", block.Hash.ToString());

                    return new StateContext(
                        worldStateRepository.GetWorldState(block.StateRootHash),
                        block.Index,
                        stateMemoryCache
                    );
                }
            );

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<DiffGraphType>>>>(
                name: "diffs",
                description: "This field allows you to query the diffs between two blocks." +
                             " `baseIndex` is the reference block index, and changedIndex is the block index from which to check" +
                             " what changes have occurred relative to `baseIndex`." +
                             " Both indices must not be higher than the current block on the chain nor lower than the genesis block index (0)." +
                             " The difference between the two blocks must be greater than zero for a valid comparison and less than ten for performance reasons.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<LongGraphType>>
                    {
                        Name = "baseIndex",
                        Description = "The index of the reference block from which the state is retrieved."
                    },
                    new QueryArgument<NonNullGraphType<LongGraphType>>
                    {
                        Name = "changedIndex",
                        Description = "The index of the target block for comparison."
                    }
                ),
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("diffs");
                    var baseIndex = context.GetArgument<long>("baseIndex");
                    var changedIndex = context.GetArgument<long>("changedIndex");

                    var blockInterval = Math.Abs(changedIndex - baseIndex);
                    if (blockInterval >= 10 || blockInterval == 0)
                    {
                        throw new ExecutionError(
                            "Interval between baseIndex and changedIndex should not be greater than 10 or zero"
                        );
                    }

                    var baseBlockStateRootHash = blockChainRepository.GetBlock(baseIndex).StateRootHash.ToString();
                    var changedBlockStateRootHash = blockChainRepository.GetBlock(changedIndex).StateRootHash.ToString();

                    var baseStateRootHash = HashDigest<SHA256>.FromString(baseBlockStateRootHash);
                    var targetStateRootHash = HashDigest<SHA256>.FromString(
                        changedBlockStateRootHash
                    );

                    return stateTrieRepository.CompareStateTrie(baseStateRootHash, targetStateRootHash);
                }
            );

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StateDiffType>>>>(
                name: "accountDiffs",
                description: "This field allows you to query the diffs based accountAddress between two blocks." +
                             " `baseIndex` is the reference block index, and changedIndex is the block index from which to check" +
                             " what changes have occurred relative to `baseIndex`." +
                             " Both indices must not be higher than the current block on the chain nor lower than the genesis block index (0)." +
                             " The difference between the two blocks must be greater than zero for a valid comparison and less than ten for performance reasons.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<LongGraphType>>
                    {
                        Name = "baseIndex",
                        Description = "The index of the reference block from which the state is retrieved."
                    },
                    new QueryArgument<NonNullGraphType<LongGraphType>>
                    {
                        Name = "changedIndex",
                        Description = "The index of the target block for comparison."
                    },
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "accountAddress",
                        Description = "The target accountAddress."
                    }
                ),
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("accountDiffs");
                    var baseIndex = context.GetArgument<long>("baseIndex");
                    var changedIndex = context.GetArgument<long>("changedIndex");
                    var accountAddress = context.GetArgument<Address>("accountAddress");

                    var blockInterval = Math.Abs(changedIndex - baseIndex);
                    if (blockInterval >= 30 || blockInterval == 0)
                    {
                        throw new ExecutionError(
                            "Interval between baseIndex and changedIndex should not be greater than 30 or zero"
                        );
                    }

                    var baseBlockStateRootHash = blockChainRepository.GetBlock(baseIndex).StateRootHash.ToString();
                    var changedBlockStateRootHash = blockChainRepository.GetBlock(changedIndex).StateRootHash.ToString();

                    var baseStateRootHash = HashDigest<SHA256>.FromString(baseBlockStateRootHash);
                    var targetStateRootHash = HashDigest<SHA256>.FromString(
                        changedBlockStateRootHash
                    );

                    return stateTrieRepository.CompareStateAccountTrie(baseStateRootHash, targetStateRootHash, accountAddress);
                }
            );

            Field<ByteStringType>(
                name: "state",
                arguments: new QueryArguments(
                    new QueryArgument<ByteStringType> { Name = "hash", Description = "The hash of the block used to fetch state from chain." },
                    new QueryArgument<LongGraphType> { Name = "index", Description = "The index of the block used to fetch state from chain." },
                    new QueryArgument<NonNullGraphType<AddressType>> { Name = "accountAddress", Description = "The address of account to fetch from the chain." },
                    new QueryArgument<NonNullGraphType<AddressType>> { Name = "address", Description = "The address of state to fetch from the account." }
                ),
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("state");
                    var block = (context.GetArgument<byte[]?>("hash"), context.GetArgument<long?>("index")) switch
                    {
                        (not null, not null) => throw new ArgumentException(
                            "Only one of 'hash' and 'index' must be given."),
                        (null, { } index) => blockChainRepository.GetBlock(index),
                        ({ } bytes, null) => blockChainRepository.GetBlock(new BlockHash(bytes)),
                        (null, null) => blockChainRepository.GetTip(),
                    };
                    var accountAddress = context.GetArgument<Address>("accountAddress");
                    var address = context.GetArgument<Address>("address");

                    activity?
                        .AddTag("BlockHash", block.Hash.ToString())
                        .AddTag("Address", address.ToString());
                    var state = worldStateRepository
                        .GetWorldState(block.StateRootHash)
                        .GetAccountState(accountAddress)
                        .GetState(address);

                    if (state is null)
                    {
                        return null;
                    }

                    return new Codec().Encode(state);
                }
            );

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<TransferNCGHistoryType>>>>(
                "transferNCGHistories",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ByteStringType>>
                    {
                        Name = "blockHash"
                    },
                    new QueryArgument<AddressType>
                    {
                        Name = "recipient"
                    }
                ), resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("transferNCGHistories");
                    BlockHash blockHash = new BlockHash(context.GetArgument<byte[]>("blockHash"));

                    activity?.AddTag("BlockHash", blockHash.ToString());

                    var block = blockChainRepository.GetBlock(blockHash);

                    var recipient = context.GetArgument<Address?>("recipient");

                    var filtered = block.Transactions
                        .Where(tx => tx.Actions.Count == 1)
                        .Where(tx =>
                            tx.Actions[0] is Dictionary dictionary && dictionary.ContainsKey("type_id") &&
                            dictionary["type_id"] is Text typeId && typeId == TransferAsset.TypeIdentifier)
                        .Select(tx =>
                        (
                            transactionRepository.GetTxExecution(blockHash, tx.Id) ??
                            throw new InvalidOperationException($"TxExecution {tx.Id} not found."),
                            ToAction(tx.Actions[0])
                        ))
                        .Where(pair => pair.Item2 is ITransferAsset)
                        .Select(pair => (pair.Item1!, (ITransferAsset)pair.Item2))
                        .Where(pair => !pair.Item1.Fail &&
                                       (!recipient.HasValue || pair.Item2.Recipient == recipient) &&
                                       pair.Item2.Amount.Currency.Ticker == "NCG");

                    var histories = filtered.Select(pair =>
                        new TransferNCGHistory(
                            pair.Item1.BlockHash,
                            pair.Item1.TxId,
                            pair.Item2.Sender,
                            pair.Item2.Recipient,
                            pair.Item2.Amount,
                            pair.Item2.Memo));

                    return histories;
                });

            Field<KeyStoreType>(
                name: "keyStore",
                deprecationReason: "Use `planet key` command instead.  https://www.npmjs.com/package/@planetarium/cli",
                resolve: context => keyStore
            ).AuthorizeWithLocalPolicyIf(useSecretToken);

            Field<NonNullGraphType<NodeStatusType>>(
                name: "nodeStatus",
                resolve: _ =>
                {
                    using var activity = ActivitySource.StartActivity("nodeStatus");
                    return standaloneContext.NodeStatus;
                });

            Field<NonNullGraphType<Libplanet.Explorer.Queries.ExplorerQuery>>(
                name: "chainQuery",
                deprecationReason: "Use /graphql/explorer",
                resolve: _ => new object()
            );

            Field<NonNullGraphType<ValidationQuery>>(
                name: "validation",
                description: "The validation method provider for Libplanet types.",
                resolve: _ => new object()
            );

            Field<NonNullGraphType<ActivationStatusQuery>>(
                    name: "activationStatus",
                    description: "Check if the provided address is activated.",
                    deprecationReason: "Since NCIP-15, it doesn't care account activation.",
                    resolve: _ => new object())
                .AuthorizeWithLocalPolicyIf(useSecretToken);

            Field<NonNullGraphType<PeerChainStateQuery>>(
                name: "peerChainState",
                description: "Get the peer's block chain state",
                resolve: _ => new object());

            Field<NonNullGraphType<StringGraphType>>(
                name: "goldBalance",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>> { Name = "address", Description = "Target address to query" },
                    new QueryArgument<ByteStringType> { Name = "hash", Description = "Offset block hash for query." }
                ),
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("goldBalance");
                    Address address = context.GetArgument<Address>("address");
                    byte[] blockHashByteArray = context.GetArgument<byte[]>("hash");
                    var block = blockHashByteArray is null
                        ? blockChainRepository.GetTip()
                        : blockChainRepository.GetBlock(new BlockHash(blockHashByteArray));
                    var worldState = worldStateRepository.GetWorldState(block.StateRootHash);
                    Currency currency = new GoldCurrencyState(
                        (Dictionary)worldState
                            .GetLegacyState(GoldCurrencyState.Address)
                    ).Currency;

                    activity?
                        .AddTag("BlockHash", block.Hash.ToString())
                        .AddTag("Address", address.ToString());
                    return worldState.GetBalance(
                        address,
                        currency
                    ).GetQuantityString();
                }
            );

            Field<NonNullGraphType<LongGraphType>>(
                name: "nextTxNonce",
                deprecationReason: "The root query is not the best place for nextTxNonce so it was moved. " +
                                   "Use transaction.nextTxNonce()",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>> { Name = "address", Description = "Target address to query" }
                ),
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("nextTxNonce");

                    Address address = context.GetArgument<Address>("address");
                    activity?.AddTag("Address", address.ToString());
                    return transactionRepository.GetNextTxNonce(address);
                }
            );

            Field<TransactionType>(
                name: "getTx",
                deprecationReason: "The root query is not the best place for getTx so it was moved. " +
                                   "Use transaction.getTx()",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<TxIdType>>
                    { Name = "txId", Description = "transaction id." }
                ),
                resolve: context =>
                {
                    var txId = context.GetArgument<TxId>("txId");
                    return transactionRepository.GetTransaction(txId);
                }
            );

            Field<ReplayResultType>(
                name: "replayTransaction",
                description: "Replicates ReplayCommand: given a transaction id, returns block info and random seed.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<TxIdType>>
                    {
                        Name = "txId",
                        Description = "Transaction id to replay."
                    }
                ),
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("replayTransaction");

                    var txId = context.GetArgument<TxId>("txId");

                    if (standaloneContext.Store is null)
                    {
                        throw new ExecutionError("Store is not ready");
                    }

                    var store = standaloneContext.Store;
                    var transaction = store.GetTransaction(txId);
                    if (transaction is null)
                    {
                        throw new ExecutionError($"Transaction {txId} not found.");
                    }

                    var blockHash = store.GetFirstTxIdBlockHashIndex(txId);
                    if (blockHash is null)
                    {
                        throw new ExecutionError($"Block containing transaction {txId} not found.");
                    }

                    var block = store.GetBlock(blockHash.Value);
                    if (block is null)
                    {
                        throw new ExecutionError($"Block {blockHash} not found.");
                    }

                    var digest = store.GetBlockDigest(blockHash.Value);
                    if (digest is null)
                    {
                        throw new ExecutionError($"Block digest for {blockHash} not found.");
                    }

                    var header = digest.Value.GetHeader();
                    var preEvaluationHash = header.PreEvaluationHash;
                    if (transaction.Signature is null)
                    {
                        throw new ExecutionError("Transaction signature is null.");
                    }

                    byte[] preEvaluationHashBytes = preEvaluationHash.ToByteArray();
                    int randomSeed = ActionEvaluator.GenerateRandomSeed(preEvaluationHashBytes, transaction.Signature, 0);

                    string previousState = string.Empty;
                    string nextState = string.Empty;
                    if (header.PreviousHash is { } prevHash)
                    {
                        var previousBlock = store.GetBlock(prevHash);
                        if (previousBlock is { })
                        {
                            previousState = previousBlock.StateRootHash.ToString();
                        }
                    }

                    // Next state is the current block's state root hash after processing
                    nextState = block.StateRootHash.ToString();

                    activity?
                        .AddTag("TxId", txId.ToString())
                        .AddTag("BlockHash", block.Hash.ToString());

                    return new ReplayResult
                    {
                        BlockIndex = block.Index,
                        BlockProtocolVersion = block.ProtocolVersion,
                        Miner = block.Miner,
                        PreviousState = previousState,
                        NextState = nextState,
                        RandomSeed = randomSeed,
                        Signer = transaction.Signer,
                        TxId = txId.ToString(),
                    };
                }
            );

            Field<AddressType>(
                name: "minerAddress",
                description: "Address of current node.",
                resolve: context =>
                {
                    if (standaloneContext.NineChroniclesNodeService?.MinerPrivateKey is null)
                    {
                        throw new ExecutionError(
                            $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.NineChroniclesNodeService)}.{nameof(StandaloneContext.NineChroniclesNodeService.MinerPrivateKey)} is null.");
                    }

                    return standaloneContext.NineChroniclesNodeService.MinerPrivateKey.Address;
                });

            Field<MonsterCollectionStatusType>(
                name: nameof(MonsterCollectionStatus),
                arguments: new QueryArguments(
                    new QueryArgument<AddressType>
                    {
                        Name = "address",
                        Description = "agent address.",
                        DefaultValue = null
                    }
                ),
                description: "Get monster collection status by address.",
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity(nameof(MonsterCollectionStatus));
                    if (!(standaloneContext.BlockChain is BlockChain blockChain))
                    {
                        throw new ExecutionError(
                            $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)} was not set yet!");
                    }

                    Address? address = context.GetArgument<Address?>("address");
                    Address agentAddress;
                    if (address is null)
                    {
                        if (standaloneContext.NineChroniclesNodeService?.MinerPrivateKey is null)
                        {
                            throw new ExecutionError(
                                $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.NineChroniclesNodeService)}.{nameof(StandaloneContext.NineChroniclesNodeService.MinerPrivateKey)} is null.");
                        }

                        agentAddress = standaloneContext.NineChroniclesNodeService!.MinerPrivateKey!.Address;
                    }
                    else
                    {
                        agentAddress = (Address)address;
                    }

                    HashDigest<SHA256> offset = blockChainRepository.GetTip().StateRootHash;
                    activity?
                        .AddTag("BlockHash", offset.ToString())
                        .AddTag("Address", address.ToString());
                    IWorldState worldState = worldStateRepository.GetWorldState(offset);
#pragma warning disable S3247
                    if (worldState.GetAgentState(agentAddress) is { } agentState)
#pragma warning restore S3247
                    {
                        Address deriveAddress =
                            MonsterCollectionState.DeriveAddress(agentAddress, agentState.MonsterCollectionRound);
                        Currency currency = new GoldCurrencyState(
                            (Dictionary)worldState.GetLegacyState(Addresses.GoldCurrency)).Currency;

                        FungibleAssetValue balance = worldState.GetBalance(agentAddress, currency);
                        if (worldState.GetLegacyState(deriveAddress) is Dictionary mcDict)
                        {
                            var rewardSheet = new MonsterCollectionRewardSheet();
                            var csv = worldState.GetLegacyState(
                                Addresses.GetSheetAddress<MonsterCollectionRewardSheet>()).ToDotnetString();
                            rewardSheet.Set(csv);
                            var monsterCollectionState = new MonsterCollectionState(mcDict);
                            long tipIndex = blockChain.Tip.Index;
                            List<MonsterCollectionRewardSheet.RewardInfo> rewards =
                                monsterCollectionState.CalculateRewards(rewardSheet, tipIndex);
                            return new MonsterCollectionStatus(
                                balance,
                                rewards,
                                tipIndex,
                                monsterCollectionState.IsLocked(tipIndex)
                            );
                        }
                        throw new ExecutionError(
                            $"{nameof(MonsterCollectionState)} Address: {deriveAddress} is null.");
                    }

                    throw new ExecutionError(
                        $"{nameof(AgentState)} Address: {agentAddress} is null.");
                });

            Field<NonNullGraphType<TransactionHeadlessQuery>>(
                name: "transaction",
                description: "Query for transaction.",
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("transaction");
                    return new object();
                });

            Field<NonNullGraphType<BooleanGraphType>>(
                name: "activated",
                deprecationReason: "Since NCIP-15, it doesn't care account activation.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "invitationCode"
                    }
                ),
                resolve: context =>
                {
                    if (!(standaloneContext.BlockChain is BlockChain blockChain))
                    {
                        throw new ExecutionError(
                            $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)} was not set yet!");
                    }

                    var worldState = worldStateRepository.GetWorldState(blockChainRepository.GetTip().StateRootHash);
                    string invitationCode = context.GetArgument<string>("invitationCode");
                    ActivationKey activationKey = ActivationKey.Decode(invitationCode);
                    if (worldState.GetLegacyState(activationKey.PendingAddress) is Dictionary dictionary)
                    {
                        var pending = new PendingActivationState(dictionary);
                        var signature = activationKey.PrivateKey.Sign(pending.Nonce);
                        if (pending.Verify(signature))
                        {
                            return false;
                        }

                        throw new ExecutionError($"invitationCode is invalid.");
                    }

                    return true;
                }
            );

            Field<NonNullGraphType<StringGraphType>>(
                name: "activationKeyNonce",
                deprecationReason: "Since NCIP-15, it doesn't care account activation.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "invitationCode"
                    }
                ),
                resolve: context =>
                {
                    ActivationKey activationKey;
                    try
                    {
                        string invitationCode = context.GetArgument<string>("invitationCode");
                        invitationCode = invitationCode.TrimEnd();
                        activationKey = ActivationKey.Decode(invitationCode);
                    }
                    catch (Exception)
                    {
                        throw new ExecutionError("invitationCode format is invalid.");
                    }

                    var worldState = worldStateRepository.GetWorldState(blockChainRepository.GetTip().StateRootHash);
                    if (worldState.GetLegacyState(activationKey.PendingAddress) is Dictionary dictionary)
                    {
                        var pending = new PendingActivationState(dictionary);
                        return ByteUtil.Hex(pending.Nonce);
                    }

                    throw new ExecutionError("invitationCode is invalid.");
                }
            );

            Field<NonNullGraphType<RpcInformationQuery>>(
                name: "rpcInformation",
                description: "Query for rpc mode information.",
                resolve: _ => new object()
            );

            Field<NonNullGraphType<ActionQuery>>(
                name: "actionQuery",
                description: "Query to create action transaction.",
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("actionQuery");
                    return new object();
                });

            Field<NonNullGraphType<ActionTxQuery>>(
                name: "actionTxQuery",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "publicKey",
                        Description = "The hexadecimal string of public key for Transaction.",
                    },
                    new QueryArgument<LongGraphType>
                    {
                        Name = "nonce",
                        Description = "The nonce for Transaction.",
                    },
                    new QueryArgument<DateTimeOffsetGraphType>
                    {
                        Name = "timestamp",
                        Description = "The time this transaction is created.",
                    },
                    new QueryArgument<FixedFungibleAssetValueInputType>
                    {
                        Name = "maxGasPrice",
                        DefaultValue = FungibleAssetValue.Parse(Currencies.Mead, "0.00001"),
                    }
                ),
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("actionTxQuery");
                    return new object();
                });

            Field<NonNullGraphType<AddressQuery>>(
                name: "addressQuery",
                description: "Query to get derived address.",
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("addressQuery");
                    return new object();
                });
            
            Field<NonNullGraphType<SimultionQuery>>(name: "simulationQuery", arguments: new QueryArguments(
                new QueryArgument<ByteStringType>
                {
                    Name = "hash",
                    Description = "Offset block hash for query.",
                },
                new QueryArgument<LongGraphType>
                {
                    Name = "index",
                    Description = "Offset block index for query."
                }),
                resolve: context =>
                {
                    using var activity = ActivitySource.StartActivity("stateQuery");
                    BlockHash blockHash = (context.GetArgument<byte[]?>("hash"), context.GetArgument<long?>("index")) switch
                    {
                        ({ } bytes, null) => new BlockHash(bytes),
                        (null, { } index) => standaloneContext.BlockChain[index].Hash,
                        (not null, not null) => throw new ArgumentException("Only one of 'hash' and 'index' must be given."),
                        (null, null) => standaloneContext.BlockChain.Tip.Hash,
                    };
                    activity?.AddTag("BlockHash", blockHash.ToString());

                    if (!(standaloneContext.BlockChain is { } chain))
                    {
                        return null;
                    }

                    if (!(blockHash is { } hash))
                    {
                        return null;
                    }

                    return new StateContext(
                        chain.GetWorldState(blockHash),
                        chain[blockHash].Index,
                        stateMemoryCache
                    );
                }
            );

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<BlockStartingTxNoncesType>>>>(
                name: "blockStartingTxNoncesQuery",
                description: "Query to get starting tx nonces from certain block.",
                arguments: new QueryArguments(
                    new QueryArgument<ByteStringType> { Name = "hash", Description = "The hash of the block used to fetch state from chain." },
                    new QueryArgument<LongGraphType> { Name = "index", Description = "The index of the block used to fetch state from chain." }
                ),
                resolve: context =>
                {
                    var block = (context.GetArgument<byte[]?>("hash"), context.GetArgument<long?>("index")) switch
                    {
                        (not null, not null) => throw new ArgumentException(
                            "Only one of 'hash' and 'index' must be given."),
                        (null, { } index) => blockChainRepository.GetBlock(index),
                        ({ } bytes, null) => blockChainRepository.GetBlock(new BlockHash(bytes)),
                        (null, null) => blockChainRepository.GetTip(),
                    };
                    using var activity = ActivitySource.StartActivity("blockStartingTxNoncesQuery");
                    return block.Transactions
                        .GroupBy(tx => tx.Signer)
                        .Select(group => (
                            Signer: group.Key,
                            Nonce: group.Min(tx => tx.Nonce)))
                        .OrderBy(x => x.Signer)
                        .ToArray();
                }
            );

            Field<ListGraphType<Abstractions.ArenaEventBaseType>>(
                "arenaBattleData",
                description: "All of the information to playback an arena battle.",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<TxIdType>>
                {
                    Name = "transactionId",
                    Description = "Transaction of the battle."
                }),
                resolve: context =>
                {
                    var sw = Stopwatch.StartNew();
                    var transactionId = context.GetArgument<TxId>("transactionId");

                    if (!(standaloneContext.Store is { } store))
                    {
                        throw new InvalidOperationException("Store is not ready");
                    }
                    var transaction = store.GetTransaction(transactionId);

                    if (transaction == null)
                    {
                        return null;
                    }

                    var action = transaction.Actions?.Select(a => ToAction(a)).FirstOrDefault(); // CS8602
                    if (action == null)
                    {
                        throw new InvalidOperationException("Action is null.");
                    }
                    if (action.GetType() != typeof(Nekoyume.Action.Arena.Battle))
                    {
                        throw new InvalidOperationException("Wrong Transaction Type, please choose a BattleArena action");
                    }
                    var innerAction = action as Nekoyume.Action.Arena.Battle;
                    if (innerAction == null)
                    {
                        throw new InvalidOperationException("Inner action is null");
                    }
                    var blockHash = store.GetFirstTxIdBlockHashIndex(transactionId);

                    if (blockHash == null)
                    {
                        throw new InvalidOperationException("Block Hash is null");
                    }

                    var block = store.GetBlock((BlockHash)blockHash);
                    if (block == null)
                    {
                        throw new InvalidOperationException("Block is null");
                    }

                    var digest = store.GetBlockDigest(blockHash.Value);
                    if (digest == null)
                    {
                        throw new InvalidOperationException("Block Digest is null.");
                    }
                    if (!(standaloneContext.BlockChain is { } chain))
                    {
                        return null;
                    }
                    var header = digest.Value.GetHeader();
                    if (header == null)
                    {
                        throw new InvalidOperationException("Block Header is null.");
                    }
                    var preEvaluationHash = header.PreEvaluationHash;
                    if (transaction.Signature == null)
                    {
                        throw new InvalidOperationException("Transaction Signature is null.");
                    }
                    byte[] hashedSignature;
                    using (var hasher = System.Security.Cryptography.SHA1.Create())
                    {
                        hashedSignature = hasher.ComputeHash(transaction.Signature);
                    }
                    byte[] preEvaluationHashBytes = preEvaluationHash.ToByteArray();
                    int seed =
                    (preEvaluationHashBytes.Length > 0
                        ? BitConverter.ToInt32(preEvaluationHashBytes, 0) : 0)
                    ^ BitConverter.ToInt32(hashedSignature, 0);

                    var random = new LocalRandom(seed);

                    var previousHash = header.PreviousHash;
                    if (!(previousHash is BlockHash))
                    {
                        throw new InvalidOperationException("Previous BlockHash missing.");
                    }
                    var accountState = chain.GetWorldState((BlockHash)previousHash);

                    var myAvatarAddress = innerAction.myAvatarAddress;

                    var myAgentAddress = transaction.Signer;

                    var myArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(myAvatarAddress);
                    var enemyAvatarAddress = innerAction.enemyAvatarAddress;

                    var enemyArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(enemyAvatarAddress);

                    var costumes = innerAction.costumes;
                    var equipment = innerAction.equipments;

                    var addressesHex = GetSignerAndOtherAddressesHex(
                        myAgentAddress,
                        myAvatarAddress,
                        enemyAvatarAddress
                    );

                    var myAvatarState = MeasureAndLog(
                        "Validate and get avatar state",
                        sw,
                        () => ValidateAndGetMyAvatarState(accountState, myAgentAddress, myAvatarAddress, enemyAvatarAddress)
                    );

                    var collectionStates = MeasureAndLog(
                        "Get collection states",
                        sw,
                        () => accountState.GetCollectionStates(new[] { myAvatarAddress, enemyAvatarAddress })
                    );

                    var gameConfigState = MeasureAndLog(
                        "Load gameConfig",
                        sw,
                        () => accountState.GetGameConfigState()
                    );

                    var sheets = MeasureAndLog(
                        "Load sheets",
                        sw,
                        () => LoadSheetsArenaBattle(accountState, collectionStates.Any())
                    );

                    var collectionModifiers = new Dictionary<Address, List<StatModifier>>
                    {
                        [myAvatarAddress] = new(),
                        [enemyAvatarAddress] = new(),
                    };

                    if (collectionStates.Any())
                    {
                        var collectionSheet = sheets.GetSheet<CollectionSheet>();
                        foreach (var (address, state) in collectionStates)
                        {
                            collectionModifiers[address] = state.GetModifiers(collectionSheet);
                        }
                    }

                    var myLoadout = MeasureAndLog(
                        "Get my spec",
                        sw,
                        () =>
                            PrepareMyLoadout(
                                accountState,
                                sheets,
                                myAvatarState,
                                block.Index,
                                addressesHex,
                                gameConfigState,
                                collectionModifiers,
                                myAvatarAddress,
                                costumes,
                                equipment
                            )
                    );
                    var (updatedStates, myItemSlotState, myRuneSlotState, myRuneStates, myCp) = myLoadout;

                    var enemyLoadout = MeasureAndLog(
                        "Get enemy spec",
                        sw,
                        () => PrepareEnemyLoadout(accountState, enemyAvatarAddress)
                    );

                    var resultLog = MeasureAndLog(
                        "Simulate battle",
                        sw,
                        () =>
                            Simulate(
                                accountState,
                                sheets,
                                myAvatarState,
                                random,
                                gameConfigState,
                                collectionModifiers,
                                (myItemSlotState, myRuneSlotState, myRuneStates),
                                enemyLoadout,
                                myAvatarAddress,
                                enemyAvatarAddress
                            )
                        );

                    return resultLog.Events;
                }
            );
        }

        private T MeasureAndLog<T>(string process, Stopwatch stopwatch, Func<T> action)
        {
            stopwatch.Restart();
            var result = action();
            stopwatch.Stop();
            Log.Verbose(
                "{Process} completed in {Elapsed} ms",
                process,
                stopwatch.Elapsed.TotalMilliseconds
            );
            return result;
        }
        private Dictionary<Type, (Address address, ISheet sheet)> LoadSheetsArenaBattle(
            IWorldState states,
            bool collectionExist
        )
        {
            var sheetTypes = new List<Type>
            {
                typeof(ArenaSheet),
                typeof(ItemRequirementSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(MaterialItemSheet),
                typeof(RuneListSheet),
                typeof(RuneLevelBonusSheet),
                typeof(BuffLimitSheet),
                typeof(BuffLinkSheet),
                typeof(CharacterSheet),
                typeof(CostumeStatSheet),
            };

            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }

            var sheets = states.GetSheets(
                containArenaSimulatorSheets: true,
                sheetTypes: sheetTypes
            );
            return sheets;
        }

        private AvatarState ValidateAndGetMyAvatarState(IWorldState states, Address signer, Address myAvatarAddress, Address enemyAvatarAddress)
        {
            if (myAvatarAddress.Equals(enemyAvatarAddress))
            {
                throw new InvalidAddressException("Battle initiated with identical addresses.");
            }

            if (!states.TryGetAvatarState(signer, myAvatarAddress, out var myAvatarState))
            {
                throw new FailedLoadStateException("Failed to load avatar state for signer.");
            }

            return myAvatarState;
        }

        protected string GetSignerAndOtherAddressesHex(Address agentAddress, params Address[] addresses)
        {
            StringBuilder sb = new StringBuilder($"[{agentAddress.ToHex()}");

            foreach (Address address in addresses)
            {
                sb.Append($", {address.ToHex()}");
            }

            sb.Append("]");
            return sb.ToString();
        }

        private (
            IWorldState UpdatedStates,
            ItemSlotState ItemSlotState,
            RuneSlotState RuneSlotState,
            AllRuneState RuneStates,
            int Cp
        ) PrepareMyLoadout(
            IWorldState states,
            Dictionary<Type, (Address address, ISheet sheet)> sheets,
            AvatarState myAvatarState,
            long? blockIndex,
            string addressesHex,
            GameConfigState gameConfigState,
            Dictionary<Address, List<StatModifier>> collectionModifiers,
            Address myAvatarAddress,
            List<Guid> costumes,
            List<Guid> equipments
        )
        {

            if(blockIndex is null)
            {
                throw new InvalidAddressException("BlockIndex is NULL.");
            }

            long blockIndexF = (long)blockIndex;

            var (equipmentItems, costumeItems) = myAvatarState.ValidEquipmentAndCostumeV2(
                costumes,
                equipments,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                blockIndexF,
                addressesHex,
                gameConfigState
            );

            var myRuneSlotStateAddress = RuneSlotState.DeriveAddress(
                myAvatarAddress,
                BattleType.Arena
            );
            var myRuneSlotState = states.TryGetLegacyState(
                myRuneSlotStateAddress,
                out List rawRuneSlotState
            )
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);

            var runeListSheet = sheets.GetSheet<RuneListSheet>();

            var myItemSlotStateAddress = ItemSlotState.DeriveAddress(
                myAvatarAddress,
                BattleType.Arena
            );
            var myItemSlotState = states.TryGetLegacyState(
                myItemSlotStateAddress,
                out List rawItemSlotState
            )
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Arena);

            myItemSlotState.UpdateEquipment(equipments);
            myItemSlotState.UpdateCostumes(costumes);

            var myRuneStates = states.GetRuneState(myAvatarAddress, out var migrateRequired);

            var characterSheet = sheets.GetSheet<CharacterSheet>();
            if (!characterSheet.TryGetValue(myAvatarState.characterId, out var myCharacterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", myAvatarState.characterId);
            }

            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var myRuneOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeInfo in myRuneSlotState.GetEquippedRuneSlotInfos())
            {
                if (!myRuneStates.TryGetRuneState(runeInfo.RuneId, out var runeState))
                {
                    continue;
                }

                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.RuneId);
                }

                if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.Level);
                }

                myRuneOptions.Add(option);
            }

            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var runeLevelBonusSheet = sheets.GetSheet<RuneLevelBonusSheet>();
            var myRuneLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                myRuneStates,
                runeListSheet,
                runeLevelBonusSheet
            );
            var myCp = CPHelper.TotalCP(
                equipmentItems,
                costumeItems,
                myRuneOptions,
                myAvatarState.level,
                myCharacterRow,
                costumeStatSheet,
                collectionModifiers[myAvatarAddress],
                myRuneLevelBonus
            );

            return (states, myItemSlotState, myRuneSlotState, myRuneStates, myCp);
        }

        private (
            ItemSlotState ItemSlotState,
            RuneSlotState RuneSlotState,
            AllRuneState RuneStates
        ) PrepareEnemyLoadout(IWorldState states, Address enemyAvatarAddress)
        {
            var enemyItemSlotStateAddress = ItemSlotState.DeriveAddress(
                enemyAvatarAddress,
                BattleType.Arena
            );
            var enemyItemSlotState = states.TryGetLegacyState(
                enemyItemSlotStateAddress,
                out List rawEnemyItemSlotState
            )
                ? new ItemSlotState(rawEnemyItemSlotState)
                : new ItemSlotState(BattleType.Arena);

            var enemyRuneSlotStateAddress = RuneSlotState.DeriveAddress(
                enemyAvatarAddress,
                BattleType.Arena
            );
            var enemyRuneSlotState = states.TryGetLegacyState(
                enemyRuneSlotStateAddress,
                out List enemyRawRuneSlotState
            )
                ? new RuneSlotState(enemyRawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);

            var enemyRuneStates = states.GetRuneState(enemyAvatarAddress, out _);

            return (enemyItemSlotState, enemyRuneSlotState, enemyRuneStates);
        }

        private ArenaLog Simulate(
            IWorldState states,
            Dictionary<Type, (Address address, ISheet sheet)> sheets,
            AvatarState myAvatarState,
            IRandom random,
            GameConfigState gameConfigState,
            Dictionary<Address, List<StatModifier>> collectionModifiers,
            (
                ItemSlotState ItemSlotState,
                RuneSlotState RuneSlotState,
                AllRuneState RuneStates
            ) mySpec,
            (
                ItemSlotState ItemSlotState,
                RuneSlotState RuneSlotState,
                AllRuneState RuneStates
            ) enemySpec,
            Address myAvatarAddress,
            Address enemyAvatarAddress
        )
        {
            var myArenaPlayerDigest = new ArenaPlayerDigest(
                myAvatarState,
                mySpec.ItemSlotState.Equipments,
                mySpec.ItemSlotState.Costumes,
                mySpec.RuneStates,
                mySpec.RuneSlotState
            );
            var enemyAvatarState = states.GetEnemyAvatarState(enemyAvatarAddress);
            var enemyArenaPlayerDigest = new ArenaPlayerDigest(
                enemyAvatarState,
                enemySpec.ItemSlotState.Equipments,
                enemySpec.ItemSlotState.Costumes,
                enemySpec.RuneStates,
                enemySpec.RuneSlotState
            );

            var buffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
            var buffLinkSheet = sheets.GetSheet<BuffLinkSheet>();
            var simulator = new ArenaSimulator(
                random,
                5,
                gameConfigState.ShatterStrikeMaxDamage
            );
            return simulator.Simulate(
                myArenaPlayerDigest,
                enemyArenaPlayerDigest,
                sheets.GetArenaSimulatorSheets(),
                collectionModifiers[myAvatarAddress],
                collectionModifiers[enemyAvatarAddress],
                buffLimitSheet,
                buffLinkSheet,
                true
            );

           
        }
    }
}
