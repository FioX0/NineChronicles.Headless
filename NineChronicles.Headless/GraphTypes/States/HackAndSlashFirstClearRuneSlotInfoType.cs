#nullable enable

using GraphQL.Types;
using Nekoyume.Action;

namespace NineChronicles.Headless.GraphTypes.States
{
    public sealed class HackAndSlashFirstClearRuneSlotInfoType : ObjectGraphType<RuneSlotInfo>
    {
        public HackAndSlashFirstClearRuneSlotInfoType()
        {
            Field<NonNullGraphType<IntGraphType>>("slotIndex", resolve: context => context.Source.SlotIndex);
            Field<NonNullGraphType<IntGraphType>>("runeId", resolve: context => context.Source.RuneId);
        }
    }
}
