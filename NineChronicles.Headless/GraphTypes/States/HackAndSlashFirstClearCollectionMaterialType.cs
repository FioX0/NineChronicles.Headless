#nullable enable

using GraphQL.Types;
using Nekoyume.TableData;

namespace NineChronicles.Headless.GraphTypes.States
{
    public sealed class HackAndSlashFirstClearCollectionMaterialType : ObjectGraphType<CollectionSheet.RequiredMaterial>
    {
        public HackAndSlashFirstClearCollectionMaterialType()
        {
            Field<NonNullGraphType<IntGraphType>>("itemId", resolve: context => context.Source.ItemId);
            Field<NonNullGraphType<IntGraphType>>("count", resolve: context => context.Source.Count);
            Field<NonNullGraphType<IntGraphType>>("level", resolve: context => context.Source.Level);
            Field<NonNullGraphType<BooleanGraphType>>("skillContains", resolve: context => context.Source.SkillContains);
        }
    }
}
