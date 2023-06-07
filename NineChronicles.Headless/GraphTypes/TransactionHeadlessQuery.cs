using System;
using System.Collections.Immutable;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using Lib9c;
using Libplanet.Blockchain;
using Libplanet.Action;
using Libplanet.Tx;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Explorer.GraphTypes;
using Nekoyume.Action;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Store;

namespace NineChronicles.Headless.GraphTypes
{
    class TransactionHeadlessQuery : ObjectGraphType
    {
        public TransactionHeadlessQuery(StandaloneContext standaloneContext)
        {
            Field<NonNullGraphType<LongGraphType>>(
                name: "nextTxNonce",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>> { Name = "address", Description = "Target address to query" }
                ),
                resolve: context =>
                {
                    if (!(standaloneContext.BlockChain is BlockChain<PolymorphicAction<ActionBase>> blockChain))
                    {
                        throw new ExecutionError(
                            $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)} was not set yet!");
                    }

                    Address address = context.GetArgument<Address>("address");
                    return blockChain.GetNextTxNonce(address);
                }
            );

            Field<TransactionType<NCAction>>(
                name: "getTx",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<TxIdType>>
                    { Name = "txId", Description = "transaction id." }
                ),
                resolve: context =>
                {
                    if (!(standaloneContext.BlockChain is BlockChain<PolymorphicAction<ActionBase>> blockChain))
                    {
                        throw new ExecutionError(
                            $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)} was not set yet!");
                    }

                    var txId = context.GetArgument<TxId>("txId");
                    return blockChain.GetTransaction(txId);
                }
            );

            Field<NonNullGraphType<StringGraphType>>(
                name: "createUnsignedTx",
                deprecationReason: "API update with action query. use unsignedTransaction",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "publicKey",
                        Description = "The base64-encoded public key for Transaction.",
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "plainValue",
                        Description = "The base64-encoded plain value of action for Transaction.",
                    },
                    new QueryArgument<LongGraphType>
                    {
                        Name = "nonce",
                        Description = "The nonce for Transaction.",
                    }
                ),
                resolve: context =>
                {
                    if (!(standaloneContext.BlockChain is BlockChain<PolymorphicAction<ActionBase>> blockChain))
                    {
                        throw new ExecutionError(
                            $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)} was not set yet!");
                    }

                    string plainValueString = context.GetArgument<string>("plainValue");
                    var plainValue = new Bencodex.Codec().Decode(System.Convert.FromBase64String(plainValueString));
#pragma warning disable 612
                    var action = new NCAction();
#pragma warning restore 612
                    action.LoadPlainValue(plainValue);

                    var publicKey = new PublicKey(Convert.FromBase64String(context.GetArgument<string>("publicKey")));
                    Address signer = publicKey.ToAddress();
                    long nonce = context.GetArgument<long?>("nonce") ?? blockChain.GetNextTxNonce(signer);
                    UnsignedTx unsignedTransaction =
                        new UnsignedTx(
                            new TxInvoice(
                                genesisHash: blockChain.Genesis.Hash,
                                actions: new TxActionList(new[] { action })),
                            new TxSigningMetadata(publicKey: publicKey, nonce: nonce));
                    return Convert.ToBase64String(unsignedTransaction.SerializeUnsignedTx().ToArray());
                });

            Field<NonNullGraphType<StringGraphType>>(
                name: "attachSignature",
                deprecationReason: "Use signTransaction",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "unsignedTransaction",
                        Description = "The base64-encoded unsigned transaction to attach the given signature."
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "signature",
                        Description = "The base64-encoded signature of the given unsigned transaction."
                    }
                ),
                resolve: context =>
                {
                    byte[] signature = Convert.FromBase64String(context.GetArgument<string>("signature"));
                    IUnsignedTx unsignedTransaction =
                        TxMarshaler.DeserializeUnsignedTx(
                            Convert.FromBase64String(context.GetArgument<string>("unsignedTransaction")));

                    Transaction signedTransaction = new Transaction(
                        unsignedTransaction,
                        signature.ToImmutableArray());

                    return Convert.ToBase64String(signedTransaction.Serialize());
                });

            Field<NonNullGraphType<TxResultType>>(
                name: "transactionResult",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<TxIdType>>
                    { Name = "txId", Description = "transaction id." }
                ),
                resolve: context =>
                {
                    if (!(standaloneContext.BlockChain is BlockChain<PolymorphicAction<ActionBase>> blockChain))
                    {
                        throw new ExecutionError(
                            $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)} was not set yet!");
                    }

                    if (!(standaloneContext.Store is IStore store))
                    {
                        throw new ExecutionError(
                            $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.Store)} was not set yet!");
                    }

                    TxId txId = context.GetArgument<TxId>("txId");
                    if (!(store.GetFirstTxIdBlockHashIndex(txId) is { } txExecutedBlockHash))
                    {
                        return blockChain.GetStagedTransactionIds().Contains(txId)
                            ? new TxResult(TxStatus.STAGING, null, null, null, null, null, null, null, null)
                            : new TxResult(TxStatus.INVALID, null, null, null, null, null, null, null, null);
                    }

                    try
                    {
                        TxExecution execution = blockChain.GetTxExecution(txExecutedBlockHash, txId);
                        Block txExecutedBlock = blockChain[txExecutedBlockHash];
                        return execution switch
                        {
                            TxSuccess txSuccess => new TxResult(TxStatus.SUCCESS, txExecutedBlock.Index,
                                txExecutedBlock.Hash.ToString(), null, null, txSuccess.UpdatedStates, txSuccess.FungibleAssetsDelta, txSuccess.UpdatedFungibleAssets, txSuccess.ActionsLogsList),
                            TxFailure txFailure => new TxResult(TxStatus.FAILURE, txExecutedBlock.Index,
                                txExecutedBlock.Hash.ToString(), txFailure.ExceptionName, txFailure.ExceptionMetadata, null, null, null, null),
                            _ => throw new NotImplementedException(
                                $"{nameof(execution)} is not expected concrete class.")
                        };
                    }
                    catch (Exception)
                    {
                        return new TxResult(TxStatus.INVALID, null, null, null, null, null, null, null, null);
                    }
                }
            );

            Field<NonNullGraphType<ByteStringType>>(
                name: "unsignedTransaction",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "publicKey",
                        Description = "The hexadecimal string of public key for Transaction.",
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "plainValue",
                        Description = "The hexadecimal string of plain value for Action.",
                    },
                    new QueryArgument<LongGraphType>
                    {
                        Name = "nonce",
                        Description = "The nonce for Transaction.",
                    },
                    new QueryArgument<LongGraphType>
                    {
                        Name = "gasLimit",
                        Description = "The gas limit for Transaction.",
                        DefaultValue = RequestPledge.RefillMead,
                    },
                    new QueryArgument<FungibleAssetValueInputType>
                    {
                        Name = "maxGasPrice",
                        DefaultValue = 1 * Currencies.Mead
                    }
                ),
                resolve: context =>
                {
                    if (!(standaloneContext.BlockChain is BlockChain<PolymorphicAction<ActionBase>> blockChain))
                    {
                        throw new ExecutionError(
                            $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)} was not set yet!");
                    }

                    string plainValueString = context.GetArgument<string>("plainValue");
                    var plainValue = new Bencodex.Codec().Decode(ByteUtil.ParseHex(plainValueString));
#pragma warning disable 612
                    var action = new NCAction();
#pragma warning restore 612
                    action.LoadPlainValue(plainValue);

                    var publicKey = new PublicKey(ByteUtil.ParseHex(context.GetArgument<string>("publicKey")));
                    Address signer = publicKey.ToAddress();
                    long nonce = context.GetArgument<long?>("nonce") ?? blockChain.GetNextTxNonce(signer);
                    long? gasLimit = context.GetArgument<long?>("gasLimit");
                    FungibleAssetValue? maxGasPrice = context.GetArgument<FungibleAssetValue?>("maxGasPrice");
                    UnsignedTx unsignedTransaction =
                        new UnsignedTx(
                            new TxInvoice(
                                genesisHash: blockChain.Genesis.Hash,
                                actions: new TxActionList(new[] { action }),
                                gasLimit: gasLimit,
                                maxGasPrice: maxGasPrice),
                            new TxSigningMetadata(publicKey, nonce));
                    return unsignedTransaction.SerializeUnsignedTx().ToArray();
                });

            Field<NonNullGraphType<ByteStringType>>(
                name: "signTransaction",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "unsignedTransaction",
                        Description = "The hexadecimal string of unsigned transaction to attach the given signature."
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "signature",
                        Description = "The hexadecimal string of signature of the given unsigned transaction."
                    }
                ),
                resolve: context =>
                {
                    byte[] signature = ByteUtil.ParseHex(context.GetArgument<string>("signature"));
                    IUnsignedTx unsignedTransaction =
                        TxMarshaler.DeserializeUnsignedTx(
                            ByteUtil.ParseHex(context.GetArgument<string>("unsignedTransaction")));

                    Transaction signedTransaction =
                        new Transaction(unsignedTransaction, signature.ToImmutableArray());

                    return signedTransaction.Serialize();
                }
            );
        }
    }
}
