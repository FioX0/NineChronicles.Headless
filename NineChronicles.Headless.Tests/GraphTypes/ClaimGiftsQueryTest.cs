using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using GraphQL.Execution;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Module;
using NineChronicles.Headless.GraphTypes;
using NineChronicles.Headless.GraphTypes.States;
using Xunit;
using static NineChronicles.Headless.Tests.GraphQLTestUtils;

namespace NineChronicles.Headless.Tests.GraphTypes
{
    public class ClaimGiftsQueryTest
    {
        [Fact]
        public async Task ClaimGiftsEncodesExactAvatarAndGiftId()
        {
            var avatar = new PrivateKey().Address;
            const int giftId = 123;
            string query = $"{{ claimGifts(avatarAddress: \"{avatar}\", giftId: {giftId}) }}";
            var result = await ExecuteQueryAsync<ActionQuery>(query, standaloneContext: CreateStandaloneContext());
            Assert.Null(result.Errors);
            var data = (Dictionary<string, object>)((ExecutionNode)result.Data!).ToValue()!;
            var encoded = new Codec().Decode(ByteUtil.ParseHex((string)data["claimGifts"]));
            var action = Assert.IsType<ClaimGifts>(DeserializeNCAction(encoded));
            Assert.Equal(avatar, action.AvatarAddress);
            Assert.Equal(giftId, action.GiftId);
        }

        [Fact]
        public async Task ClaimableGiftsFiltersClaimedAndInactiveRowsIncludingBoundary()
        {
            var avatar = new PrivateKey().Address;
            const string csv = "id,started_block_index,ended_block_index\n" +
                "4,10,20\n2,1,50\n3,21,30\n1,20,20\n";
            IWorld world = new World(MockWorldState.CreateModern());
            world = world.SetLegacyState(Addresses.TableSheet.Derive("ClaimableGiftsSheet"), (Text)csv);
            world = world.SetClaimedGifts(avatar, new List<int> { 2 });
            string query = $"{{ claimableGifts(avatarAddress: \"{avatar}\") }}";
            var result = await ExecuteQueryAsync<StateQuery>(query,
                source: new StateContext(world, 20L, new StateMemoryCache()));
            Assert.Null(result.Errors);
            var data = (Dictionary<string, object>)((ExecutionNode)result.Data!).ToValue()!;
            var ids = ((object[])data["claimableGifts"]).Select(value => (int)value).ToArray();
            Assert.Equal(new[] { 1, 4 }, ids);
        }
    }
}
