using System;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class InfiniteTowerSimulationResult
    {
        public int floor { get; set; }
        public decimal winPercentage { get; set; }
        public int[] conditionIds { get; set; } = Array.Empty<int>();
        public string[] conditionDescriptions { get; set; } = Array.Empty<string>();
    }
}


