using GraphQL.Types;

namespace NineChronicles.Headless.GraphTypes.States
{
    public sealed class InfiniteTowerFloorRulesType : ObjectGraphType<InfiniteTowerFloorRules>
    {
        public InfiniteTowerFloorRulesType()
        {
            Field<NonNullGraphType<IntGraphType>>("floorId", resolve: c => c.Source.floorId);
            Field<LongGraphType>("requiredCp", resolve: c => c.Source.requiredCp);
            Field<LongGraphType>("maxCp", resolve: c => c.Source.maxCp);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>(
                "forbiddenItemSubTypes", resolve: c => c.Source.forbiddenItemSubTypes);
            Field<IntGraphType>("minItemGrade", resolve: c => c.Source.minItemGrade);
            Field<IntGraphType>("maxItemGrade", resolve: c => c.Source.maxItemGrade);
            Field<IntGraphType>("minItemLevel", resolve: c => c.Source.minItemLevel);
            Field<IntGraphType>("maxItemLevel", resolve: c => c.Source.maxItemLevel);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>(
                "requiredElementalTypes", resolve: c => c.Source.requiredElementalTypes);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>(
                "forbiddenRuneTypes", resolve: c => c.Source.forbiddenRuneTypes);
        }
    }
}
