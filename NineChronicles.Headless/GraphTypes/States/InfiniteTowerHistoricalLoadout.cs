#nullable enable

using System;
using System.Collections.Generic;
using Libplanet.Action.State;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Model.Stat;

namespace NineChronicles.Headless.GraphTypes.States
{
    /// <summary>
    /// Compact historical display projection for an Infinite Tower first-clear loadout.
    /// Names, stats, skills, and options come from the transaction's pre-execution state.
    /// </summary>
    public sealed class InfiniteTowerHistoricalLoadout
    {
        public HackAndSlashHistoricalLoadout.HackAndSlashHistoricalAvatar Avatar { get; init; } = null!;
        public long RecordedInfiniteTowerCp { get; init; }
        public IReadOnlyList<HackAndSlashHistoricalLoadout.HackAndSlashHistoricalEquipment> Equipments { get; init; } =
            Array.Empty<HackAndSlashHistoricalLoadout.HackAndSlashHistoricalEquipment>();
        public IReadOnlyList<HackAndSlashHistoricalLoadout.HackAndSlashHistoricalCostume> Costumes { get; init; } =
            Array.Empty<HackAndSlashHistoricalLoadout.HackAndSlashHistoricalCostume>();
        public IReadOnlyList<HackAndSlashHistoricalLoadout.HackAndSlashHistoricalFood> Foods { get; init; } =
            Array.Empty<HackAndSlashHistoricalLoadout.HackAndSlashHistoricalFood>();
        public IReadOnlyList<HackAndSlashHistoricalLoadout.HackAndSlashHistoricalRune> Runes { get; init; } =
            Array.Empty<HackAndSlashHistoricalLoadout.HackAndSlashHistoricalRune>();
        public HackAndSlashHistoricalLoadout.HackAndSlashHistoricalCollectionSummary CollectionSummary { get; init; } = null!;

        internal static class Builder
        {
            internal static InfiniteTowerHistoricalLoadout Build(
                IWorldState inputWorldState,
                AvatarState inputAvatar,
                IReadOnlyList<Equipment> equipments,
                IReadOnlyList<Costume> costumes,
                IReadOnlyList<Consumable> foods,
                IReadOnlyList<RuneState> runes,
                IReadOnlyList<RuneSlotInfo> runeSlots,
                AllRuneState allRuneState,
                IReadOnlyList<int> collectionIds,
                IReadOnlyList<StatModifier> collectionModifiers,
                long recordedInfiniteTowerCp)
            {
                var shared = HackAndSlashHistoricalLoadout.Builder.BuildWithRecordedCp(
                    inputWorldState,
                    inputAvatar,
                    equipments,
                    costumes,
                    foods,
                    runes,
                    runeSlots,
                    allRuneState,
                    collectionIds,
                    collectionModifiers,
                    recordedInfiniteTowerCp);

                return new InfiniteTowerHistoricalLoadout
                {
                    Avatar = shared.Avatar,
                    RecordedInfiniteTowerCp = recordedInfiniteTowerCp,
                    Equipments = shared.Equipments,
                    Costumes = shared.Costumes,
                    Foods = shared.Foods,
                    Runes = shared.Runes,
                    CollectionSummary = shared.CollectionSummary,
                };
            }
        }
    }
}
