using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;

namespace NineChronicles.Headless.GraphTypes
{
    public class ReplayResultType : ObjectGraphType<ReplayResult>
    {
        public ReplayResultType()
        {
            Field<NonNullGraphType<LongGraphType>>(
                "blockIndex",
                resolve: context => context.Source.BlockIndex
            );

            Field<NonNullGraphType<IntGraphType>>(
                "blockProtocolVersion",
                resolve: context => context.Source.BlockProtocolVersion
            );

            Field<NonNullGraphType<AddressType>>(
                "miner",
                resolve: context => context.Source.Miner
            );

            Field<NonNullGraphType<StringGraphType>>(
                "previousState",
                resolve: context => context.Source.PreviousState
            );

            Field<NonNullGraphType<StringGraphType>>(
                "nextState",
                resolve: context => context.Source.NextState
            );

            Field<NonNullGraphType<IntGraphType>>(
                "randomSeed",
                resolve: context => context.Source.RandomSeed
            );

            Field<NonNullGraphType<AddressType>>(
                "signer",
                resolve: context => context.Source.Signer
            );

            Field<NonNullGraphType<StringGraphType>>(
                "txId",
                resolve: context => context.Source.TxId
            );
        }
    }
}
