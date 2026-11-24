using System.Collections.Generic;
using System.Threading.Tasks;
using Bencodex.Types;
using GraphQL.Execution;
using Libplanet.Action.State;
using Libplanet.Mocks;
using Nekoyume;
using Nekoyume.Module;
using Nekoyume.TableData.Crystal;
using NineChronicles.Headless.GraphTypes;
using NineChronicles.Headless.GraphTypes.States;
using Xunit;

namespace NineChronicles.Headless.Tests.GraphTypes
{
    public class CraftCrystalCheckTest
    {
        [Fact]
        public async Task CrystalCostPreservesValuesAboveInt32Max()
        {
            var sheetAddress = Addresses.GetSheetAddress<CrystalHammerPointSheet>();
            var world = new World(MockWorldState.CreateModern()).SetLegacyState(
                sheetAddress, (Text)"recipe_id,max_point,crystal_cost\n1,3,2147483648");
            var result = await GraphQLTestUtils.ExecuteQueryAsync<StateQuery>(
                "{ craftCrystalCheck(avatarAddress: \"0000000000000000000000000000000000000000\", recipeId: 1) " +
                "{ crystalCost maxPoint } }",
                source: new StateContext(world, 9, new StateMemoryCache()));

            Assert.Null(result.Errors);
            var data = (Dictionary<string, object>)((ExecutionNode)result.Data!).ToValue()!;
            var value = (Dictionary<string, object>)data["craftCrystalCheck"];
            Assert.Equal(2_147_483_648L, Assert.IsType<long>(value["crystalCost"]));
            Assert.Equal(3, Assert.IsType<int>(value["maxPoint"]));
        }
    }
}
