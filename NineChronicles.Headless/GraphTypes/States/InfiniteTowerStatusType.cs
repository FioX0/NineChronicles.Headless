using GraphQL.Types;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class InfiniteTowerStatusType : ObjectGraphType<InfiniteTowerStatus>
    {
        public InfiniteTowerStatusType()
        {
            Field<NonNullGraphType<StringGraphType>>(
                nameof(InfiniteTowerStatus.avatarAddress),
                resolve: context => context.Source.avatarAddress);
            Field<IntGraphType>(
                nameof(InfiniteTowerStatus.infiniteTowerId),
                resolve: context => context.Source.infiniteTowerId);
            Field<NonNullGraphType<LongGraphType>>(
                nameof(InfiniteTowerStatus.currentBlockIndex),
                resolve: context => context.Source.currentBlockIndex);
            Field<NonNullGraphType<BooleanGraphType>>(
                nameof(InfiniteTowerStatus.isActive),
                resolve: context => context.Source.isActive);
            Field<NonNullGraphType<StringGraphType>>(
                nameof(InfiniteTowerStatus.scheduleStatus),
                resolve: context => context.Source.scheduleStatus);
            Field<LongGraphType>(
                nameof(InfiniteTowerStatus.scheduleStartBlockIndex),
                resolve: context => context.Source.scheduleStartBlockIndex);
            Field<LongGraphType>(
                nameof(InfiniteTowerStatus.scheduleEndBlockIndex),
                resolve: context => context.Source.scheduleEndBlockIndex);
            Field<IntGraphType>(
                nameof(InfiniteTowerStatus.floorBegin),
                resolve: context => context.Source.floorBegin);
            Field<IntGraphType>(
                nameof(InfiniteTowerStatus.floorEnd),
                resolve: context => context.Source.floorEnd);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(InfiniteTowerStatus.clearedFloor),
                resolve: context => context.Source.clearedFloor);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(InfiniteTowerStatus.currentFloor),
                resolve: context => context.Source.currentFloor);
            Field<NonNullGraphType<BooleanGraphType>>(
                nameof(InfiniteTowerStatus.hasPlayableFloor),
                resolve: context => context.Source.hasPlayableFloor);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(InfiniteTowerStatus.remainingTickets),
                resolve: context => context.Source.remainingTickets);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(InfiniteTowerStatus.maxTickets),
                resolve: context => context.Source.maxTickets);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(InfiniteTowerStatus.dailyFreeTickets),
                resolve: context => context.Source.dailyFreeTickets);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(InfiniteTowerStatus.resetIntervalBlocks),
                resolve: context => context.Source.resetIntervalBlocks);
            Field<LongGraphType>(
                nameof(InfiniteTowerStatus.nextTicketRefreshBlockIndex),
                resolve: context => context.Source.nextTicketRefreshBlockIndex);
            Field<LongGraphType>(
                nameof(InfiniteTowerStatus.blocksUntilNextTicketRefresh),
                resolve: context => context.Source.blocksUntilNextTicketRefresh);
            Field<NonNullGraphType<LongGraphType>>(
                nameof(InfiniteTowerStatus.lastResetBlockIndex),
                resolve: context => context.Source.lastResetBlockIndex);
            Field<NonNullGraphType<LongGraphType>>(
                nameof(InfiniteTowerStatus.lastTicketRefillBlockIndex),
                resolve: context => context.Source.lastTicketRefillBlockIndex);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(InfiniteTowerStatus.totalTicketsUsed),
                resolve: context => context.Source.totalTicketsUsed);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(InfiniteTowerStatus.numberOfTicketPurchases),
                resolve: context => context.Source.numberOfTicketPurchases);
        }
    }
}
