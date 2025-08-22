using Libplanet.Crypto;

namespace NineChronicles.Headless.GraphTypes
{
    public class ReplayResult
    {
        public long BlockIndex { get; set; }
        public int BlockProtocolVersion { get; set; }
        public Address Miner { get; set; }
        public string PreviousState { get; set; } = string.Empty;
        public string NextState { get; set; } = string.Empty;
        public int RandomSeed { get; set; }
        public Address Signer { get; set; }
        public string TxId { get; set; } = string.Empty;
    }
}
