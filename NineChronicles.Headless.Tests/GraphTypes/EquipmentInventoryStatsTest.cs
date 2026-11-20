using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Types;
using Libplanet.Action.State;
using Libplanet.Mocks;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using NineChronicles.Headless.GraphTypes;
using NineChronicles.Headless.GraphTypes.States;
using NineChronicles.Headless.Tests.Common;
using Xunit;

namespace NineChronicles.Headless.Tests.GraphTypes;

public class EquipmentInventoryStatsTest
{
    [Theory]
    [InlineData(StatType.HP, "hP", 2_623_144_430L)]
    [InlineData(StatType.ATK, "aTK", 2_147_483_648L)]
    [InlineData(StatType.DEF, "dEF", 2_147_483_649L)]
    [InlineData(StatType.CRI, "cRI", 2_147_483_650L)]
    [InlineData(StatType.HIT, "hIT", 2_147_483_651L)]
    [InlineData(StatType.SPD, "sPD", 9_007_199_254_740_993L)]
    public async Task FilteredInventoryQueryPreservesStatsAboveInt32Max(
        StatType statType, string field, long expectedValue)
    {
        var itemId = Guid.Parse("00000000-0000-4000-8000-000000000001");
        var row = Fixtures.TableSheetsFX.EquipmentItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Armor);
        var equipment = new Armor(row, itemId, 0);
        equipment.StatsMap.SetStatAdditionalValue(statType, expectedValue - equipment.StatsMap.GetBaseStat(statType));

        var avatar = new AvatarState(Fixtures.AvatarStateFX);
        avatar.inventory = new Inventory();
        avatar.inventory.AddNonFungibleItem(equipment);
        var world = new World(MockWorldState.CreateModern()).SetAvatarState(avatar.address, avatar);
        var state = new StateContext(world, 1, new StateMemoryCache());

        var root = new ObjectGraphType();
        root.Field<NonNullGraphType<StateQuery>>("stateQuery", resolve: _ => state);
        var result = await GraphQLTestUtils.ExecuteQueryAsync(root, $$"""
            query { stateQuery { avatar(avatarAddress: "{{avatar.address.ToHex()}}") {
                inventory { equipments(itemIds: ["{{itemId}}"]) {
                    id, grade, level, itemId, elementalType, itemType, itemSubType,
                    statsMap { hP, aTK, hIT, dEF, sPD, cRI },
                    skills { chance, power, statPowerRatio, id, referencedStatType },
                    buffSkills { chance, power }
                } }
            } } }
            """);

        Assert.Null(result.Errors);
        var data = (Dictionary<string, object>)((ExecutionNode)result.Data!).ToValue()!;
        var stateData = (Dictionary<string, object>)data["stateQuery"];
        var avatarData = (Dictionary<string, object>)stateData["avatar"];
        var inventory = (Dictionary<string, object>)avatarData["inventory"];
        var items = ((System.Collections.IEnumerable)inventory["equipments"]).Cast<Dictionary<string, object>>();
        var item = Assert.Single(items);
        var statMap = (Dictionary<string, object>)item["statsMap"];
        Assert.Equal(expectedValue, Assert.IsType<long>(statMap[field]));

        using var json = new System.IO.MemoryStream();
        await new GraphQL.SystemTextJson.DocumentWriter().WriteAsync(json, result);
        using var response = System.Text.Json.JsonDocument.Parse(json.ToArray());
        var wireStats = response.RootElement.GetProperty("data").GetProperty("stateQuery").GetProperty("avatar")
            .GetProperty("inventory").GetProperty("equipments")[0].GetProperty("statsMap");
        Assert.Equal(expectedValue, wireStats.GetProperty(field).GetInt64());
    }
}
