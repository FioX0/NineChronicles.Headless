using GraphQL.Types;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class ArenaRuneSlotStateType : ObjectGraphType<ArenaRuneSlotState>
    {
        public ArenaRuneSlotStateType()
        {
            Field<NonNullGraphType<IntGraphType>>(
                nameof(ArenaRuneSlotState.slotIndex),
                resolve: context => context.Source.slotIndex);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(ArenaRuneSlotState.runeId),
                resolve: context => context.Source.runeId);
            Field<NonNullGraphType<BooleanGraphType>>(
                nameof(ArenaRuneSlotState.runeStateExists),
                resolve: context => context.Source.runeStateExists);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(ArenaRuneSlotState.level),
                resolve: context => context.Source.level);
        }
    }
}
