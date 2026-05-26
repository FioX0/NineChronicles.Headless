using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using Libplanet.Crypto;
using Libplanet.Explorer.GraphTypes;
using Nekoyume.Action;
using Nekoyume.Model.Collection;
using NineChronicles.Headless.GraphTypes.Input;

namespace NineChronicles.Headless.GraphTypes;

public partial class ActionQuery
{
    private static ICollectionMaterial ToCollectionMaterial(CollectionMaterialInput input)
    {
        if (input.NonFungibleId is { } nonFungibleId)
        {
            return new NonFungibleCollectionMaterial
            {
                ItemId = input.ItemId,
                ItemCount = input.ItemCount,
                NonFungibleId = nonFungibleId,
                Level = input.Level ?? 0,
                SkillContains = input.SkillContains ?? false,
            };
        }

        return new FungibleCollectionMaterial
        {
            ItemId = input.ItemId,
            ItemCount = input.ItemCount,
        };
    }

    private void RegisterActivateCollection()
    {
        Field<NonNullGraphType<ByteStringType>>(
            "activateCollection",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<AddressType>>
                {
                    Name = "avatarAddress",
                    Description = "Avatar address activating the collection."
                },
                new QueryArgument<NonNullGraphType<ListGraphType<NonNullGraphType<CollectionDataInputType>>>>
                {
                    Name = "collectionData",
                    Description = "Collection IDs and materials to burn. At most 10 collection entries."
                }
            ),
            resolve: context =>
            {
                var avatarAddress = context.GetArgument<Address>("avatarAddress");
                var collectionData = context.GetArgument<List<CollectionDataInput>>("collectionData");

                ActionBase action = new ActivateCollection
                {
                    AvatarAddress = avatarAddress,
                    CollectionData = collectionData
                        .Select(data => (
                            data.CollectionId,
                            data.Materials.Select(ToCollectionMaterial).ToList()))
                        .ToList(),
                };

                return Encode(context, action);
            });
    }
}
