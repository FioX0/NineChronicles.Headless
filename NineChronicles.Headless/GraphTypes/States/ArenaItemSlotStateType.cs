using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class ArenaItemSlotStateType : ObjectGraphType<ArenaItemSlotState>
    {
        public ArenaItemSlotStateType()
        {
            Field<NonNullGraphType<AddressType>>(
                nameof(ArenaItemSlotState.avatarAddress),
                resolve: context => context.Source.avatarAddress);
            Field<NonNullGraphType<AddressType>>(
                nameof(ArenaItemSlotState.itemSlotStateAddress),
                resolve: context => context.Source.itemSlotStateAddress);
            Field<NonNullGraphType<AddressType>>(
                nameof(ArenaItemSlotState.arenaAvatarStateAddress),
                resolve: context => context.Source.arenaAvatarStateAddress);
            Field<NonNullGraphType<StringGraphType>>(
                nameof(ArenaItemSlotState.battleType),
                resolve: context => context.Source.battleType);
            Field<NonNullGraphType<BooleanGraphType>>(
                nameof(ArenaItemSlotState.itemSlotStateExists),
                resolve: context => context.Source.itemSlotStateExists);
            Field<NonNullGraphType<BooleanGraphType>>(
                nameof(ArenaItemSlotState.arenaAvatarStateExists),
                resolve: context => context.Source.arenaAvatarStateExists);
            Field<NonNullGraphType<ListGraphType<GuidGraphType>>>(
                nameof(ArenaItemSlotState.itemSlotEquipments),
                resolve: context => context.Source.itemSlotEquipments);
            Field<NonNullGraphType<ListGraphType<GuidGraphType>>>(
                nameof(ArenaItemSlotState.itemSlotCostumes),
                resolve: context => context.Source.itemSlotCostumes);
            Field<NonNullGraphType<ListGraphType<GuidGraphType>>>(
                nameof(ArenaItemSlotState.arenaAvatarEquipments),
                resolve: context => context.Source.arenaAvatarEquipments);
            Field<NonNullGraphType<ListGraphType<GuidGraphType>>>(
                nameof(ArenaItemSlotState.arenaAvatarCostumes),
                resolve: context => context.Source.arenaAvatarCostumes);
        }
    }
}
