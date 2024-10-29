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

            Field<NonNullGraphType<TxIdType>>(
                name: "stageTransaction",
                description: "Add a new transaction to staging and return TxId",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "payload",
                        Description = "The hexadecimal string of the transaction to stage."
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "key",
                        Description = "key to allow stageTransaction"
                    }
                ),
                resolve: context =>
                {
                    try
                    {
                        using var activity = ActivitySource.StartActivity("stageTransaction");
                        string key = context.GetArgument<string>("key");
                        if(key != "")
                        {
                            throw new ExecutionError(
                            $"Incorrect StageTransaction key"
                            );
                        }
                        byte[] bytes = ByteUtil.ParseHex(context.GetArgument<string>("payload"));
                        Transaction tx = Transaction.Deserialize(bytes);
                        NineChroniclesNodeService? service = standaloneContext.NineChroniclesNodeService;
                        BlockChain? blockChain = service?.Swarm.BlockChain;

                        if (blockChain is null)
                        {
                            throw new InvalidOperationException($"{nameof(blockChain)} is null.");
                        }

                        Exception? validationExc = blockChain.Policy.ValidateNextBlockTx(blockChain, tx);
                        if (validationExc is null)
                        {
                            blockChain.StageTransaction(tx);

                            if (service?.Swarm is { } swarm && swarm.Running)
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
                    catch (Exception e)
                    {
                        throw new ExecutionError($"An unexpected exception occurred. {e.Message}");
                    }
                }
            );
        }
    }
}
