#nullable enable

using GraphQL.Types;
using Nekoyume.Model.Stat;

namespace NineChronicles.Headless.GraphTypes.States
{
    public sealed class HackAndSlashFirstClearStatModifierType : ObjectGraphType<StatModifier>
    {
        public HackAndSlashFirstClearStatModifierType()
        {
            Field<NonNullGraphType<StringGraphType>>("statType", resolve: context => context.Source.StatType.ToString());
            Field<NonNullGraphType<StringGraphType>>("operation", resolve: context => context.Source.Operation.ToString());
            Field<NonNullGraphType<LongGraphType>>("value", resolve: context => context.Source.Value);
        }
    }
}
