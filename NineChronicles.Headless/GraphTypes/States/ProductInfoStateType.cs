using GraphQL.Types;
using Libplanet.Explorer.GraphTypes;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;

namespace NineChronicles.Headless.GraphTypes.States
{
    public class ProductInfoStateType : ObjectGraphType<ProductInfoState>
    {
        public ProductInfoStateType()
        {
            Field<NonNullGraphType<GuidGraphType>>(
                nameof(ProductInfoState.ProductId),
                description: "ProductId",
                resolve: context => context.Source.ProductId);

            Field<NonNullGraphType<LongGraphType>>(
                nameof(ProductInfoState.RegisteredBlockIndex),
                description: "RegisteredBlockIndex",
                resolve: context => context.Source.RegisteredBlockIndex);
        }
    }
}
