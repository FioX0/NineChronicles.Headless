using Libplanet;
using System;
using System.Collections.Generic;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class AdventureBossSimulationState
    {
        public long? blockIndex { get; set; }
        public List<AdventureBossSimulationResult>? result { get; set; }
    }
}
