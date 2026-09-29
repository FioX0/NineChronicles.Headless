using System.Collections.Generic;

namespace NineChronicles.Headless.GraphTypes.States
{
    /// <summary>Current on-chain, deterministic entry restrictions for a tower floor.</summary>
    public sealed class InfiniteTowerFloorRules
    {
        public int floorId { get; set; }
        public long? requiredCp { get; set; }
        public long? maxCp { get; set; }
        public List<string> forbiddenItemSubTypes { get; set; } = new();
        public int? minItemGrade { get; set; }
        public int? maxItemGrade { get; set; }
        public int? minItemLevel { get; set; }
        public int? maxItemLevel { get; set; }
        public List<string> requiredElementalTypes { get; set; } = new();
        public List<string> forbiddenRuneTypes { get; set; } = new();
    }
}
