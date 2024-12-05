using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;
using Nekoyume.Model.State;

namespace NineChronicles.Headless.GraphTypes.States
{
    internal class ArenaStateType : ObjectGraphType<ArenaState>
    {
        public ArenaStateType()
        {
            Field<NonNullGraphType<IntGraphType>>(
                nameof(ArenaState.championshipId),
                resolve: context => context.Source.championshipId);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(ArenaState.championshipIndex),
                resolve: context => context.Source.championshipIndex);
            
        }
    }
}
