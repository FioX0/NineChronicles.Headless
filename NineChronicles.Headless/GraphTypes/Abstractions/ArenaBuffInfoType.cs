using GraphQL.Types;
using Nekoyume.Model.Buff;

namespace NineChronicles.Headless.GraphTypes.Abstractions
{
    internal class ArenaBuffInfoType : ObjectGraphType<Buff>
    {
        public ArenaBuffInfoType()
        {
            Field<StringGraphType>(
                "BuffType",
                resolve: context => context.Source.GetType().Name);
            Field<IntGraphType>(
                "Id",
                resolve: context => context.Source.BuffInfo.Id);
            Field<IntGraphType>(
                "GroupId",
                resolve: context => context.Source.BuffInfo.GroupId);
            Field<IntGraphType>(
                "Chance",
                resolve: context => context.Source.BuffInfo.Chance);
            Field<IntGraphType>(
                "Duration",
                resolve: context => context.Source.BuffInfo.Duration);
            Field<IntGraphType>(
                nameof(Buff.OriginalDuration),
                resolve: context => context.Source.OriginalDuration);
            Field<IntGraphType>(
                nameof(Buff.RemainedDuration),
                resolve: context => context.Source.RemainedDuration);
            Field<StringGraphType>(
                "SkillTargetType",
                resolve: context => context.Source.BuffInfo.SkillTargetType.ToString());
            Field<BooleanGraphType>(
                "IsBuff",
                resolve: context => context.Source.IsBuff());
            Field<BooleanGraphType>(
                "IsDebuff",
                resolve: context => context.Source.IsDebuff());
            Field<IntGraphType>(
                "Stack",
                resolve: context => context.Source is StatBuff statBuff
                    ? statBuff.Stack
                    : (int?)null);
            Field<IntGraphType>(
                "StatBuffId",
                resolve: context => context.Source is StatBuff statBuff
                    ? statBuff.RowData.Id
                    : (int?)null);
            Field<StringGraphType>(
                "StatType",
                resolve: context => context.Source is StatBuff statBuff
                    ? statBuff.RowData.StatType.ToString()
                    : null);
            Field<StringGraphType>(
                "OperationType",
                resolve: context => context.Source is StatBuff statBuff
                    ? statBuff.RowData.OperationType.ToString()
                    : null);
            Field<LongGraphType>(
                "Value",
                resolve: context => context.Source is StatBuff statBuff
                    ? statBuff.RowData.Value
                    : (long?)null);
            Field<LongGraphType>(
                "CustomBuffValue",
                resolve: context => context.Source is StatBuff statBuff && statBuff.CustomField.HasValue
                    ? statBuff.CustomField.Value.BuffValue
                    : (long?)null);
            Field<IntGraphType>(
                "CustomBuffDuration",
                resolve: context => context.Source is StatBuff statBuff && statBuff.CustomField.HasValue
                    ? statBuff.CustomField.Value.BuffDuration
                    : (int?)null);
        }
    }
}