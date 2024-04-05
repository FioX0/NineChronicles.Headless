using Libplanet;
using System;
using System.Collections.Generic;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class ProductInfoState
    {
        public Guid ProductId { get; set; }
        public long RegisteredBlockIndex { get; set; }
    }
}
