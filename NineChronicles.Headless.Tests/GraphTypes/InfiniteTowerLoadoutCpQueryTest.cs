using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Execution;
using Xunit;
using Xunit.Abstractions;

namespace NineChronicles.Headless.Tests.GraphTypes
{
    public sealed class InfiniteTowerLoadoutCpQueryTest : GraphQLTestBase
    {
        public InfiniteTowerLoadoutCpQueryTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task LoadoutCpExposesExplicitRuneSlotInput()
        {
            var result = await ExecuteQueryAsync(@"{
                __type(name: ""StateQuery"") { fields { name args { name } } }
            }");
            Assert.Null(result.Errors);
            var data = (Dictionary<string, object?>)((ExecutionNode)result.Data!).ToValue()!;
            var type = Assert.IsType<Dictionary<string, object?>>(data["__type"]);
            var fields = Assert.IsType<object[]>(type["fields"]);
            var cp = fields.Select(f => Assert.IsType<Dictionary<string, object?>>(f))
                .Single(f => (string)f["name"]! == "infiniteTowerLoadoutCp");
            var args = Assert.IsType<object[]>(cp["args"]);
            Assert.Contains(args.Select(a => (string)Assert.IsType<Dictionary<string, object?>>(a)["name"]!),
                name => name == "runeSlotInfos");
        }
    }
}
