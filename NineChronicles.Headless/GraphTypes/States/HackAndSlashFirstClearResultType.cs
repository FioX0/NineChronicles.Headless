#nullable enable

using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;
using Nekoyume.Action;
using Nekoyume.Model.Stat;
using NineChronicles.Headless.GraphTypes.States.Models.Item;

namespace NineChronicles.Headless.GraphTypes.States
{
    public sealed class HackAndSlashFirstClearResultType : ObjectGraphType<HackAndSlashFirstClearResult>
    {
        public HackAndSlashFirstClearResultType()
        {
            Field<NonNullGraphType<BooleanGraphType>>("isFirstClear", resolve: context => context.Source.IsFirstClear);
            Field<NonNullGraphType<StringGraphType>>("txId", resolve: context => context.Source.TxId);
            Field<NonNullGraphType<LongGraphType>>("blockIndex", resolve: context => context.Source.BlockIndex);
            Field<NonNullGraphType<DateTimeOffsetGraphType>>("blockTimestamp", resolve: context => context.Source.BlockTimestamp);
            Field<NonNullGraphType<StringGraphType>>("blockHash", resolve: context => context.Source.BlockHash);
            Field<NonNullGraphType<IntGraphType>>("worldId", resolve: context => context.Source.WorldId);
            Field<NonNullGraphType<IntGraphType>>("stageId", resolve: context => context.Source.StageId);
            Field<IntGraphType>("stageBuffId", resolve: context => context.Source.StageBuffId);
            Field<NonNullGraphType<IntGraphType>>("totalPlayCount", resolve: context => context.Source.TotalPlayCount);
            Field<NonNullGraphType<AddressType>>("avatarAddress", resolve: context => context.Source.AvatarAddress);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<GuidGraphType>>>>("equipmentIds", resolve: context => context.Source.EquipmentIds);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<GuidGraphType>>>>("costumeIds", resolve: context => context.Source.CostumeIds);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<GuidGraphType>>>>("foodIds", resolve: context => context.Source.FoodIds);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<GuidGraphType>>>>("unresolvedCostumeIds", resolve: context => context.Source.UnresolvedCostumeIds);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<GuidGraphType>>>>("unresolvedFoodIds", resolve: context => context.Source.UnresolvedFoodIds);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashFirstClearRuneSlotInfoType>>>>("runeSlotInfos", resolve: context => context.Source.RuneSlotInfos);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>>("collectionIds", resolve: context => context.Source.CollectionIds);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashFirstClearStatModifierType>>>>("collectionModifiers", resolve: context => context.Source.CollectionModifiers);
            Field<NonNullGraphType<IntGraphType>>("equipmentCount", resolve: context => context.Source.EquipmentCount);
            Field<NonNullGraphType<IntGraphType>>("costumeCount", resolve: context => context.Source.CostumeCount);
            Field<NonNullGraphType<IntGraphType>>("foodCount", resolve: context => context.Source.FoodCount);
            Field<NonNullGraphType<IntGraphType>>("runeCount", resolve: context => context.Source.RuneCount);
            Field<NonNullGraphType<IntGraphType>>("collectionCount", resolve: context => context.Source.CollectionCount);
            Field<NonNullGraphType<IntGraphType>>("collectionModifierCount", resolve: context => context.Source.CollectionModifierCount);
            Field<ListGraphType<NonNullGraphType<EquipmentType>>>("equipments", resolve: context => context.Source.Equipments);
            Field<ListGraphType<NonNullGraphType<CostumeType>>>("costumes", resolve: context => context.Source.Costumes);
            Field<ListGraphType<NonNullGraphType<ConsumableType>>>("foods", resolve: context => context.Source.Foods);
            Field<ListGraphType<NonNullGraphType<RuneStateType>>>("runes", resolve: context => context.Source.Runes);
            Field<ListGraphType<NonNullGraphType<HackAndSlashFirstClearCollectionType>>>("collections", resolve: context => context.Source.Collections);
            Field<HackAndSlashHistoricalLoadoutType>(
                "historicalLoadout",
                resolve: context => context.Source.HistoricalLoadout);
            Field<AvatarStateType>("avatar", resolve: context => context.Source.Avatar);
        }
    }
}
