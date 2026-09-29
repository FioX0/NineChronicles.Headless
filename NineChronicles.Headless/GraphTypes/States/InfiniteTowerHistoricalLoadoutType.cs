#nullable enable

using GraphQL.Types;
using HistoricalType = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadoutType;

namespace NineChronicles.Headless.GraphTypes.States
{
    public sealed class InfiniteTowerHistoricalLoadoutType : ObjectGraphType<InfiniteTowerHistoricalLoadout>
    {
        public InfiniteTowerHistoricalLoadoutType()
        {
            Field<NonNullGraphType<HistoricalType.HackAndSlashHistoricalAvatarType>>(
                "avatar",
                resolve: context => context.Source.Avatar);
            Field<NonNullGraphType<LongGraphType>>(
                "recordedInfiniteTowerCp",
                resolve: context => context.Source.RecordedInfiniteTowerCp);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HistoricalType.HackAndSlashHistoricalEquipmentType>>>>(
                "equipments",
                resolve: context => context.Source.Equipments);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HistoricalType.HackAndSlashHistoricalCostumeType>>>>(
                "costumes",
                resolve: context => context.Source.Costumes);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HistoricalType.HackAndSlashHistoricalFoodType>>>>(
                "foods",
                resolve: context => context.Source.Foods);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HistoricalType.HackAndSlashHistoricalRuneType>>>>(
                "runes",
                resolve: context => context.Source.Runes);
            Field<NonNullGraphType<HistoricalType.HackAndSlashHistoricalCollectionSummaryType>>(
                "collectionSummary",
                resolve: context => context.Source.CollectionSummary);
        }
    }
}
