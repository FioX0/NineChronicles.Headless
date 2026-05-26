using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace NineChronicles.Headless.GraphTypes.Input;

public sealed class CollectionMaterialInput
{
    public int ItemId { get; init; }

    public int ItemCount { get; init; }

    public Guid? NonFungibleId { get; init; }

    public int? Level { get; init; }

    public bool? SkillContains { get; init; }
}

public sealed class CollectionMaterialInputType : InputObjectGraphType<CollectionMaterialInput>
{
    public CollectionMaterialInputType()
    {
        Name = "CollectionMaterialInput";

        Field<NonNullGraphType<IntGraphType>>(
            name: "itemId",
            description: "Collection material item ID.");

        Field<NonNullGraphType<IntGraphType>>(
            name: "itemCount",
            description: "Collection material count required by the collection row.");

        Field<GuidGraphType>(
            name: "nonFungibleId",
            description: "Non-fungible item ID for equipment or costume materials. Omit for fungible material/consumable inputs.");

        Field<IntGraphType>(
            name: "level",
            description: "Optional non-fungible material level metadata.");

        Field<BooleanGraphType>(
            name: "skillContains",
            description: "Optional non-fungible material skill metadata.");
    }

    public override object ParseDictionary(IDictionary<string, object?> value)
    {
        return new CollectionMaterialInput
        {
            ItemId = (int)value["itemId"]!,
            ItemCount = (int)value["itemCount"]!,
            NonFungibleId = ParseGuid(value.TryGetValue("nonFungibleId", out var nonFungibleId) ? nonFungibleId : null),
            Level = ParseInt(value.TryGetValue("level", out var level) ? level : null),
            SkillContains = ParseBool(value.TryGetValue("skillContains", out var skillContains) ? skillContains : null),
        };
    }

    private static Guid? ParseGuid(object? value)
    {
        return value switch
        {
            null => null,
            Guid guid => guid,
            string { Length: > 0 } text => Guid.Parse(text),
            _ => null,
        };
    }

    private static int? ParseInt(object? value)
    {
        return value switch
        {
            null => null,
            int number => number,
            long number => checked((int)number),
            _ => Convert.ToInt32(value),
        };
    }

    private static bool? ParseBool(object? value)
    {
        return value switch
        {
            null => null,
            bool flag => flag,
            _ => Convert.ToBoolean(value),
        };
    }
}
