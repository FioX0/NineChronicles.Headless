using GraphQL.Types;
using Nekoyume.Model.Stat;

namespace NineChronicles.Headless.GraphTypes.States.Models
{
    public class StatsMapType : ObjectGraphType<StatsMap>
    {
        public StatsMapType()
        {
            Field<NonNullGraphType<LongGraphType>>(nameof(StatsMap.HP));
            Field<NonNullGraphType<LongGraphType>>(nameof(StatsMap.ATK));
            Field<NonNullGraphType<LongGraphType>>(nameof(StatsMap.DEF));
            Field<NonNullGraphType<LongGraphType>>(nameof(StatsMap.CRI));
            Field<NonNullGraphType<LongGraphType>>(nameof(StatsMap.HIT));
            Field<NonNullGraphType<LongGraphType>>(nameof(StatsMap.SPD));
        }
    }
}
