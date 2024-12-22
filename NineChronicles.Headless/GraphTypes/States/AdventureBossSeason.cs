using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;

namespace NineChronicles.Headless.GraphTypes;

public class AdventureBossSeason : ObjectGraphType<AdventureBossSeasonStatus>
{
    public AdventureBossSeason()
    {
        Field<NonNullGraphType<IntGraphType>>(
            nameof(AdventureBossSeasonStatus.BossId),
            description: "BossId of AdvBoss",
            resolve: context => context.Source.BossId);
        Field<NonNullGraphType<LongGraphType>>(
            nameof(AdventureBossSeasonStatus.EndBlockIndex),
            description: "Arena score of avatar.",
            resolve: context => context.Source.EndBlockIndex);
        Field<NonNullGraphType<LongGraphType>>(
            nameof(AdventureBossSeasonStatus.NextStartBlockIndex),
            description: "Arena rank of avatar.",
            resolve: context => context.Source.NextStartBlockIndex);
        Field<NonNullGraphType<LongGraphType>>(
            nameof(AdventureBossSeasonStatus.StartBlockIndex),
            description: "Score for victory.",
            resolve: context => context.Source.StartBlockIndex);
        Field<NonNullGraphType<BooleanGraphType>>(
            nameof(AdventureBossSeasonStatus.Finished),
            description: "Score for defeat.",
            resolve: context => context.Source.Finished);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(AdventureBossSeasonStatus.Floor),
            description: "Cp of avatar.",
            resolve: context => context.Source.Floor);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(AdventureBossSeasonStatus.MaxFloor),
            description: "Portrait icon id.",
            resolve: context => context.Source.MaxFloor);
        Field<NonNullGraphType<StringGraphType>>(
            nameof(AdventureBossSeasonStatus.Name),
            description: "Level of avatar.",
            resolve: context => context.Source.Name);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(AdventureBossSeasonStatus.UsedApPotion),
            description: "Name of avatar.",
            resolve: context => context.Source.UsedApPotion);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(AdventureBossSeasonStatus.UsedGoldenDust),
            description: "Level of avatar.",
            resolve: context => context.Source.UsedGoldenDust);
        Field<NonNullGraphType<FloatGraphType>>(
            nameof(AdventureBossSeasonStatus.UsedNcg),
            description: "Level of avatar.",
            resolve: context => context.Source.UsedNcg);
        Field<NonNullGraphType<BigIntGraphType>>(
            nameof(AdventureBossSeasonStatus.TotalBounty),
            description: "Level of avatar.",
            resolve: context => context.Source.TotalBounty);
    }
}
