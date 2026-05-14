using System;
using System.Collections.Generic;
using Libplanet.Crypto;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class ArenaItemSlotState
    {
        public Address avatarAddress { get; set; }
        public Address itemSlotStateAddress { get; set; }
        public Address arenaAvatarStateAddress { get; set; }
        public string battleType { get; set; } = string.Empty;
        public bool itemSlotStateExists { get; set; }
        public bool arenaAvatarStateExists { get; set; }
        public List<Guid> itemSlotEquipments { get; set; } = new();
        public List<Guid> itemSlotCostumes { get; set; } = new();
        public List<Guid> arenaAvatarEquipments { get; set; } = new();
        public List<Guid> arenaAvatarCostumes { get; set; } = new();
    }
}
