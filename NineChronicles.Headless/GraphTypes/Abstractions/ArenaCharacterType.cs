using GraphQL.Types;
using Nekoyume.Model;

namespace NineChronicles.Headless.GraphTypes.Abstractions
{
    internal class ArenaCharacterType : ObjectGraphType<ArenaCharacter>
    {
        public ArenaCharacterType()
        {
            Field<GuidGraphType>(
                nameof(ArenaCharacter.Id),
                resolve: context => context.Source.Id);
            Field<StringGraphType>(
                nameof(ArenaCharacter.OffensiveElementalType),
                resolve: context => context.Source.OffensiveElementalType.ToString());
            Field<StringGraphType>(
                nameof(ArenaCharacter.DefenseElementalType),
                resolve: context => context.Source.DefenseElementalType.ToString());
            Field<StringGraphType>(
                nameof(ArenaCharacter.SizeType),
                resolve: context => context.Source.SizeType.ToString());
            Field<FloatGraphType>(
                nameof(ArenaCharacter.RunSpeed),
                resolve: context => context.Source.RunSpeed);
            Field<FloatGraphType>(
                nameof(ArenaCharacter.AttackRange),
                resolve: context => context.Source.AttackRange);
            Field<IntGraphType>(
                nameof(ArenaCharacter.CharacterId),
                resolve: context => context.Source.CharacterId);
            Field<BooleanGraphType>(
                nameof(ArenaCharacter.IsEnemy),
                resolve: context => context.Source.IsEnemy);
            Field<IntGraphType>(
                nameof(ArenaCharacter.Level),
                resolve: context => context.Source.Level);
            Field<LongGraphType>(
                nameof(ArenaCharacter.CurrentHP),
                resolve: context => context.Source.CurrentHP);
            Field<LongGraphType>(
                nameof(ArenaCharacter.HP),
                resolve: context => context.Source.AdditionalHP);
            Field<LongGraphType>(
                nameof(ArenaCharacter.ATK),
                resolve: context => context.Source.ATK);
            Field<LongGraphType>(
                nameof(ArenaCharacter.DEF),
                resolve: context => context.Source.DEF);
            Field<LongGraphType>(
                nameof(ArenaCharacter.CRI),
                resolve: context => context.Source.CRI);
            Field<LongGraphType>(
                nameof(ArenaCharacter.HIT),
                resolve: context => context.Source.HIT);
            Field<LongGraphType>(
                nameof(ArenaCharacter.SPD),
                resolve: context => context.Source.SPD);
            Field<BooleanGraphType>(
                nameof(ArenaCharacter.IsDead),
                resolve: context => context.Source.IsDead);
        }
    }
}
