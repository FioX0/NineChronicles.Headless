#nullable enable

using System;
using System.Collections.Generic;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NineChronicles.Headless.GraphTypes.States.Models;

namespace NineChronicles.Headless.GraphTypes.States
{
    public sealed class InfiniteTowerFirstClearResult
    {
        public bool IsFirstClear { get; init; }
        public string TxId { get; init; } = string.Empty;
        public long BlockIndex { get; init; }
        public DateTimeOffset BlockTimestamp { get; init; }
        public string BlockHash { get; init; } = string.Empty;
        public string InputStateRoot { get; init; } = string.Empty;
        public Address AvatarAddress { get; init; }
        public int InfiniteTowerId { get; init; }
        public int ScheduleId { get; init; }
        public int FloorId { get; init; }
        public bool BuyTicketIfNeeded { get; init; }
        public bool UseNcgForTicket { get; init; }
        public int RandomSeed { get; init; }
        public int RulesFingerprintVersion => 1;
        public string? ConfiguredRulesFingerprint { get; init; }
        public string? AppliedRulesFingerprint { get; init; }
        public InfiniteTowerFirstClearRules.Season Season { get; init; } = null!;
        public InfiniteTowerFirstClearRules.RuleSet? Rules { get; init; }
        public IReadOnlyList<Guid> EquipmentIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<Guid> CostumeIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<Guid> FoodIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<Guid> UnresolvedCostumeIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<Guid> UnresolvedFoodIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<RuneSlotInfo> RuneSlotInfos { get; init; } = Array.Empty<RuneSlotInfo>();
        public IReadOnlyList<int> CollectionIds { get; init; } = Array.Empty<int>();
        public IReadOnlyList<Nekoyume.Model.Stat.StatModifier> CollectionModifiers { get; init; } = Array.Empty<Nekoyume.Model.Stat.StatModifier>();
        public IReadOnlyList<Equipment>? Equipments { get; init; }
        public IReadOnlyList<Costume>? Costumes { get; init; }
        public IReadOnlyList<Consumable>? Foods { get; init; }
        public IReadOnlyList<RuneState>? Runes { get; init; }
        public IReadOnlyList<CollectionSheet.Row>? Collections { get; init; }
        public Lazy<InfiniteTowerHistoricalLoadout?>? HistoricalLoadoutLoader { get; init; }
        public InfiniteTowerHistoricalLoadout? HistoricalLoadout => HistoricalLoadoutLoader?.Value;
        public int EquipmentCount => Equipments?.Count ?? 0;
        public int CostumeCount => Costumes?.Count ?? 0;
        public int FoodCount => Foods?.Count ?? 0;
        public int RuneCount => Runes?.Count ?? 0;
        public int CollectionCount => Collections?.Count ?? 0;
        public int CollectionModifierCount => CollectionModifiers.Count;
        public long? RecordedInfiniteTowerCp { get; init; }
        public AvatarStateType.AvatarStateContext? Avatar { get; init; }
    }
}
