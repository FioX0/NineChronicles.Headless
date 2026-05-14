using GraphQL.Types;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class ArenaPrepStateType : ObjectGraphType<ArenaPrepState>
    {
        public ArenaPrepStateType()
        {
            Field<NonNullGraphType<ArenaPrepAvatarStateType>>(
                nameof(ArenaPrepState.my),
                resolve: context => context.Source.my);
            Field<NonNullGraphType<ArenaPrepAvatarStateType>>(
                nameof(ArenaPrepState.enemy),
                resolve: context => context.Source.enemy);
        }
    }
}
