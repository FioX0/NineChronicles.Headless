using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;

namespace NineChronicles.Headless.GraphTypes;

public class ArenaParticipantType9CAPI : ObjectGraphType<ArenaParticipant9CAPI>
{
    public ArenaParticipantType9CAPI()
    {
        Field<NonNullGraphType<AddressType>>(
            nameof(ArenaParticipant9CAPI.AvatarAddr),
            description: "Address of avatar.",
            resolve: context => context.Source.AvatarAddr);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipant9CAPI.Score),
            description: "Arena score of avatar.",
            resolve: context => context.Source.Score);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipant9CAPI.Rank),
            description: "Arena rank of avatar.",
            resolve: context => context.Source.Rank);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipant9CAPI.WinScore),
            description: "Score for victory.",
            resolve: context => context.Source.WinScore);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipant9CAPI.LoseScore),
            description: "Score for defeat.",
            resolve: context => context.Source.LoseScore);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipant9CAPI.Cp),
            description: "Cp of avatar.",
            resolve: context => context.Source.Cp);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipant9CAPI.PortraitId),
            description: "Portrait icon id.",
            resolve: context => context.Source.PortraitId);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipant9CAPI.Level),
            description: "Level of avatar.",
            resolve: context => context.Source.Level);
        Field<NonNullGraphType<StringGraphType>>(
            nameof(ArenaParticipant9CAPI.NameWithHash),
            description: "Name of avatar.",
            resolve: context => context.Source.NameWithHash);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipant9CAPI.Ticket),
            description: "Ticket",
            resolve: context => context.Source.Ticket);
    }
}
