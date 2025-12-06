using GraphQL.Types;

namespace NineChronicles.Headless.GraphTypes.States
{
    internal class InfiniteTowerSimulationResultType : ObjectGraphType<InfiniteTowerSimulationResult>
    {
        public InfiniteTowerSimulationResultType()
        {
            Field<NonNullGraphType<IntGraphType>>(
                nameof(InfiniteTowerSimulationResult.floor),
                description: "Infinite tower floor.",
                resolve: context => context.Source.floor);

            Field<NonNullGraphType<DecimalGraphType>>(
                nameof(InfiniteTowerSimulationResult.winPercentage),
                description: "Win percentage for this floor.",
                resolve: context => context.Source.winPercentage);

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>>(
                nameof(InfiniteTowerSimulationResult.conditionIds),
                description: "IDs of conditions (guaranteed + random) applied in this variation.",
                resolve: context => context.Source.conditionIds);

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>(
                nameof(InfiniteTowerSimulationResult.conditionDescriptions),
                description: "User-readable descriptions of conditions (guaranteed + random) applied in this variation.",
                resolve: context => context.Source.conditionDescriptions);
        }
    }
}


