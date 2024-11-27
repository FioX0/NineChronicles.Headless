using Libplanet.Crypto;
using Nekoyume.Model.State;

namespace NineChronicles.Headless;

public class AdventureBossSeasonStatus
{
    public int BossId { get; set; }
    public long EndBlockIndex { get; set; }
    public long NextStartBlockIndex { get; set; }
    public long StartBlockIndex { get; set; }
    public bool Finished { get; set; }
    public int Floor { get; set; }
    public int MaxFloor { get; set; }
    public string? Name { get; set; }
    public int UsedApPotion { get; set; }
    public int UsedGoldenDust { get; set; }
    public float UsedNcg { get; set; }
}
