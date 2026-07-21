using GraphQL.Types;
using Nekoyume.Model.BattleStatus.Arena;
using static Nekoyume.Model.BattleStatus.Arena.ArenaSkill;

namespace NineChronicles.Headless.GraphTypes.Abstractions
{
    internal class ArenaSkillInfoType : ObjectGraphType<ArenaSkillInfo>
    {
        public ArenaSkillInfoType()
        {
            Field<ArenaCharacterType>(
                nameof(ArenaSkillInfo.Target),
                resolve: context => context.Source.Target);
            Field<LongGraphType>(
                nameof(ArenaSkillInfo.Effect),
                resolve: context => context.Source.Effect);
            Field<BooleanGraphType>(
                nameof(ArenaSkillInfo.Critical),
                resolve: context => context.Source.Critical);
            Field<StringGraphType>(
                nameof(ArenaSkillInfo.SkillCategory),
                resolve: context => context.Source.SkillCategory.ToString());
            Field<StringGraphType>(
                nameof(ArenaSkillInfo.ElementalType),
                resolve: context => context.Source.ElementalType.ToString());
            Field<StringGraphType>(
                nameof(ArenaSkillInfo.SkillTargetType),
                resolve: context => context.Source.SkillTargetType.ToString());
            Field<IntGraphType>(
                nameof(ArenaSkillInfo.Turn),
                resolve: context => context.Source.Turn);
            Field<BooleanGraphType>(
                nameof(ArenaSkillInfo.Affected),
                resolve: context => context.Source.Affected);
            Field<ArenaBuffInfoType>(
                nameof(ArenaSkillInfo.Buff),
                resolve: context => context.Source.Buff);
            Field<ListGraphType<ArenaBuffInfoType>>(
                nameof(ArenaSkillInfo.DispelList),
                resolve: context => context.Source.DispelList);
        }
    }
}
