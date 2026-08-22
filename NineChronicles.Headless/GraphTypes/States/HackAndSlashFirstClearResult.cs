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
    public sealed class HackAndSlashFirstClearResult
    {
        public bool IsFirstClear { get; init; }
        public string TxId { get; init; } = string.Empty;
        public long BlockIndex { get; init; }
        // Canonical inclusion time of the containing block, never the client-signed tx timestamp.
        public DateTimeOffset BlockTimestamp { get; init; }
        public string BlockHash { get; init; } = string.Empty;
        public int WorldId { get; init; }
        public int StageId { get; init; }
        public int? StageBuffId { get; init; }
        public int TotalPlayCount { get; init; }
        public Address AvatarAddress { get; init; }
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
        public int EquipmentCount => Equipments?.Count ?? 0;
        public int CostumeCount => Costumes?.Count ?? 0;
        public int FoodCount => Foods?.Count ?? 0;
        public int RuneCount => Runes?.Count ?? 0;
        public int CollectionCount => Collections?.Count ?? 0;
        public int CollectionModifierCount => CollectionModifiers.Count;
        // Deliberately lazy: regular first-clear probes and legacy callers do not deserialize
        // historical tables unless they request the compact display projection.
        public Lazy<HackAndSlashHistoricalLoadout?>? HistoricalLoadoutLoader { get; init; }
        public HackAndSlashHistoricalLoadout? HistoricalLoadout => HistoricalLoadoutLoader?.Value;
        public AvatarStateType.AvatarStateContext? Avatar { get; init; }
    }
}
