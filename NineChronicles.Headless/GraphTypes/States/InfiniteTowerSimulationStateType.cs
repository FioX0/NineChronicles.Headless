using GraphQL.Types;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class InfiniteTowerSimulationStateType : ObjectGraphType<InfiniteTowerSimulationState>
    {
        public InfiniteTowerSimulationStateType()
        {
            Field<NonNullGraphType<IntGraphType>>(
                nameof(InfiniteTowerSimulationState.blockIndex),
                description: "Block index at which the simulation is run.",
                resolve: context => context.Source.blockIndex);

            Field<NonNullGraphType<ListGraphType<InfiniteTowerSimulationResultType>>>(
                nameof(InfiniteTowerSimulationState.result),
                description: "Simulation results per floor.",
                resolve: context => context.Source.result);
        }
    }
}


