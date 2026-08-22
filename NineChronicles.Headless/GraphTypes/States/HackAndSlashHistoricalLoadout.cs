#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bencodex.Types;
using CsvHelper;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Model.Stat;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;

namespace NineChronicles.Headless.GraphTypes.States
{
    /// <summary>
    /// Compact, transaction-exact display projection for a historical HackAndSlash first clear.
    /// Every name and static value comes from the pre-execution world state, not current tables.
    /// </summary>
    public sealed class HackAndSlashHistoricalLoadout
    {
        public HackAndSlashHistoricalAvatar Avatar { get; init; } = null!;
        public long RecordedAdventureCp { get; init; }
        public IReadOnlyList<HackAndSlashHistoricalEquipment> Equipments { get; init; } = Array.Empty<HackAndSlashHistoricalEquipment>();
        public IReadOnlyList<HackAndSlashHistoricalCostume> Costumes { get; init; } = Array.Empty<HackAndSlashHistoricalCostume>();
        public IReadOnlyList<HackAndSlashHistoricalFood> Foods { get; init; } = Array.Empty<HackAndSlashHistoricalFood>();
        public IReadOnlyList<HackAndSlashHistoricalRune> Runes { get; init; } = Array.Empty<HackAndSlashHistoricalRune>();
        public HackAndSlashHistoricalCollectionSummary CollectionSummary { get; init; } = null!;

        public sealed class HackAndSlashHistoricalAvatar
        {
            public string Name { get; init; } = string.Empty;
            public int Level { get; init; }
            public long Exp { get; init; }
            public int ActionPoint { get; init; }
        }

        /// <summary>Selected historical equipment, reduced to display fields.</summary>
        public sealed class HackAndSlashHistoricalEquipment
        {
            public HackAndSlashHistoricalEquipmentItem Item { get; init; } = null!;
            public string Name { get; init; } = string.Empty;
        }

        public sealed class HackAndSlashHistoricalEquipmentItem
        {
            public string ItemId { get; init; } = string.Empty;
            public int Grade { get; init; }
            public int Id { get; init; }
            public string ItemSubType { get; init; } = string.Empty;
            public string ElementalType { get; init; } = string.Empty;
            public int SetId { get; init; }
            public HackAndSlashHistoricalDecimalStat Stat { get; init; } = null!;
            public int Level { get; init; }
            public long Exp { get; init; }
            public IReadOnlyList<HackAndSlashHistoricalSkill> Skills { get; init; } = Array.Empty<HackAndSlashHistoricalSkill>();
            public IReadOnlyList<HackAndSlashHistoricalSkill> BuffSkills { get; init; } = Array.Empty<HackAndSlashHistoricalSkill>();
            public HackAndSlashHistoricalStatsMap StatsMap { get; init; } = null!;
        }

        public sealed class HackAndSlashHistoricalCostume
        {
            public HackAndSlashHistoricalCostumeItem Item { get; init; } = null!;
            public string Name { get; init; } = string.Empty;
            public IReadOnlyList<HackAndSlashHistoricalStat> StatModifiers { get; init; } = Array.Empty<HackAndSlashHistoricalStat>();
        }

        public sealed class HackAndSlashHistoricalCostumeItem
        {
            public string ItemId { get; init; } = string.Empty;
            public int Grade { get; init; }
            public int Id { get; init; }
            public string ItemSubType { get; init; } = string.Empty;
            public string ElementalType { get; init; } = string.Empty;
        }

        public sealed class HackAndSlashHistoricalFood
        {
            public HackAndSlashHistoricalFoodItem Item { get; init; } = null!;
            public string Name { get; init; } = string.Empty;
            // From the historical ConsumableItemSheet, not mutable item inventory state.
            public IReadOnlyList<HackAndSlashHistoricalStat> StaticStats { get; init; } = Array.Empty<HackAndSlashHistoricalStat>();
        }

        public sealed class HackAndSlashHistoricalFoodItem
        {
            public string ItemId { get; init; } = string.Empty;
            public int Grade { get; init; }
            public int Id { get; init; }
            public string ItemSubType { get; init; } = string.Empty;
            public string ElementalType { get; init; } = string.Empty;
            public string MainStat { get; init; } = string.Empty;
        }

        public sealed class HackAndSlashHistoricalRune
        {
            public int SlotIndex { get; init; }
            public HackAndSlashHistoricalRuneState Rune { get; init; } = null!;
            public string Name { get; init; } = string.Empty;
            public HackAndSlashHistoricalRuneOption Option { get; init; } = null!;
        }

        public sealed class HackAndSlashHistoricalRuneState
        {
            public int RuneId { get; init; }
            public int Level { get; init; }
        }

        public sealed class HackAndSlashHistoricalRuneOption
        {
            public long Cp { get; init; }
            public IReadOnlyList<HackAndSlashHistoricalRuneStat> Stats { get; init; } = Array.Empty<HackAndSlashHistoricalRuneStat>();
            public HackAndSlashHistoricalRuneSkill? Skill { get; init; }
        }

        public sealed class HackAndSlashHistoricalRuneStat
        {
            public string StatType { get; init; } = string.Empty;
            public string Operation { get; init; } = string.Empty;
            public decimal RawValue { get; init; }
            public long EffectiveValue { get; init; }
        }

        public sealed class HackAndSlashHistoricalRuneSkill
        {
            public int SkillId { get; init; }
            // Historical SkillSheet.csv _name from the pre-execution world state.
            public string Name { get; init; } = string.Empty;
            public int Cooldown { get; init; }
            public int Chance { get; init; }
            public decimal Value { get; init; }
            public string ValueOperation { get; init; } = string.Empty;
            public string StatType { get; init; } = string.Empty;
            public string StatReferenceType { get; init; } = string.Empty;
            public int BuffDuration { get; init; }
        }

        public sealed class HackAndSlashHistoricalStat
        {
            public string StatType { get; init; } = string.Empty;
            public decimal Value { get; init; }
        }

        public sealed class HackAndSlashHistoricalDecimalStat
        {
            public string StatType { get; init; } = string.Empty;
            public decimal BaseValue { get; init; }
            public decimal AdditionalValue { get; init; }
            public decimal TotalValue { get; init; }
        }

        public sealed class HackAndSlashHistoricalStatsMap
        {
            public long HP { get; init; }
            public long ATK { get; init; }
            public long DEF { get; init; }
            public long CRI { get; init; }
            public long HIT { get; init; }
            public long SPD { get; init; }
        }

        public sealed class HackAndSlashHistoricalSkill
        {
            public int Id { get; init; }
            // Historical SkillSheet.csv _name from the pre-execution world state.
            public string Name { get; init; } = string.Empty;
            public string ElementalType { get; init; } = string.Empty;
            public long Power { get; init; }
            public int Chance { get; init; }
            public int StatPowerRatio { get; init; }
            public string ReferencedStatType { get; init; } = string.Empty;
        }

        public sealed class HackAndSlashHistoricalCollectionSummary
        {
            public int ActiveCount { get; init; }
            public int RawModifierCount { get; init; }
            public IReadOnlyList<HackAndSlashHistoricalModifier> ModifierTotals { get; init; } = Array.Empty<HackAndSlashHistoricalModifier>();
        }

        public sealed class HackAndSlashHistoricalModifier
        {
            public string StatType { get; init; } = string.Empty;
            public string Operation { get; init; } = string.Empty;
            public long Value { get; init; }
        }

        internal static class Builder
        {
            // Historical queries can span arbitrary block heights. Bound this process-wide cache
            // so public GraphQL traffic cannot retain a dictionary for every historical sheet.
            private const int MaxNameCacheEntries = 256;
            private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<int, string>> NameCache = new();

            internal static HackAndSlashHistoricalLoadout Build(
                IWorldState inputWorldState,
                IWorldState outputWorldState,
                AvatarState inputAvatar,
                Address avatarAddress,
                IReadOnlyList<Equipment> equipments,
                IReadOnlyList<Costume> costumes,
                IReadOnlyList<Consumable> foods,
                IReadOnlyList<RuneState> runes,
                IReadOnlyList<RuneSlotInfo> runeSlots,
                AllRuneState allRuneState,
                IReadOnlyList<int> collectionIds,
                IReadOnlyList<StatModifier> collectionModifiers)
            {
                var equipmentNames = equipments.Count > 0
                    ? GetNames<EquipmentItemSheet>(inputWorldState)
                    : null;
                var skillNames = (equipments.Any(equipment =>
                        equipment.Skills.Count > 0 || equipment.BuffSkills.Count > 0) ||
                    runes.Count > 0)
                    ? GetNames<SkillSheet>(inputWorldState)
                    : null;
                var costumeNames = costumes.Count > 0
                    ? GetNames<CostumeItemSheet>(inputWorldState)
                    : null;
                var foodNames = foods.Count > 0
                    ? GetNames<ConsumableItemSheet>(inputWorldState)
                    : null;
                var runeNames = runes.Count > 0
                    ? GetNames<RuneListSheet>(inputWorldState)
                    : null;
                var consumableItemSheet = foods.Count > 0
                    ? inputWorldState.GetSheet<ConsumableItemSheet>()
                    : null;
                var costumeStatSheet = costumes.Count > 0
                    ? inputWorldState.GetSheet<CostumeStatSheet>()
                    : null;
                var runeOptionSheet = runes.Count > 0
                    ? inputWorldState.GetSheet<RuneOptionSheet>()
                    : null;
                var runeListSheet = runes.Count > 0
                    ? inputWorldState.GetSheet<RuneListSheet>()
                    : null;
                var runeLevelBonusSheet = runes.Count > 0
                    ? inputWorldState.GetSheet<RuneLevelBonusSheet>()
                    : null;
                var runeLevelBonus = runeListSheet is not null && runeLevelBonusSheet is not null
                    ? RuneHelper.CalculateRuneLevelBonus(allRuneState, runeListSheet, runeLevelBonusSheet)
                    : 0;

                return new HackAndSlashHistoricalLoadout
                {
                    Avatar = new HackAndSlashHistoricalAvatar
                    {
                        Name = inputAvatar.name,
                        Level = inputAvatar.level,
                        Exp = inputAvatar.exp,
                        ActionPoint = inputAvatar.actionPoint,
                    },
                    RecordedAdventureCp = GetRecordedAdventureCp(outputWorldState, avatarAddress),
                    Equipments = equipments.Select(equipment => new HackAndSlashHistoricalEquipment
                    {
                        Item = ToEquipmentItem(equipment, skillNames),
                        Name = RequireName<EquipmentItemSheet>(equipmentNames!, equipment.Id),
                    }).ToArray(),
                    Costumes = costumes.Select(costume => new HackAndSlashHistoricalCostume
                    {
                        Item = ToCostumeItem(costume),
                        Name = RequireName<CostumeItemSheet>(costumeNames!, costume.Id),
                        StatModifiers = GetCostumeStats(costume, costumeStatSheet),
                    }).ToArray(),
                    Foods = foods.Select(food => new HackAndSlashHistoricalFood
                    {
                        Item = ToFoodItem(food),
                        Name = RequireName<ConsumableItemSheet>(foodNames!, food.Id),
                        StaticStats = GetFoodStats(food, consumableItemSheet),
                    }).ToArray(),
                    Runes = GetRunes(
                        runes,
                        runeSlots,
                        runeNames,
                        runeOptionSheet,
                        runeLevelBonus,
                        skillNames),
                    CollectionSummary = new HackAndSlashHistoricalCollectionSummary
                    {
                        ActiveCount = collectionIds.Count,
                        RawModifierCount = collectionModifiers.Count,
                        ModifierTotals = AggregateModifiers(collectionModifiers),
                    },
                };
            }

            private static HackAndSlashHistoricalEquipmentItem ToEquipmentItem(
                Equipment equipment,
                IReadOnlyDictionary<int, string>? skillNames) => new()
            {
                ItemId = equipment.ItemId.ToString(),
                Grade = equipment.Grade,
                Id = equipment.Id,
                ItemSubType = equipment.ItemSubType.ToString(),
                ElementalType = equipment.ElementalType.ToString(),
                SetId = equipment.SetId,
                Stat = ToDecimalStat(equipment.Stat),
                Level = equipment.level,
                Exp = equipment.Exp,
                Skills = equipment.Skills.Select(skill => ToSkill(skill, skillNames!)).ToArray(),
                BuffSkills = equipment.BuffSkills.Select(skill => ToSkill(skill, skillNames!)).ToArray(),
                StatsMap = ToStatsMap(equipment.StatsMap),
            };

            private static HackAndSlashHistoricalCostumeItem ToCostumeItem(Costume costume) => new()
            {
                ItemId = costume.ItemId.ToString(),
                Grade = costume.Grade,
                Id = costume.Id,
                ItemSubType = costume.ItemSubType.ToString(),
                ElementalType = costume.ElementalType.ToString(),
            };

            private static HackAndSlashHistoricalFoodItem ToFoodItem(Consumable food) => new()
            {
                ItemId = food.ItemId.ToString(),
                Grade = food.Grade,
                Id = food.Id,
                ItemSubType = food.ItemSubType.ToString(),
                ElementalType = food.ElementalType.ToString(),
                MainStat = food.MainStat.ToString(),
            };

            private static HackAndSlashHistoricalDecimalStat ToDecimalStat(DecimalStat stat) => new()
            {
                StatType = stat.StatType.ToString(),
                BaseValue = stat.BaseValue,
                AdditionalValue = stat.AdditionalValue,
                TotalValue = stat.TotalValue,
            };

            private static HackAndSlashHistoricalStatsMap ToStatsMap(StatsMap statsMap) => new()
            {
                HP = statsMap.HP,
                ATK = statsMap.ATK,
                DEF = statsMap.DEF,
                CRI = statsMap.CRI,
                HIT = statsMap.HIT,
                SPD = statsMap.SPD,
            };

            private static HackAndSlashHistoricalSkill ToSkill(
                Nekoyume.Model.Skill.Skill skill,
                IReadOnlyDictionary<int, string> skillNames) => new()
            {
                Id = skill.SkillRow.Id,
                Name = RequireName<SkillSheet>(skillNames, skill.SkillRow.Id),
                ElementalType = skill.SkillRow.ElementalType.ToString(),
                Power = skill.Power,
                Chance = skill.Chance,
                StatPowerRatio = skill.StatPowerRatio,
                ReferencedStatType = skill.ReferencedStatType.ToString(),
            };

            private static IReadOnlyList<HackAndSlashHistoricalRune> GetRunes(
                IReadOnlyList<RuneState> runes,
                IReadOnlyList<RuneSlotInfo> runeSlots,
                IReadOnlyDictionary<int, string>? names,
                RuneOptionSheet? optionSheet,
                int runeLevelBonus,
                IReadOnlyDictionary<int, string>? skillNames)
            {
                var results = new List<HackAndSlashHistoricalRune>(runes.Count);
                for (var index = 0; index < runes.Count; index++)
                {
                    var rune = runes[index];
                    var slotIndex = index < runeSlots.Count ? runeSlots[index].SlotIndex : index;
                    if (optionSheet is null || !optionSheet.TryGetOptionInfo(rune.RuneId, rune.Level, out var optionInfo))
                    {
                        throw new InvalidOperationException(
                            $"Historical RuneOptionSheet has no option for rune {rune.RuneId} level {rune.Level}.");
                    }

                    results.Add(new HackAndSlashHistoricalRune
                    {
                        SlotIndex = slotIndex,
                        Rune = new HackAndSlashHistoricalRuneState
                        {
                            RuneId = rune.RuneId,
                            Level = rune.Level,
                        },
                        Name = RequireName<RuneListSheet>(names!, rune.RuneId),
                        Option = ToRuneOption(optionInfo, runeLevelBonus, skillNames),
                    });
                }

                return results;
            }

            private static HackAndSlashHistoricalRuneOption ToRuneOption(
                RuneOptionSheet.Row.RuneOptionInfo optionInfo,
                int runeLevelBonus,
                IReadOnlyDictionary<int, string>? skillNames)
            {
                var stats = optionInfo.Stats.Select(stat => new HackAndSlashHistoricalRuneStat
                {
                    StatType = stat.stat.StatType.ToString(),
                    Operation = stat.operationType.ToString(),
                    RawValue = stat.stat.BaseValue,
                    EffectiveValue = NumberConversionHelper.SafeDecimalToInt64(
                        stat.stat.BaseValue * (100000 + runeLevelBonus) / 100000m),
                }).ToArray();
                var skill = optionInfo.SkillId == default
                    ? null
                    : new HackAndSlashHistoricalRuneSkill
                    {
                        SkillId = optionInfo.SkillId,
                        Name = RequireName<SkillSheet>(skillNames!, optionInfo.SkillId),
                        Cooldown = optionInfo.SkillCooldown,
                        Chance = optionInfo.SkillChance,
                        Value = optionInfo.SkillValue,
                        ValueOperation = optionInfo.SkillValueType.ToString(),
                        StatType = optionInfo.SkillStatType.ToString(),
                        StatReferenceType = optionInfo.StatReferenceType.ToString(),
                        BuffDuration = optionInfo.BuffDuration,
                    };
                return new HackAndSlashHistoricalRuneOption
                {
                    Cp = optionInfo.Cp,
                    Stats = stats,
                    Skill = skill,
                };
            }

            private static IReadOnlyList<HackAndSlashHistoricalStat> GetCostumeStats(
                Costume costume,
                CostumeStatSheet? costumeStatSheet)
            {
                if (costumeStatSheet is null)
                {
                    return Array.Empty<HackAndSlashHistoricalStat>();
                }

                return (costumeStatSheet.OrderedList ?? Array.Empty<CostumeStatSheet.Row>())
                    .Where(row => row.CostumeId == costume.Id)
                    .Select(row => new HackAndSlashHistoricalStat
                    {
                        StatType = row.StatType.ToString(),
                        Value = row.Stat,
                    })
                    .ToArray();
            }

            private static IReadOnlyList<HackAndSlashHistoricalStat> GetFoodStats(
                Consumable food,
                ConsumableItemSheet? consumableItemSheet)
            {
                if (consumableItemSheet is null || !consumableItemSheet.TryGetValue(food.Id, out var row))
                {
                    throw new InvalidOperationException(
                        $"Historical ConsumableItemSheet has no row for food {food.Id}.");
                }

                return row.Stats.Select(stat => new HackAndSlashHistoricalStat
                {
                    StatType = stat.StatType.ToString(),
                    Value = stat.BaseValue,
                }).ToArray();
            }

            private static IReadOnlyList<HackAndSlashHistoricalModifier> AggregateModifiers(
                IReadOnlyList<StatModifier> modifiers)
            {
                return modifiers
                    .GroupBy(modifier => (modifier.StatType, modifier.Operation))
                    .OrderBy(group => group.Key.StatType.ToString(), StringComparer.Ordinal)
                    .ThenBy(group => group.Key.Operation.ToString(), StringComparer.Ordinal)
                    .Select(group => new HackAndSlashHistoricalModifier
                    {
                        StatType = group.Key.StatType.ToString(),
                        Operation = group.Key.Operation.ToString(),
                        Value = group.Aggregate(0L, (total, modifier) => checked(total + modifier.Value)),
                    })
                    .ToArray();
            }

            private static long GetRecordedAdventureCp(IWorldState outputWorldState, Address avatarAddress)
            {
                var account = outputWorldState.GetAccountState(Addresses.GetCpAccountAddress(BattleType.Adventure))
                    ?? throw new InvalidOperationException("Post-execution adventure CP account is missing.");
                var serialized = account.GetState(avatarAddress);
                if (serialized is null or Null)
                {
                    throw new InvalidOperationException(
                        $"Post-execution adventure CP is missing for avatar {avatarAddress}.");
                }

                return new CpState(serialized).Cp;
            }

            private static IReadOnlyDictionary<int, string> GetNames<T>(IWorldState worldState)
                where T : ISheet, new()
            {
                var csv = worldState.GetSheetCsv<T>();
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(csv)));
                if (!NameCache.ContainsKey(hash) && NameCache.Count >= MaxNameCacheEntries)
                {
                    NameCache.Clear();
                }

                return NameCache.GetOrAdd(hash, _ => ParseNames<T>(csv));
            }

            private static IReadOnlyDictionary<int, string> ParseNames<T>(string csv)
                where T : ISheet, new()
            {
                using var textReader = new StringReader(csv);
                using var reader = new CsvReader(textReader, CultureInfo.InvariantCulture);
                if (!reader.Read())
                {
                    throw new InvalidOperationException($"Historical {typeof(T).Name} CSV is empty.");
                }

                reader.ReadHeader();
                var names = new Dictionary<int, string>();
                while (reader.Read())
                {
                    var idText = reader.GetField("id");
                    var name = reader.GetField("_name");
                    if (int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) &&
                        !string.IsNullOrWhiteSpace(name))
                    {
                        names[id] = name.Trim();
                    }
                }

                return names;
            }

            private static string RequireName<T>(IReadOnlyDictionary<int, string> names, int templateId)
                where T : ISheet, new()
            {
                if (names.TryGetValue(templateId, out var name))
                {
                    return name;
                }

                throw new InvalidOperationException(
                    $"Historical {typeof(T).Name} CSV has no _name for template {templateId}.");
            }
        }
    }
}
