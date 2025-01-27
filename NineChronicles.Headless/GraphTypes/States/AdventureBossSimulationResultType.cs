using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;
using Nekoyume.Model.State;

namespace NineChronicles.Headless.GraphTypes.States
{
    internal class AdventureBossSimulationResultType : ObjectGraphType<AdventureBossSimulationResult>
    {
        public AdventureBossSimulationResultType()
        {
            Field<NonNullGraphType<IntGraphType>>(
                nameof(AdventureBossSimulationResult.floor),
                description: "Block Index",
                resolve: context => context.Source.floor);

            Field<NonNullGraphType<DecimalGraphType>>(
                nameof(AdventureBossSimulationResult.winPercentage),
                description: "Block Index",
                resolve: context => context.Source.winPercentage);
        }
    }
}
