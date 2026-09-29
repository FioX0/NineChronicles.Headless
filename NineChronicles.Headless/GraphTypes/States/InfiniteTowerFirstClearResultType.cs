#nullable enable

using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;
using NineChronicles.Headless.GraphTypes.States.Models.Item;

namespace NineChronicles.Headless.GraphTypes.States
{
    public sealed class InfiniteTowerFirstClearResultType : ObjectGraphType<InfiniteTowerFirstClearResult>
    {
        public InfiniteTowerFirstClearResultType()
        {
            Field<NonNullGraphType<BooleanGraphType>>("isFirstClear", resolve: context => context.Source.IsFirstClear);
            Field<NonNullGraphType<StringGraphType>>("txId", resolve: context => context.Source.TxId);
            Field<NonNullGraphType<LongGraphType>>("blockIndex", resolve: context => context.Source.BlockIndex);
            Field<NonNullGraphType<DateTimeOffsetGraphType>>("blockTimestamp", resolve: context => context.Source.BlockTimestamp);
            Field<NonNullGraphType<StringGraphType>>("blockHash", resolve: context => context.Source.BlockHash);
            Field<NonNullGraphType<StringGraphType>>("inputStateRoot", resolve: context => context.Source.InputStateRoot);
            Field<NonNullGraphType<AddressType>>("avatarAddress", resolve: context => context.Source.AvatarAddress);
            Field<NonNullGraphType<IntGraphType>>("infiniteTowerId", resolve: context => context.Source.InfiniteTowerId);
            Field<NonNullGraphType<IntGraphType>>("scheduleId", resolve: context => context.Source.ScheduleId);
            Field<NonNullGraphType<IntGraphType>>("floorId", resolve: context => context.Source.FloorId);
            Field<NonNullGraphType<BooleanGraphType>>("buyTicketIfNeeded", resolve: context => context.Source.BuyTicketIfNeeded);
            Field<NonNullGraphType<BooleanGraphType>>("useNcgForTicket", resolve: context => context.Source.UseNcgForTicket);
            Field<NonNullGraphType<IntGraphType>>("randomSeed", resolve: context => context.Source.RandomSeed);
            Field<NonNullGraphType<IntGraphType>>("rulesFingerprintVersion", resolve: context => context.Source.RulesFingerprintVersion);
            Field<StringGraphType>("configuredRulesFingerprint", resolve: context => context.Source.ConfiguredRulesFingerprint);
            Field<StringGraphType>("appliedRulesFingerprint", resolve: context => context.Source.AppliedRulesFingerprint);
            Field<NonNullGraphType<SeasonType>>("season", resolve: context => context.Source.Season);
            Field<RuleSetType>("rules", resolve: context => context.Source.Rules);
            Field<RuleSetType>(
                "effectiveRules",
                description: "Historical floor restrictions and exact conditions applied to this battle.",
                resolve: context => context.Source.Rules);
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
            Field<LongGraphType>("recordedInfiniteTowerCp", resolve: context => context.Source.RecordedInfiniteTowerCp);
            Field<ListGraphType<NonNullGraphType<EquipmentType>>>("equipments", resolve: context => context.Source.Equipments);
            Field<ListGraphType<NonNullGraphType<CostumeType>>>("costumes", resolve: context => context.Source.Costumes);
            Field<ListGraphType<NonNullGraphType<ConsumableType>>>("foods", resolve: context => context.Source.Foods);
            Field<ListGraphType<NonNullGraphType<RuneStateType>>>("runes", resolve: context => context.Source.Runes);
            Field<ListGraphType<NonNullGraphType<HackAndSlashFirstClearCollectionType>>>("collections", resolve: context => context.Source.Collections);
            Field<InfiniteTowerHistoricalLoadoutType>(
                "historicalLoadout",
                resolve: context => context.Source.HistoricalLoadout);
            Field<AvatarStateType>("avatar", resolve: context => context.Source.Avatar);
        }

        public sealed class SeasonType : ObjectGraphType<InfiniteTowerFirstClearRules.Season>
        {
            public SeasonType()
            {
                Name = "InfiniteTowerFirstClearSeasonType";
                Field<NonNullGraphType<IntGraphType>>("scheduleId", resolve: context => context.Source.ScheduleId);
                Field<NonNullGraphType<IntGraphType>>("infiniteTowerId", resolve: context => context.Source.InfiniteTowerId);
                Field<NonNullGraphType<LongGraphType>>("startBlockIndex", resolve: context => context.Source.StartBlockIndex);
                Field<NonNullGraphType<LongGraphType>>("endBlockIndex", resolve: context => context.Source.EndBlockIndex);
                Field<NonNullGraphType<IntGraphType>>("dailyFreeTickets", resolve: context => context.Source.DailyFreeTickets);
                Field<NonNullGraphType<IntGraphType>>("maxTickets", resolve: context => context.Source.MaxTickets);
                Field<NonNullGraphType<IntGraphType>>("resetIntervalBlocks", resolve: context => context.Source.ResetIntervalBlocks);
                Field<NonNullGraphType<IntGraphType>>("floorBegin", resolve: context => context.Source.FloorBegin);
                Field<NonNullGraphType<IntGraphType>>("floorEnd", resolve: context => context.Source.FloorEnd);
            }
        }

        public sealed class RuleSetType : ObjectGraphType<InfiniteTowerFirstClearRules.RuleSet>
        {
            public RuleSetType()
            {
                Name = "InfiniteTowerFirstClearRuleSetType";
                Field<NonNullGraphType<StringGraphType>>("selectionMode", resolve: context => context.Source.SelectionMode);
                Field<NonNullGraphType<IntGraphType>>("shatterStrikeMaxDamage", resolve: context => context.Source.ShatterStrikeMaxDamage);
                Field<NonNullGraphType<IntGraphType>>("turnLimit", resolve: context => context.Source.TurnLimit);
                Field<LongGraphType>("requiredCp", resolve: context => context.Source.RequiredCp);
                Field<LongGraphType>("maxCp", resolve: context => context.Source.MaxCp);
                Field<IntGraphType>("minItemGrade", resolve: context => context.Source.MinItemGrade);
                Field<IntGraphType>("maxItemGrade", resolve: context => context.Source.MaxItemGrade);
                Field<IntGraphType>("minItemLevel", resolve: context => context.Source.MinItemLevel);
                Field<IntGraphType>("maxItemLevel", resolve: context => context.Source.MaxItemLevel);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("forbiddenItemSubTypes", resolve: context => context.Source.ForbiddenItemSubTypes);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("forbiddenRuneTypes", resolve: context => context.Source.ForbiddenRuneTypes);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("requiredElementalTypes", resolve: context => context.Source.RequiredElementalTypes);
                Field<NonNullGraphType<IntGraphType>>("minRandomConditions", resolve: context => context.Source.MinRandomConditions);
                Field<NonNullGraphType<IntGraphType>>("maxRandomConditions", resolve: context => context.Source.MaxRandomConditions);
                Field<ConditionType>("guaranteedCondition", resolve: context => context.Source.GuaranteedCondition);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<ConditionCandidateType>>>>("randomConditionCandidates", resolve: context => context.Source.RandomConditionCandidates);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<ConditionType>>>>("appliedConditions", resolve: context => context.Source.AppliedConditions);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<StatModifierType>>>>("enemyInitialStatModifiers", resolve: context => context.Source.EnemyInitialStatModifiers);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<WaveType>>>>("waves", resolve: context => context.Source.Waves);
            }
        }

        public sealed class ConditionCandidateType : ObjectGraphType<InfiniteTowerFirstClearRules.ConditionCandidate>
        {
            public ConditionCandidateType()
            {
                Name = "InfiniteTowerFirstClearConditionCandidateType";
                Field<IntGraphType>("weight", resolve: context => context.Source.Weight);
                Field<NonNullGraphType<ConditionType>>("condition", resolve: context => context.Source.Condition);
            }
        }

        public sealed class ConditionType : ObjectGraphType<InfiniteTowerFirstClearRules.Condition>
        {
            public ConditionType()
            {
                Name = "InfiniteTowerFirstClearConditionType";
                Field<NonNullGraphType<IntGraphType>>("id", resolve: context => context.Source.Id);
                Field<NonNullGraphType<StringGraphType>>("statType", resolve: context => context.Source.StatType);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("targetTypes", resolve: context => context.Source.TargetTypes);
                Field<NonNullGraphType<StringGraphType>>("operation", resolve: context => context.Source.Operation);
                Field<NonNullGraphType<IntGraphType>>("value", resolve: context => context.Source.Value);
            }
        }

        public sealed class StatModifierType : ObjectGraphType<InfiniteTowerFirstClearRules.StatModifier>
        {
            public StatModifierType()
            {
                Name = "InfiniteTowerFirstClearEnemyStatModifierType";
                Field<NonNullGraphType<StringGraphType>>("statType", resolve: context => context.Source.StatType);
                Field<NonNullGraphType<StringGraphType>>("operation", resolve: context => context.Source.Operation);
                Field<NonNullGraphType<LongGraphType>>("value", resolve: context => context.Source.Value);
            }
        }

        public sealed class WaveType : ObjectGraphType<InfiniteTowerFirstClearRules.Wave>
        {
            public WaveType()
            {
                Name = "InfiniteTowerFirstClearWaveType";
                Field<NonNullGraphType<IntGraphType>>("number", resolve: context => context.Source.Number);
                Field<NonNullGraphType<BooleanGraphType>>("hasBoss", resolve: context => context.Source.HasBoss);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<MonsterType>>>>("monsters", resolve: context => context.Source.Monsters);
            }
        }

        public sealed class MonsterType : ObjectGraphType<InfiniteTowerFirstClearRules.Monster>
        {
            public MonsterType()
            {
                Name = "InfiniteTowerFirstClearMonsterType";
                Field<NonNullGraphType<IntGraphType>>("characterId", resolve: context => context.Source.CharacterId);
                Field<NonNullGraphType<IntGraphType>>("level", resolve: context => context.Source.Level);
                Field<NonNullGraphType<IntGraphType>>("count", resolve: context => context.Source.Count);
            }
        }
    }
}
