using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;
using Nekoyume.Model.State;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class AdventureBossSimulationStateType : ObjectGraphType<AdventureBossSimulationState>
    {
        public AdventureBossSimulationStateType()
        {
            Field<NonNullGraphType<IntGraphType>>(
                nameof(AdventureBossSimulationState.blockIndex),
                description: "Block Index",
                resolve: context => context.Source.blockIndex);
            Field<NonNullGraphType<ListGraphType<AdventureBossSimulationResultType>>>(
                nameof(AdventureBossSimulationState.result),
                description: "Block Index",
                resolve: context => context.Source.result);
        }
    }
}
