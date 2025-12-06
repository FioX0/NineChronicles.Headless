using System.Collections.Generic;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class InfiniteTowerSimulationState
    {
        public long? blockIndex { get; set; }
        public List<InfiniteTowerSimulationResult>? result { get; set; }
    }
}


