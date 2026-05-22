using GraphQL.Types;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class DustBalanceType : ObjectGraphType<DustBalance>
    {
        public DustBalanceType()
        {
            Field<NonNullGraphType<IntGraphType>>(
                nameof(DustBalance.id),
                resolve: context => context.Source.id);
            Field<NonNullGraphType<StringGraphType>>(
                nameof(DustBalance.name),
                resolve: context => context.Source.name);
            Field<NonNullGraphType<IntGraphType>>(
                nameof(DustBalance.amount),
                resolve: context => context.Source.amount);
        }
    }
}
