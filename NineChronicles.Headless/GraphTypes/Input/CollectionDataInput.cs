using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;

namespace NineChronicles.Headless.GraphTypes.Input;

public sealed class CollectionDataInput
{
    public int CollectionId { get; init; }

    public IReadOnlyList<CollectionMaterialInput> Materials { get; init; } =
        new List<CollectionMaterialInput>();
}

public sealed class CollectionDataInputType : InputObjectGraphType<CollectionDataInput>
{
    public CollectionDataInputType()
    {
        Name = "CollectionDataInput";

        Field<NonNullGraphType<IntGraphType>>(
            name: "collectionId",
            description: "Collection ID to activate.");

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<CollectionMaterialInputType>>>>(
            name: "materials",
            description: "Materials to burn for this collection.");
    }

    public override object ParseDictionary(IDictionary<string, object?> value)
    {
        var materials = ((object[])value["materials"]!)
            .Cast<CollectionMaterialInput>()
            .ToList();

        return new CollectionDataInput
        {
            CollectionId = (int)value["collectionId"]!,
            Materials = materials,
        };
    }
}
