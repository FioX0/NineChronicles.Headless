using System;
using System.Collections.Generic;
using Libplanet.Crypto;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class ArenaPrepAvatarState
    {
        public Address avatarAddress { get; set; }
        public string nameWithHash { get; set; } = string.Empty;
        public int level { get; set; }
        public bool itemSlotStateExists { get; set; }
        public bool runeSlotStateExists { get; set; }
        public List<Guid> equipments { get; set; } = new();
        public List<Guid> costumes { get; set; } = new();
        public List<ArenaRuneSlotState> runes { get; set; } = new();
        public int allRuneStateCount { get; set; }
        public int collectionModifierCount { get; set; }
        public List<string> collectionModifiers { get; set; } = new();
        public long cp { get; set; }
    }
}
