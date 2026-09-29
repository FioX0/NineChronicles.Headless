#nullable enable

using System;
using System.Collections.Generic;

namespace NineChronicles.Headless.GraphTypes.States
{
    public static class InfiniteTowerFirstClearRules
    {
        public sealed class Season
        {
            public int ScheduleId { get; init; }
            public int InfiniteTowerId { get; init; }
            public long StartBlockIndex { get; init; }
            public long EndBlockIndex { get; init; }
            public int DailyFreeTickets { get; init; }
            public int MaxTickets { get; init; }
            public int ResetIntervalBlocks { get; init; }
            public int FloorBegin { get; init; }
            public int FloorEnd { get; init; }
        }

        public sealed class RuleSet
        {
            public string SelectionMode { get; init; } = string.Empty;
            public int ShatterStrikeMaxDamage { get; init; }
            public int TurnLimit { get; init; }
            public long? RequiredCp { get; init; }
            public long? MaxCp { get; init; }
            public int? MinItemGrade { get; init; }
            public int? MaxItemGrade { get; init; }
            public int? MinItemLevel { get; init; }
            public int? MaxItemLevel { get; init; }
            public IReadOnlyList<string> ForbiddenItemSubTypes { get; init; } = Array.Empty<string>();
            public IReadOnlyList<string> ForbiddenRuneTypes { get; init; } = Array.Empty<string>();
            public IReadOnlyList<string> RequiredElementalTypes { get; init; } = Array.Empty<string>();
            public int MinRandomConditions { get; init; }
            public int MaxRandomConditions { get; init; }
            public Condition? GuaranteedCondition { get; init; }
            public IReadOnlyList<ConditionCandidate> RandomConditionCandidates { get; init; } = Array.Empty<ConditionCandidate>();
            public IReadOnlyList<Condition> AppliedConditions { get; init; } = Array.Empty<Condition>();
            public IReadOnlyList<StatModifier> EnemyInitialStatModifiers { get; init; } = Array.Empty<StatModifier>();
            public IReadOnlyList<Wave> Waves { get; init; } = Array.Empty<Wave>();
        }

        public sealed class ConditionCandidate
        {
            public int? Weight { get; init; }
            public Condition Condition { get; init; } = null!;
        }

        public sealed class Condition
        {
            public int Id { get; init; }
            public string StatType { get; init; } = string.Empty;
            public IReadOnlyList<string> TargetTypes { get; init; } = Array.Empty<string>();
            public string Operation { get; init; } = string.Empty;
            public int Value { get; init; }
        }

        public sealed class StatModifier
        {
            public string StatType { get; init; } = string.Empty;
            public string Operation { get; init; } = string.Empty;
            public long Value { get; init; }
        }

        public sealed class Wave
        {
            public int Number { get; init; }
            public bool HasBoss { get; init; }
            public IReadOnlyList<Monster> Monsters { get; init; } = Array.Empty<Monster>();
        }

        public sealed class Monster
        {
            public int CharacterId { get; init; }
            public int Level { get; init; }
            public int Count { get; init; }
        }
    }
}
