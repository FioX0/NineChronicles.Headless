using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class ArenaPrepAvatarStateType : ObjectGraphType<ArenaPrepAvatarState>
    {
        public ArenaPrepAvatarStateType()
        {
            Field<NonNullGraphType<AddressType>>(
                nameof(ArenaPrepAvatarState.avatarAddress),
                resolve: context => context.Source.avatarAddress);
            Field<NonNullGraphType<StringGraphType>>(
                nameof(ArenaPrepAvatarState.nameWithHash),
                resolve: context => context.Source.nameWithHash);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(ArenaPrepAvatarState.level),
                resolve: context => context.Source.level);
            Field<NonNullGraphType<BooleanGraphType>>(
                nameof(ArenaPrepAvatarState.itemSlotStateExists),
                resolve: context => context.Source.itemSlotStateExists);
            Field<NonNullGraphType<BooleanGraphType>>(
                nameof(ArenaPrepAvatarState.runeSlotStateExists),
                resolve: context => context.Source.runeSlotStateExists);
            Field<NonNullGraphType<ListGraphType<GuidGraphType>>>(
                nameof(ArenaPrepAvatarState.equipments),
                resolve: context => context.Source.equipments);
            Field<NonNullGraphType<ListGraphType<GuidGraphType>>>(
                nameof(ArenaPrepAvatarState.costumes),
                resolve: context => context.Source.costumes);
            Field<NonNullGraphType<ListGraphType<ArenaRuneSlotStateType>>>(
                nameof(ArenaPrepAvatarState.runes),
                resolve: context => context.Source.runes);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(ArenaPrepAvatarState.allRuneStateCount),
                resolve: context => context.Source.allRuneStateCount);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(ArenaPrepAvatarState.collectionModifierCount),
                resolve: context => context.Source.collectionModifierCount);
            Field<NonNullGraphType<ListGraphType<StringGraphType>>>(
                nameof(ArenaPrepAvatarState.collectionModifiers),
                resolve: context => context.Source.collectionModifiers);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(ArenaPrepAvatarState.cp),
                resolve: context => context.Source.cp);
        }
    }
}
