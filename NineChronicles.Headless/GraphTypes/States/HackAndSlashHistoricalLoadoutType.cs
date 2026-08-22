#nullable enable

using GraphQL.Types;
using HackAndSlashHistoricalAvatar = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalAvatar;
using HackAndSlashHistoricalCollectionSummary = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalCollectionSummary;
using HackAndSlashHistoricalCostume = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalCostume;
using HackAndSlashHistoricalCostumeItem = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalCostumeItem;
using HackAndSlashHistoricalDecimalStat = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalDecimalStat;
using HackAndSlashHistoricalEquipment = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalEquipment;
using HackAndSlashHistoricalEquipmentItem = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalEquipmentItem;
using HackAndSlashHistoricalFood = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalFood;
using HackAndSlashHistoricalFoodItem = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalFoodItem;
using HackAndSlashHistoricalModifier = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalModifier;
using HackAndSlashHistoricalRune = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalRune;
using HackAndSlashHistoricalRuneOption = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalRuneOption;
using HackAndSlashHistoricalRuneSkill = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalRuneSkill;
using HackAndSlashHistoricalRuneState = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalRuneState;
using HackAndSlashHistoricalRuneStat = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalRuneStat;
using HackAndSlashHistoricalSkill = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalSkill;
using HackAndSlashHistoricalStat = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalStat;
using HackAndSlashHistoricalStatsMap = NineChronicles.Headless.GraphTypes.States.HackAndSlashHistoricalLoadout.HackAndSlashHistoricalStatsMap;

namespace NineChronicles.Headless.GraphTypes.States
{
    public sealed class HackAndSlashHistoricalLoadoutType : ObjectGraphType<HackAndSlashHistoricalLoadout>
    {
        public HackAndSlashHistoricalLoadoutType()
        {
            Field<NonNullGraphType<HackAndSlashHistoricalAvatarType>>("avatar", resolve: context => context.Source.Avatar);
            Field<NonNullGraphType<LongGraphType>>("recordedAdventureCp", resolve: context => context.Source.RecordedAdventureCp);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashHistoricalEquipmentType>>>>("equipments", resolve: context => context.Source.Equipments);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashHistoricalCostumeType>>>>("costumes", resolve: context => context.Source.Costumes);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashHistoricalFoodType>>>>("foods", resolve: context => context.Source.Foods);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashHistoricalRuneType>>>>("runes", resolve: context => context.Source.Runes);
            Field<NonNullGraphType<HackAndSlashHistoricalCollectionSummaryType>>("collectionSummary", resolve: context => context.Source.CollectionSummary);
        }

        public sealed class HackAndSlashHistoricalAvatarType : ObjectGraphType<HackAndSlashHistoricalAvatar>
        {
            public HackAndSlashHistoricalAvatarType()
            {
                Field<NonNullGraphType<StringGraphType>>("name", resolve: context => context.Source.Name);
                Field<NonNullGraphType<IntGraphType>>("level", resolve: context => context.Source.Level);
                Field<NonNullGraphType<LongGraphType>>("exp", resolve: context => context.Source.Exp);
                Field<NonNullGraphType<IntGraphType>>("actionPoint", resolve: context => context.Source.ActionPoint);
            }
        }

        public sealed class HackAndSlashHistoricalEquipmentType : ObjectGraphType<HackAndSlashHistoricalEquipment>
        {
            public HackAndSlashHistoricalEquipmentType()
            {
                Field<NonNullGraphType<StringGraphType>>("name", resolve: context => context.Source.Name);
                Field<NonNullGraphType<HackAndSlashHistoricalEquipmentItemType>>("item", resolve: context => context.Source.Item);
            }
        }

        public sealed class HackAndSlashHistoricalEquipmentItemType : ObjectGraphType<HackAndSlashHistoricalEquipmentItem>
        {
            public HackAndSlashHistoricalEquipmentItemType()
            {
                Field<NonNullGraphType<StringGraphType>>("itemId", resolve: context => context.Source.ItemId);
                Field<NonNullGraphType<IntGraphType>>("grade", resolve: context => context.Source.Grade);
                Field<NonNullGraphType<IntGraphType>>("id", resolve: context => context.Source.Id);
                Field<NonNullGraphType<StringGraphType>>("itemSubType", resolve: context => context.Source.ItemSubType);
                Field<NonNullGraphType<StringGraphType>>("elementalType", resolve: context => context.Source.ElementalType);
                Field<NonNullGraphType<IntGraphType>>("setId", resolve: context => context.Source.SetId);
                Field<NonNullGraphType<HackAndSlashHistoricalDecimalStatType>>("stat", resolve: context => context.Source.Stat);
                Field<NonNullGraphType<IntGraphType>>("level", resolve: context => context.Source.Level);
                Field<NonNullGraphType<LongGraphType>>("exp", resolve: context => context.Source.Exp);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashHistoricalSkillType>>>>("skills", resolve: context => context.Source.Skills);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashHistoricalSkillType>>>>("buffSkills", resolve: context => context.Source.BuffSkills);
                Field<NonNullGraphType<HackAndSlashHistoricalStatsMapType>>("statsMap", resolve: context => context.Source.StatsMap);
            }
        }

        public sealed class HackAndSlashHistoricalCostumeType : ObjectGraphType<HackAndSlashHistoricalCostume>
        {
            public HackAndSlashHistoricalCostumeType()
            {
                Field<NonNullGraphType<StringGraphType>>("name", resolve: context => context.Source.Name);
                Field<NonNullGraphType<HackAndSlashHistoricalCostumeItemType>>("item", resolve: context => context.Source.Item);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashHistoricalStatType>>>>("statModifiers", resolve: context => context.Source.StatModifiers);
            }
        }

        public sealed class HackAndSlashHistoricalCostumeItemType : ObjectGraphType<HackAndSlashHistoricalCostumeItem>
        {
            public HackAndSlashHistoricalCostumeItemType()
            {
                Field<NonNullGraphType<StringGraphType>>("itemId", resolve: context => context.Source.ItemId);
                Field<NonNullGraphType<IntGraphType>>("grade", resolve: context => context.Source.Grade);
                Field<NonNullGraphType<IntGraphType>>("id", resolve: context => context.Source.Id);
                Field<NonNullGraphType<StringGraphType>>("itemSubType", resolve: context => context.Source.ItemSubType);
                Field<NonNullGraphType<StringGraphType>>("elementalType", resolve: context => context.Source.ElementalType);
            }
        }

        public sealed class HackAndSlashHistoricalFoodType : ObjectGraphType<HackAndSlashHistoricalFood>
        {
            public HackAndSlashHistoricalFoodType()
            {
                Field<NonNullGraphType<StringGraphType>>("name", resolve: context => context.Source.Name);
                Field<NonNullGraphType<HackAndSlashHistoricalFoodItemType>>("item", resolve: context => context.Source.Item);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashHistoricalStatType>>>>("staticStats", resolve: context => context.Source.StaticStats);
            }
        }

        public sealed class HackAndSlashHistoricalFoodItemType : ObjectGraphType<HackAndSlashHistoricalFoodItem>
        {
            public HackAndSlashHistoricalFoodItemType()
            {
                Field<NonNullGraphType<StringGraphType>>("itemId", resolve: context => context.Source.ItemId);
                Field<NonNullGraphType<IntGraphType>>("grade", resolve: context => context.Source.Grade);
                Field<NonNullGraphType<IntGraphType>>("id", resolve: context => context.Source.Id);
                Field<NonNullGraphType<StringGraphType>>("itemSubType", resolve: context => context.Source.ItemSubType);
                Field<NonNullGraphType<StringGraphType>>("elementalType", resolve: context => context.Source.ElementalType);
                Field<NonNullGraphType<StringGraphType>>("mainStat", resolve: context => context.Source.MainStat);
            }
        }

        public sealed class HackAndSlashHistoricalRuneType : ObjectGraphType<HackAndSlashHistoricalRune>
        {
            public HackAndSlashHistoricalRuneType()
            {
                Field<NonNullGraphType<IntGraphType>>("slotIndex", resolve: context => context.Source.SlotIndex);
                Field<NonNullGraphType<StringGraphType>>("name", resolve: context => context.Source.Name);
                Field<NonNullGraphType<HackAndSlashHistoricalRuneStateType>>("rune", resolve: context => context.Source.Rune);
                Field<NonNullGraphType<HackAndSlashHistoricalRuneOptionType>>("option", resolve: context => context.Source.Option);
            }
        }

        public sealed class HackAndSlashHistoricalRuneStateType : ObjectGraphType<HackAndSlashHistoricalRuneState>
        {
            public HackAndSlashHistoricalRuneStateType()
            {
                Field<NonNullGraphType<IntGraphType>>("runeId", resolve: context => context.Source.RuneId);
                Field<NonNullGraphType<IntGraphType>>("level", resolve: context => context.Source.Level);
            }
        }

        public sealed class HackAndSlashHistoricalRuneOptionType : ObjectGraphType<HackAndSlashHistoricalRuneOption>
        {
            public HackAndSlashHistoricalRuneOptionType()
            {
                Field<NonNullGraphType<LongGraphType>>("cp", resolve: context => context.Source.Cp);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashHistoricalRuneStatType>>>>("stats", resolve: context => context.Source.Stats);
                Field<HackAndSlashHistoricalRuneSkillType>("skill", resolve: context => context.Source.Skill);
            }
        }

        public sealed class HackAndSlashHistoricalRuneStatType : ObjectGraphType<HackAndSlashHistoricalRuneStat>
        {
            public HackAndSlashHistoricalRuneStatType()
            {
                Field<NonNullGraphType<StringGraphType>>("statType", resolve: context => context.Source.StatType);
                Field<NonNullGraphType<StringGraphType>>("operation", resolve: context => context.Source.Operation);
                Field<NonNullGraphType<DecimalGraphType>>("rawValue", resolve: context => context.Source.RawValue);
                Field<NonNullGraphType<LongGraphType>>("effectiveValue", resolve: context => context.Source.EffectiveValue);
            }
        }

        public sealed class HackAndSlashHistoricalRuneSkillType : ObjectGraphType<HackAndSlashHistoricalRuneSkill>
        {
            public HackAndSlashHistoricalRuneSkillType()
            {
                Field<NonNullGraphType<IntGraphType>>("skillId", resolve: context => context.Source.SkillId);
                Field<NonNullGraphType<StringGraphType>>("name", resolve: context => context.Source.Name);
                Field<NonNullGraphType<IntGraphType>>("cooldown", resolve: context => context.Source.Cooldown);
                Field<NonNullGraphType<IntGraphType>>("chance", resolve: context => context.Source.Chance);
                Field<NonNullGraphType<DecimalGraphType>>("value", resolve: context => context.Source.Value);
                Field<NonNullGraphType<StringGraphType>>("valueOperation", resolve: context => context.Source.ValueOperation);
                Field<NonNullGraphType<StringGraphType>>("statType", resolve: context => context.Source.StatType);
                Field<NonNullGraphType<StringGraphType>>("statReferenceType", resolve: context => context.Source.StatReferenceType);
                Field<NonNullGraphType<IntGraphType>>("buffDuration", resolve: context => context.Source.BuffDuration);
            }
        }

        public sealed class HackAndSlashHistoricalStatType : ObjectGraphType<HackAndSlashHistoricalStat>
        {
            public HackAndSlashHistoricalStatType()
            {
                Field<NonNullGraphType<StringGraphType>>("statType", resolve: context => context.Source.StatType);
                Field<NonNullGraphType<DecimalGraphType>>("value", resolve: context => context.Source.Value);
            }
        }

        public sealed class HackAndSlashHistoricalDecimalStatType : ObjectGraphType<HackAndSlashHistoricalDecimalStat>
        {
            public HackAndSlashHistoricalDecimalStatType()
            {
                Field<NonNullGraphType<StringGraphType>>("statType", resolve: context => context.Source.StatType);
                Field<NonNullGraphType<DecimalGraphType>>("baseValue", resolve: context => context.Source.BaseValue);
                Field<NonNullGraphType<DecimalGraphType>>("additionalValue", resolve: context => context.Source.AdditionalValue);
                Field<NonNullGraphType<DecimalGraphType>>("totalValue", resolve: context => context.Source.TotalValue);
            }
        }

        public sealed class HackAndSlashHistoricalStatsMapType : ObjectGraphType<HackAndSlashHistoricalStatsMap>
        {
            public HackAndSlashHistoricalStatsMapType()
            {
                Field<NonNullGraphType<LongGraphType>>("HP", resolve: context => context.Source.HP);
                Field<NonNullGraphType<LongGraphType>>("ATK", resolve: context => context.Source.ATK);
                Field<NonNullGraphType<LongGraphType>>("DEF", resolve: context => context.Source.DEF);
                Field<NonNullGraphType<LongGraphType>>("CRI", resolve: context => context.Source.CRI);
                Field<NonNullGraphType<LongGraphType>>("HIT", resolve: context => context.Source.HIT);
                Field<NonNullGraphType<LongGraphType>>("SPD", resolve: context => context.Source.SPD);
            }
        }

        public sealed class HackAndSlashHistoricalSkillType : ObjectGraphType<HackAndSlashHistoricalSkill>
        {
            public HackAndSlashHistoricalSkillType()
            {
                Field<NonNullGraphType<IntGraphType>>("id", resolve: context => context.Source.Id);
                Field<NonNullGraphType<StringGraphType>>("name", resolve: context => context.Source.Name);
                Field<NonNullGraphType<StringGraphType>>("elementalType", resolve: context => context.Source.ElementalType);
                Field<NonNullGraphType<LongGraphType>>("power", resolve: context => context.Source.Power);
                Field<NonNullGraphType<IntGraphType>>("chance", resolve: context => context.Source.Chance);
                Field<NonNullGraphType<IntGraphType>>("statPowerRatio", resolve: context => context.Source.StatPowerRatio);
                Field<NonNullGraphType<StringGraphType>>("referencedStatType", resolve: context => context.Source.ReferencedStatType);
            }
        }

        public sealed class HackAndSlashHistoricalCollectionSummaryType : ObjectGraphType<HackAndSlashHistoricalCollectionSummary>
        {
            public HackAndSlashHistoricalCollectionSummaryType()
            {
                Field<NonNullGraphType<IntGraphType>>("activeCount", resolve: context => context.Source.ActiveCount);
                Field<NonNullGraphType<IntGraphType>>("rawModifierCount", resolve: context => context.Source.RawModifierCount);
                Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashHistoricalModifierType>>>>("modifierTotals", resolve: context => context.Source.ModifierTotals);
            }
        }

        public sealed class HackAndSlashHistoricalModifierType : ObjectGraphType<HackAndSlashHistoricalModifier>
        {
            public HackAndSlashHistoricalModifierType()
            {
                Field<NonNullGraphType<StringGraphType>>("statType", resolve: context => context.Source.StatType);
                Field<NonNullGraphType<StringGraphType>>("operation", resolve: context => context.Source.Operation);
                Field<NonNullGraphType<LongGraphType>>("value", resolve: context => context.Source.Value);
            }
        }
    }
}
