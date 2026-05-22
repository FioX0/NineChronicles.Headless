namespace NineChronicles.Headless.GraphTypes.States
{
    public class InfiniteTowerStatus
    {
        public string avatarAddress { get; set; } = string.Empty;
        public int? infiniteTowerId { get; set; }
        public long currentBlockIndex { get; set; }
        public bool isActive { get; set; }
        public string scheduleStatus { get; set; } = string.Empty;
        public long? scheduleStartBlockIndex { get; set; }
        public long? scheduleEndBlockIndex { get; set; }
        public int? floorBegin { get; set; }
        public int? floorEnd { get; set; }
        public int clearedFloor { get; set; }
        public int currentFloor { get; set; }
        public bool hasPlayableFloor { get; set; }
        public int remainingTickets { get; set; }
        public int maxTickets { get; set; }
        public int dailyFreeTickets { get; set; }
        public int resetIntervalBlocks { get; set; }
        public long? nextTicketRefreshBlockIndex { get; set; }
        public long? blocksUntilNextTicketRefresh { get; set; }
        public long lastResetBlockIndex { get; set; }
        public long lastTicketRefillBlockIndex { get; set; }
        public int totalTicketsUsed { get; set; }
        public int numberOfTicketPurchases { get; set; }
    }
}
