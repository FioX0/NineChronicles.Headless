using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Explorer.GraphTypes;
using Libplanet.Types.Assets;
using Libplanet.Types.Tx;
using Microsoft.Extensions.Configuration;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Serilog;
using System;
using System.Diagnostics;
using Nekoyume.Module;

namespace NineChronicles.Headless.GraphTypes
{
    public class StandaloneMutation : ObjectGraphType
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource("NineChronicles.Headless.GraphTypes.StandaloneMutation");

        public StandaloneMutation(
            StandaloneContext standaloneContext,
            NineChroniclesNodeService nodeService,
            IConfiguration configuration
        )
        {
            if (configuration[GraphQLService.SecretTokenKey] is { })
            {
                this.AuthorizeWith(GraphQLService.LocalPolicyKey);
            }
            else if (Convert.ToBoolean(configuration.GetSection("Jwt")["EnableJwtAuthentication"]))
            {
                this.AuthorizeWith(GraphQLService.JwtPolicyKey);
            }

            Field<KeyStoreMutation>(
                name: "keyStore",
                deprecationReason: "Use `planet key` command instead.  https://www.npmjs.com/package/@planetarium/cli",
                resolve: context => standaloneContext.KeyStore);

            Field<ActionMutation>(
                name: "action",
                resolve: _ => new ActionMutation(nodeService));

            Field<NonNullGraphType<BooleanGraphType>>(
                name: "blacklistAgent",
                description: "Persistently block an agent from staging transactions on configured nodes.",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<AddressType>> { Name = "agentAddress" }),
                resolve: context =>
                {
                    var address = context.GetArgument<Address>("agentAddress");
                    standaloneContext.AgentBlacklist.Add(address);
                    // Drop already-staged blocked transactions on this node as part of the update.
                    _ = standaloneContext.BlockChain.StagePolicy.Iterate(standaloneContext.BlockChain, filtered: false);
                    return standaloneContext.AgentBlacklist.IsBlocked(address);
                })
                .AuthorizeWith(GraphQLService.AgentBlacklistAdminPolicyKey);

            Field<NonNullGraphType<BooleanGraphType>>(
                name: "unblacklistAgent",
                description: "Remove an agent from the persistent transaction blacklist.",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<AddressType>> { Name = "agentAddress" }),
                resolve: context =>
                {
                    var address = context.GetArgument<Address>("agentAddress");
                    standaloneContext.AgentBlacklist.Remove(address);
                    return !standaloneContext.AgentBlacklist.IsBlocked(address);
                })
                .AuthorizeWith(GraphQLService.AgentBlacklistAdminPolicyKey);

            Field<NonNullGraphType<TxIdType>>(
                name: "stageTransaction",
                description: "Add a new transaction to staging and return TxId",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "payload",
                        Description = "The hexadecimal string of the transaction to stage."
                    }
                ),
                resolve: context =>
                {
                    try
                    {
                        using var activity = ActivitySource.StartActivity("stageTransaction");
                        byte[] bytes = ByteUtil.ParseHex(context.GetArgument<string>("payload"));
                        Transaction tx = Transaction.Deserialize(bytes);
                        if (standaloneContext.AgentBlacklist.IsBlocked(tx.Signer))
                        {
                            throw new ExecutionError("Agent is blocked by this node.");
                        }

                        NineChroniclesNodeService? service = standaloneContext.NineChroniclesNodeService;
                        BlockChain? blockChain = service?.Swarm.BlockChain;

                        if (blockChain is null)
                        {
                            throw new InvalidOperationException($"{nameof(blockChain)} is null.");
                        }

                        Exception? validationExc = blockChain.Policy.ValidateNextBlockTx(blockChain, tx);
                        if (validationExc is null)
                        {
                            bool staged = blockChain.StageTransaction(tx);
                            if (blockChain.StagePolicy.Get(blockChain, tx.Id, filtered: false) is null)
                            {
                                throw new ExecutionError("Transaction rejected by staging policy.");
                            }

                            // A duplicate already in the pool can be acknowledged, but only a
                            // newly staged transaction may be broadcast by this request.
                            if (staged && service?.Swarm is { } swarm && swarm.Running)
                            {
                                swarm.BroadcastTxs(new[] { tx });
                            }

                            return tx.Id;
                        }

                        throw new ExecutionError(
                            $"The given transaction is invalid. (due to: {validationExc.Message})",
                            validationExc
                        );
                    }
                    catch (ExecutionError)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        throw new ExecutionError($"An unexpected exception occurred. {e.Message}");
                    }
                }
            );
        }
    }
}
