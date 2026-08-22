#nullable enable

using GraphQL.Types;
using Nekoyume.TableData;

namespace NineChronicles.Headless.GraphTypes.States
{
    public sealed class HackAndSlashFirstClearCollectionType : ObjectGraphType<CollectionSheet.Row>
    {
        public HackAndSlashFirstClearCollectionType()
        {
            Field<NonNullGraphType<IntGraphType>>("id", resolve: context => context.Source.Id);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashFirstClearCollectionMaterialType>>>>(
                "materials",
                resolve: context => context.Source.Materials);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<HackAndSlashFirstClearStatModifierType>>>>(
                "statModifiers",
                resolve: context => context.Source.StatModifiers);
        }
    }
}
