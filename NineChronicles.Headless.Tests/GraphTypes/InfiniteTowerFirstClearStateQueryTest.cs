#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bencodex.Types;
using GraphQL.Execution;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Libplanet.Types.Tx;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Action.Loader;
using Nekoyume.Blockchain.Policy;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using NineChronicles.Headless.GraphTypes;
using NineChronicles.Headless.GraphTypes.States;
using NineChronicles.Headless.Tests.Common;
using NineChronicles.Headless.Utils;
using Xunit;
using BencodexDictionary = Bencodex.Types.Dictionary;

namespace NineChronicles.Headless.Tests.GraphTypes
{
    public sealed class InfiniteTowerFirstClearStateQueryTest
    {
        private readonly IStore _store;
        private readonly IStateStore _stateStore;
        private readonly BlockChain _blockChain;
        private readonly StandaloneContext _standaloneContext;
        private readonly StateMemoryCache _stateMemoryCache = new();
        private readonly PrivateKey _proposer = new();
        private readonly Dictionary<string, string> _sheets = TableSheetsImporter.ImportSheets();

        public InfiniteTowerFirstClearStateQueryTest()
        {
            _sheets[nameof(InfiniteTowerScheduleSheet)] =
                "Id,InfiniteTowerId,StartBlockIndex,EndBlockIndex,DailyFreeTickets,MaxTickets,ResetIntervalBlocks,FloorBegin,FloorEnd\n" +
                "11,7,0,1000000,3,10,100,1,1";
            _sheets[nameof(InfiniteTowerConditionSheet)] =
                "Id,StatType,TargetType,OperationType,Value\n" +
                "1,1,2,1,0\n" +
                "2,2,2,1,0\n" +
                "3,3,2,1,0";
            _sheets[nameof(InfiniteTowerFloorSheet)] = CreateFloorSheet();
            _sheets[nameof(InfiniteTowerFloorWaveSheet)] =
                "Id,FloorId,Wave,Monster1Id,Monster1Level,Monster1Count,Monster2Id,Monster2Level,Monster2Count,Monster3Id,Monster3Level,Monster3Count,Monster4Id,Monster4Level,Monster4Count,HasBoss\n" +
                "1,1,1,201000,1,1,,,,,,,,,,0";

            _store = new DefaultStore(null);
            _stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            IBlockPolicy policy = new BlockPolicySource().GetPolicy();
            var actionEvaluator = new ActionEvaluator(
                policy.PolicyActionsRegistry,
                _stateStore,
                new NCActionLoader());
            var validatorSet = new ValidatorSet(
                new[] { new Validator(_proposer.PublicKey, 10_000_000_000_000_000_000) }.ToList());
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            var genesisBlock = BlockChain.ProposeGenesisBlock(
                transactions: new IAction[]
                    {
                        new InitializeStates(
                            validatorSet: validatorSet,
                            rankingState: new RankingState0(),
                            shopState: new ShopState(),
                            tableSheets: _sheets,
                            gameConfigState: gameConfigState,
                            redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                            adminAddressState: null,
                            activatedAccountsState: new ActivatedAccountsState(ImmutableHashSet<Address>.Empty),
                            goldCurrencyState: new GoldCurrencyState(Currency.Uncapped("ncg", 2, null), 0),
                            goldDistributions: Array.Empty<GoldDistribution>(),
                            pendingActivationStates: Array.Empty<PendingActivationState>())
                    }.Select((action, nonce) => Transaction.Create(
                        nonce,
                        new PrivateKey(),
                        null,
                        new[] { action.PlainValue }))
                    .ToImmutableList(),
                privateKey: new PrivateKey());

            _blockChain = BlockChain.Create(
                policy,
                new VolatileStagePolicy(),
                _store,
                _stateStore,
                genesisBlock,
                actionEvaluator);
            var currencyFactory = new CurrencyFactory(
                () => _blockChain.GetWorldState(_blockChain.Tip.Hash));
            _standaloneContext = new StandaloneContext
            {
                BlockChain = _blockChain,
                Store = _store,
                CurrencyFactory = currencyFactory,
                FungibleAssetValueFactory = new FungibleAssetValueFactory(currencyFactory),
            };
        }

        [Fact]
        public void StateQueryExposesInfiniteTowerFirstClearField()
        {
            var query = new StateQuery();

            Assert.NotNull(query.GetField("infiniteTowerFirstClear"));
        }

        [Fact]
        public async Task SuccessfulFirstClearReturnsTrue()
        {
            var key = new PrivateKey();
            var avatarAddress = Addresses.GetAvatarAddress(key.Address, 0);
            Append(new ActionBase[]
            {
                new CreateAvatar
                {
                    index = 0,
                    hair = 1,
                    lens = 2,
                    ear = 3,
                    tail = 4,
                    name = "towerclear",
                }
            }, key);
            var inputAvatar = _blockChain.GetNextWorldState().GetAvatarState(avatarAddress);
            var selectedEquipment = inputAvatar.inventory.Equipments.First();
            var tx = Append(new ActionBase[]
            {
                new InfiniteTowerBattle
                {
                    AvatarAddress = avatarAddress,
                    InfiniteTowerId = 7,
                    FloorId = 1,
                    Equipments = new List<Guid> { selectedEquipment.ItemId },
                    Costumes = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                    BuyTicketIfNeeded = false,
                    UseNcgForTicket = false,
                }
            }, key);
            var execution = _blockChain.GetTxExecution(_blockChain.Tip.Hash, tx.Id)!;
            Assert.False(
                execution.Fail,
                string.Join(",", execution.ExceptionNames ?? new List<string?>()));

            var result = await ExecuteRawAsync($@"{{
                infiniteTowerFirstClear(txId: ""{tx.Id}"") {{
                    isFirstClear
                }}
            }}");

            Assert.Null(result.Errors);
            var data = (Dictionary<string, object?>)((ExecutionNode)result.Data!).ToValue()!;
            var actual = Assert.IsType<Dictionary<string, object?>>(data["infiniteTowerFirstClear"]);
            Assert.True(Assert.IsType<bool>(actual["isFirstClear"]));
        }

        [Fact]
        public async Task SuccessfulFirstClearReturnsSeasonAndExactRandomRules()
        {
            var key = new PrivateKey();
            var avatarAddress = Addresses.GetAvatarAddress(key.Address, 0);
            Append(new ActionBase[]
            {
                new CreateAvatar
                {
                    index = 0,
                    hair = 1,
                    lens = 2,
                    ear = 3,
                    tail = 4,
                    name = "towerrules",
                }
            }, key);
            var inputAvatar = _blockChain.GetNextWorldState().GetAvatarState(avatarAddress);
            var selectedEquipment = inputAvatar.inventory.Equipments.First();
            var tx = Append(new ActionBase[]
            {
                new InfiniteTowerBattle
                {
                    AvatarAddress = avatarAddress,
                    InfiniteTowerId = 7,
                    FloorId = 1,
                    Equipments = new List<Guid> { selectedEquipment.ItemId },
                    Costumes = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                    BuyTicketIfNeeded = false,
                    UseNcgForTicket = false,
                }
            }, key);
            var block = _blockChain.Tip;
            var expectedSeed = ActionEvaluator.GenerateRandomSeed(
                block.PreEvaluationHash.ToByteArray(),
                tx.Signature,
                0);
            var expectedRandom = new LocalRandom(expectedSeed);
            var inputWorld = _blockChain.GetWorldState(
                _blockChain.GetTxExecution(block.Hash, tx.Id)!.InputState);
            var floor = inputWorld.GetSheet<InfiniteTowerFloorSheet>()[1];
            var conditionSheet = inputWorld.GetSheet<InfiniteTowerConditionSheet>();
            var guaranteed = conditionSheet[1];
            var expectedAppliedIds = new[] { guaranteed.Id }
                .Concat(floor.GetRandomConditionsWithWeights(
                    conditionSheet,
                    expectedRandom,
                    guaranteed.Id).Select(condition => condition.Id))
                .ToArray();

            var result = await ExecuteRawAsync($@"{{
                infiniteTowerFirstClear(txId: ""{tx.Id}"") {{
                    isFirstClear txId blockIndex blockTimestamp blockHash inputStateRoot
                    avatarAddress infiniteTowerId scheduleId floorId
                    buyTicketIfNeeded useNcgForTicket randomSeed
                    configuredRulesFingerprint appliedRulesFingerprint
                    equipmentIds costumeIds foodIds unresolvedCostumeIds unresolvedFoodIds
                    runeSlotInfos {{ slotIndex runeId }}
                    collectionIds collectionModifiers {{ statType operation value }}
                    equipmentCount costumeCount foodCount runeCount collectionCount collectionModifierCount
                    recordedInfiniteTowerCp
                    equipments {{ itemId id level }}
                    costumes {{ itemId id }}
                    foods {{ itemId id }}
                    runes {{ runeId level }}
                    collections {{ id }}
                    avatar {{ level exp actionPoint }}
                    season {{
                        scheduleId infiniteTowerId startBlockIndex endBlockIndex
                        dailyFreeTickets maxTickets resetIntervalBlocks floorBegin floorEnd
                    }}
                    rules {{
                        selectionMode shatterStrikeMaxDamage turnLimit
                        minRandomConditions maxRandomConditions
                        guaranteedCondition {{ id statType targetTypes operation value }}
                        randomConditionCandidates {{
                            weight
                            condition {{ id statType targetTypes operation value }}
                        }}
                        appliedConditions {{ id statType targetTypes operation value }}
                        enemyInitialStatModifiers {{ statType operation value }}
                        waves {{
                            number hasBoss
                            monsters {{ characterId level count }}
                        }}
                    }}
                }}
            }}");

            Assert.Null(result.Errors);
            var data = (Dictionary<string, object?>)((ExecutionNode)result.Data!).ToValue()!;
            var actual = Assert.IsType<Dictionary<string, object?>>(data["infiniteTowerFirstClear"]);
            Assert.True(Assert.IsType<bool>(actual["isFirstClear"]));
            Assert.Equal(tx.Id.ToString(), actual["txId"]);
            Assert.Equal(block.Index, actual["blockIndex"]);
            Assert.Equal(block.Hash.ToString(), actual["blockHash"]);
            Assert.Equal(avatarAddress.ToString(), actual["avatarAddress"]);
            Assert.Equal(7, actual["infiniteTowerId"]);
            Assert.Equal(11, actual["scheduleId"]);
            Assert.Equal(1, actual["floorId"]);
            Assert.False(Assert.IsType<bool>(actual["buyTicketIfNeeded"]));
            Assert.False(Assert.IsType<bool>(actual["useNcgForTicket"]));
            Assert.Equal(expectedSeed, actual["randomSeed"]);
            Assert.Matches("^[0-9a-f]{64}$", Assert.IsType<string>(actual["configuredRulesFingerprint"]));
            Assert.Matches("^[0-9a-f]{64}$", Assert.IsType<string>(actual["appliedRulesFingerprint"]));
            Assert.Equal(
                selectedEquipment.ItemId.ToString(),
                Assert.Single(Assert.IsType<object[]>(actual["equipmentIds"])));
            Assert.Empty(Assert.IsType<object[]>(actual["costumeIds"]));
            Assert.Empty(Assert.IsType<object[]>(actual["foodIds"]));
            Assert.Empty(Assert.IsType<object[]>(actual["unresolvedCostumeIds"]));
            Assert.Empty(Assert.IsType<object[]>(actual["unresolvedFoodIds"]));
            Assert.Empty(Assert.IsType<object[]>(actual["runeSlotInfos"]));
            Assert.Empty(Assert.IsType<object[]>(actual["collectionIds"]));
            Assert.Equal(1, actual["equipmentCount"]);
            Assert.Equal(0, actual["costumeCount"]);
            Assert.Equal(0, actual["foodCount"]);
            Assert.Equal(0, actual["runeCount"]);
            Assert.Equal(0, actual["collectionCount"]);
            var equipment = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(actual["equipments"])));
            Assert.Equal(selectedEquipment.ItemId.ToString(), equipment["itemId"]);
            Assert.Empty(Assert.IsType<object[]>(actual["costumes"]));
            Assert.Empty(Assert.IsType<object[]>(actual["foods"]));
            Assert.Empty(Assert.IsType<object[]>(actual["runes"]));
            Assert.Empty(Assert.IsType<object[]>(actual["collections"]));
            var avatar = Assert.IsType<Dictionary<string, object?>>(actual["avatar"]);
            Assert.Equal(inputAvatar.level, avatar["level"]);
            Assert.Equal(inputAvatar.exp, Convert.ToInt64(avatar["exp"]));
            var execution = _blockChain.GetTxExecution(block.Hash, tx.Id)!;
            var outputWorld = _blockChain.GetWorldState(execution.OutputState);
            var expectedCp = new CpState(outputWorld
                .GetAccountState(Addresses.GetCpAccountAddress(BattleType.InfiniteTower))
                .GetState(avatarAddress)).Cp;
            Assert.Equal(expectedCp, actual["recordedInfiniteTowerCp"]);

            var season = Assert.IsType<Dictionary<string, object?>>(actual["season"]);
            Assert.Equal(11, season["scheduleId"]);
            Assert.Equal(7, season["infiniteTowerId"]);
            Assert.Equal(0L, season["startBlockIndex"]);
            Assert.Equal(1_000_000L, season["endBlockIndex"]);
            Assert.Equal(3, season["dailyFreeTickets"]);
            Assert.Equal(10, season["maxTickets"]);
            Assert.Equal(100, season["resetIntervalBlocks"]);
            Assert.Equal(1, season["floorBegin"]);
            Assert.Equal(1, season["floorEnd"]);

            var rules = Assert.IsType<Dictionary<string, object?>>(actual["rules"]);
            Assert.Equal("WEIGHTED", rules["selectionMode"]);
            Assert.Equal(
                (int)inputWorld.GetGameConfigState().ShatterStrikeMaxDamage,
                rules["shatterStrikeMaxDamage"]);
            Assert.Equal(150, rules["turnLimit"]);
            Assert.Equal(1, rules["minRandomConditions"]);
            Assert.Equal(1, rules["maxRandomConditions"]);
            var guaranteedCondition = Assert.IsType<Dictionary<string, object?>>(
                rules["guaranteedCondition"]);
            Assert.Equal(1, guaranteedCondition["id"]);
            var candidates = Assert.IsType<object[]>(rules["randomConditionCandidates"]);
            Assert.Equal(new[] { 2, 3 }, candidates
                .Select(candidate => Assert.IsType<Dictionary<string, object?>>(candidate))
                .Select(candidate => Assert.IsType<Dictionary<string, object?>>(candidate["condition"]))
                .Select(condition => Convert.ToInt32(condition["id"])));
            Assert.All(candidates, candidate => Assert.Equal(
                1,
                Assert.IsType<Dictionary<string, object?>>(candidate)["weight"]));
            var appliedIds = Assert.IsType<object[]>(rules["appliedConditions"])
                .Select(condition => Assert.IsType<Dictionary<string, object?>>(condition))
                .Select(condition => Convert.ToInt32(condition["id"]))
                .ToArray();
            Assert.Equal(expectedAppliedIds, appliedIds);
            var wave = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(rules["waves"])));
            Assert.Equal(1, wave["number"]);
            var monster = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(wave["monsters"])));
            Assert.Equal(201000, monster["characterId"]);
            Assert.Equal(1, monster["level"]);
            Assert.Equal(1, monster["count"]);
        }

        [Fact]
        public async Task FirstClearReturnsHistoricalLoadoutWithTowerCp()
        {
            var key = new PrivateKey();
            var avatarAddress = Addresses.GetAvatarAddress(key.Address, 0);
            Append(new ActionBase[]
            {
                new CreateAvatar
                {
                    index = 0,
                    hair = 1,
                    lens = 2,
                    ear = 3,
                    tail = 4,
                    name = "towerhistory",
                }
            }, key);
            var inputAvatar = _blockChain.GetNextWorldState().GetAvatarState(avatarAddress);
            var selectedEquipment = inputAvatar.inventory.Equipments.First();
            var tx = Append(new ActionBase[]
            {
                new InfiniteTowerBattle
                {
                    AvatarAddress = avatarAddress,
                    InfiniteTowerId = 7,
                    FloorId = 1,
                    Equipments = new List<Guid> { selectedEquipment.ItemId },
                    Costumes = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                    BuyTicketIfNeeded = false,
                    UseNcgForTicket = false,
                }
            }, key);
            var execution = _blockChain.GetTxExecution(_blockChain.Tip.Hash, tx.Id)!;
            Assert.False(
                execution.Fail,
                string.Join(",", execution.ExceptionNames ?? new List<string?>()));
            var outputWorld = _blockChain.GetWorldState(execution.OutputState);
            var expectedCp = new CpState(outputWorld
                .GetAccountState(Addresses.GetCpAccountAddress(BattleType.InfiniteTower))
                .GetState(avatarAddress)).Cp;

            var result = await ExecuteRawAsync($@"{{
                infiniteTowerFirstClear(txId: ""{tx.Id}"") {{
                    historicalLoadout {{
                        avatar {{ name level exp actionPoint }}
                        recordedInfiniteTowerCp
                        equipments {{
                            name
                            item {{
                                itemId grade id itemSubType elementalType setId level exp
                                stat {{ statType baseValue additionalValue totalValue }}
                                skills {{ id name elementalType power chance statPowerRatio referencedStatType }}
                                buffSkills {{ id name elementalType power chance statPowerRatio referencedStatType }}
                                statsMap {{ hP aTK dEF cRI hIT sPD }}
                            }}
                        }}
                        costumes {{ name item {{ itemId grade id itemSubType elementalType }} statModifiers {{ statType value }} }}
                        foods {{ name item {{ itemId grade id itemSubType elementalType mainStat }} staticStats {{ statType value }} }}
                        runes {{
                            slotIndex name rune {{ runeId level }}
                            option {{
                                cp stats {{ statType operation rawValue effectiveValue }}
                                skill {{ skillId name cooldown chance value valueOperation statType statReferenceType buffDuration }}
                            }}
                        }}
                        collectionSummary {{ activeCount rawModifierCount modifierTotals {{ statType operation value }} }}
                    }}
                }}
            }}");

            Assert.Null(result.Errors);
            var data = (Dictionary<string, object?>)((ExecutionNode)result.Data!).ToValue()!;
            var actual = Assert.IsType<Dictionary<string, object?>>(data["infiniteTowerFirstClear"]);
            var loadout = Assert.IsType<Dictionary<string, object?>>(actual["historicalLoadout"]);
            Assert.Equal(expectedCp, loadout["recordedInfiniteTowerCp"]);
            var avatar = Assert.IsType<Dictionary<string, object?>>(loadout["avatar"]);
            Assert.Equal(inputAvatar.name, avatar["name"]);
            Assert.Equal(inputAvatar.level, avatar["level"]);
            Assert.Equal(inputAvatar.exp, Convert.ToInt64(avatar["exp"]));
            var equipment = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(loadout["equipments"])));
            Assert.False(string.IsNullOrWhiteSpace(Assert.IsType<string>(equipment["name"])));
            var item = Assert.IsType<Dictionary<string, object?>>(equipment["item"]);
            Assert.Equal(selectedEquipment.ItemId.ToString(), item["itemId"]);
            Assert.Equal(selectedEquipment.Id, item["id"]);
            Assert.Equal(selectedEquipment.level, item["level"]);
            Assert.IsType<Dictionary<string, object?>>(item["stat"]);
            Assert.IsType<Dictionary<string, object?>>(item["statsMap"]);
            Assert.Empty(Assert.IsType<object[]>(loadout["costumes"]));
            Assert.Empty(Assert.IsType<object[]>(loadout["foods"]));
            Assert.Empty(Assert.IsType<object[]>(loadout["runes"]));
            var collections = Assert.IsType<Dictionary<string, object?>>(loadout["collectionSummary"]);
            Assert.Equal(0, collections["activeCount"]);
            Assert.Equal(0, collections["rawModifierCount"]);
        }

        [Fact]
        public async Task EffectiveRulesExposeExactAppliedRules()
        {
            var condition = new InfiniteTowerFirstClearRules.Condition
            {
                Id = 42,
                StatType = "ATK",
                TargetTypes = new[] { "Enemy" },
                Operation = "Percentage",
                Value = -50,
            };
            var rules = new InfiniteTowerFirstClearRules.RuleSet
            {
                SelectionMode = "WEIGHTED",
                RequiredCp = 123,
                AppliedConditions = new[] { condition },
            };
            var source = new InfiniteTowerFirstClearResult
            {
                IsFirstClear = true,
                AvatarAddress = new PrivateKey().Address,
                Season = new InfiniteTowerFirstClearRules.Season(),
                Rules = rules,
            };

            var result = await GraphQLTestUtils.ExecuteQueryAsync<InfiniteTowerFirstClearResultType>(
                @"{
                    rules { selectionMode requiredCp appliedConditions { id statType targetTypes operation value } }
                    effectiveRules { selectionMode requiredCp appliedConditions { id statType targetTypes operation value } }
                }",
                source: source);

            Assert.Null(result.Errors);
            var actual = Assert.IsType<Dictionary<string, object?>>(
                ((ExecutionNode)result.Data!).ToValue());
            Assert.Equal(actual["rules"], actual["effectiveRules"]);
            var effectiveRules = Assert.IsType<Dictionary<string, object?>>(actual["effectiveRules"]);
            var applied = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(effectiveRules["appliedConditions"])));
            Assert.Equal(42, applied["id"]);
            Assert.Equal("ATK", applied["statType"]);
            Assert.Equal(-50, applied["value"]);
        }

        [Fact]
        public async Task UnknownOrStagedTransactionReturnsNull()
        {
            await AssertNullAsync(new TxId());

            var staged = Transaction.Create(
                0,
                new PrivateKey(),
                _blockChain.Genesis.Hash,
                ImmutableArray<IValue>.Empty);
            _blockChain.StageTransaction(staged);

            await AssertNullAsync(staged.Id);
        }

        [Fact]
        public async Task SuccessfulWrongActionReturnsNull()
        {
            var tx = Append(new ActionBase[]
            {
                new CreateAvatar
                {
                    index = 0,
                    hair = 1,
                    lens = 2,
                    ear = 3,
                    tail = 4,
                    name = "wrongaction",
                }
            }, new PrivateKey());

            await AssertNullAsync(tx.Id);
        }

        [Fact]
        public async Task FailedInfiniteTowerBattleReturnsNull()
        {
            var tx = Append(new ActionBase[]
            {
                new InfiniteTowerBattle
                {
                    AvatarAddress = new PrivateKey().Address,
                    InfiniteTowerId = 7,
                    FloorId = 1,
                    Equipments = new List<Guid>(),
                    Costumes = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                }
            }, new PrivateKey());
            Assert.True(_blockChain.GetTxExecution(_blockChain.Tip.Hash, tx.Id)!.Fail);

            await AssertNullAsync(tx.Id);
        }

        [Fact]
        public void MalformedCurrentInfiniteTowerPayloadRaisesContextualError()
        {
            var txId = new TxId();
            var blockHash = _blockChain.Tip.Hash;
            var typeIdentifier = typeof(InfiniteTowerBattle)
                .GetCustomAttribute<ActionTypeAttribute>()!
                .TypeIdentifier;
            var action = BencodexDictionary.Empty.Add((Text)"type_id", typeIdentifier);

            var error = Assert.Throws<GraphQL.ExecutionError>(() =>
                StateQuery.TryGetInfiniteTowerBattle(action, txId, blockHash, out _));

            Assert.Contains(txId.ToString(), error.Message);
            Assert.Contains(blockHash.ToString(), error.Message);
            Assert.IsType<InvalidActionException>(error.InnerException);
        }

        [Fact]
        public void SeasonResetMakesOldClearEligibleAgain()
        {
            var avatarAddress = new PrivateKey().Address;
            var scheduleSheet = new InfiniteTowerScheduleSheet();
            scheduleSheet.Set(
                "Id,InfiniteTowerId,StartBlockIndex,EndBlockIndex,DailyFreeTickets,MaxTickets,ResetIntervalBlocks,FloorBegin,FloorEnd\n" +
                "11,7,100,1000,3,10,100,1,100");
            var input = CreateInfiniteTowerInfo(avatarAddress, clearedFloor: 10, lastResetBlockIndex: 50);
            var output = CreateInfiniteTowerInfo(avatarAddress, clearedFloor: 1, lastResetBlockIndex: 101);

            Assert.True(StateQuery.IsInfiniteTowerFirstClearTransition(
                input,
                output,
                scheduleSheet[11],
                floorId: 1));
            Assert.False(StateQuery.IsInfiniteTowerFirstClearTransition(
                output,
                output,
                scheduleSheet[11],
                floorId: 1));
        }

        [Fact]
        public void AppliedFingerprintSeparatesDifferentRandomConditionRolls()
        {
            InfiniteTowerFirstClearRules.Condition Condition(int id) => new()
            {
                Id = id,
                StatType = "ATK",
                TargetTypes = new[] { "Enemy" },
                Operation = "Percentage",
                Value = -10 * id,
            };
            InfiniteTowerFirstClearRules.RuleSet Rules(int appliedId) => new()
            {
                SelectionMode = "WEIGHTED",
                MinRandomConditions = 1,
                MaxRandomConditions = 1,
                GuaranteedCondition = Condition(1),
                RandomConditionCandidates = new[]
                {
                    new InfiniteTowerFirstClearRules.ConditionCandidate
                    {
                        Weight = 1,
                        Condition = Condition(2),
                    },
                    new InfiniteTowerFirstClearRules.ConditionCandidate
                    {
                        Weight = 1,
                        Condition = Condition(3),
                    },
                },
                AppliedConditions = new[] { Condition(1), Condition(appliedId) },
            };

            var rulesX = Rules(2);
            var rulesY = Rules(3);

            Assert.Equal(
                StateQuery.ComputeRulesFingerprint(1, rulesX, false),
                StateQuery.ComputeRulesFingerprint(1, rulesY, false));
            Assert.NotEqual(
                StateQuery.ComputeRulesFingerprint(1, rulesX, true),
                StateQuery.ComputeRulesFingerprint(1, rulesY, true));
        }

        [Fact]
        public void UniformRuleSelectionUsesEntireOrderedConditionSheet()
        {
            IWorld world = new World(_blockChain.GetNextWorldState());
            world = world.SetLegacyState(
                Addresses.GetSheetAddress<InfiniteTowerFloorSheet>(),
                CreateFloorSheet(weighted: false).Serialize());

            var rules = StateQuery.BuildInfiniteTowerRuleSet(world, floorId: 1, randomSeed: 42);

            Assert.Equal("UNIFORM_ALL", rules.SelectionMode);
            Assert.Equal(new[] { 2, 3 }, rules.RandomConditionCandidates
                .Select(candidate => candidate.Condition.Id));
            Assert.All(rules.RandomConditionCandidates, candidate => Assert.Null(candidate.Weight));
            Assert.Equal(1, rules.AppliedConditions[0].Id);
            Assert.Equal(2, rules.AppliedConditions.Count);
            Assert.Contains(rules.AppliedConditions[1].Id, new[] { 2, 3 });
        }

        [Fact]
        public async Task FalseResultLeavesFirstClearOnlyFieldsNull()
        {
            var source = new InfiniteTowerFirstClearResult
            {
                IsFirstClear = false,
                AvatarAddress = new PrivateKey().Address,
                Season = new InfiniteTowerFirstClearRules.Season(),
            };

            var result = await GraphQLTestUtils.ExecuteQueryAsync<InfiniteTowerFirstClearResultType>(
                @"{
                    isFirstClear configuredRulesFingerprint appliedRulesFingerprint rules
                    { selectionMode }
                    effectiveRules { selectionMode }
                    historicalLoadout { recordedInfiniteTowerCp }
                    equipmentIds costumeIds foodIds runeSlotInfos { runeId }
                    equipmentCount costumeCount foodCount runeCount collectionCount
                    recordedInfiniteTowerCp equipments { id } costumes { id } foods { id }
                    runes { runeId } collections { id } avatar { level }
                }",
                source: source);

            Assert.Null(result.Errors);
            var actual = Assert.IsType<Dictionary<string, object?>>(
                ((ExecutionNode)result.Data!).ToValue());
            Assert.False(Assert.IsType<bool>(actual["isFirstClear"]));
            Assert.Null(actual["configuredRulesFingerprint"]);
            Assert.Null(actual["appliedRulesFingerprint"]);
            Assert.Null(actual["rules"]);
            Assert.Null(actual["effectiveRules"]);
            Assert.Null(actual["historicalLoadout"]);
            Assert.Null(actual["recordedInfiniteTowerCp"]);
            Assert.Null(actual["equipments"]);
            Assert.Null(actual["costumes"]);
            Assert.Null(actual["foods"]);
            Assert.Null(actual["runes"]);
            Assert.Null(actual["collections"]);
            Assert.Null(actual["avatar"]);
            Assert.Empty(Assert.IsType<object[]>(actual["equipmentIds"]));
            Assert.Equal(0, actual["equipmentCount"]);
        }

        [Fact]
        public async Task SchemaUsesStableDescriptiveRuleTypeNames()
        {
            var result = await ExecuteRawAsync(@"{
                __type(name: ""InfiniteTowerFirstClearResultType"") {
                    fields { name type { name ofType { name } } }
                }
            }");

            Assert.Null(result.Errors);
            var data = (Dictionary<string, object?>)((ExecutionNode)result.Data!).ToValue()!;
            var type = Assert.IsType<Dictionary<string, object?>>(data["__type"]);
            var fields = Assert.IsType<object[]>(type["fields"])
                .Select(field => Assert.IsType<Dictionary<string, object?>>(field))
                .ToDictionary(field => Assert.IsType<string>(field["name"]));
            var seasonType = Assert.IsType<Dictionary<string, object?>>(fields["season"]["type"]);
            var seasonOfType = Assert.IsType<Dictionary<string, object?>>(seasonType["ofType"]);
            Assert.Equal("InfiniteTowerFirstClearSeasonType", seasonOfType["name"]);
            var rulesType = Assert.IsType<Dictionary<string, object?>>(fields["rules"]["type"]);
            Assert.Equal("InfiniteTowerFirstClearRuleSetType", rulesType["name"]);
            var effectiveRulesType = Assert.IsType<Dictionary<string, object?>>(fields["effectiveRules"]["type"]);
            Assert.Equal("InfiniteTowerFirstClearRuleSetType", effectiveRulesType["name"]);
            var historicalLoadoutType = Assert.IsType<Dictionary<string, object?>>(fields["historicalLoadout"]["type"]);
            Assert.Equal("InfiniteTowerHistoricalLoadoutType", historicalLoadoutType["name"]);
        }

        private async Task AssertNullAsync(TxId txId)
        {
            var result = await ExecuteRawAsync($@"{{
                infiniteTowerFirstClear(txId: ""{txId}"") {{ isFirstClear }}
            }}");
            Assert.Null(result.Errors);
            var data = (Dictionary<string, object?>)((ExecutionNode)result.Data!).ToValue()!;
            Assert.Null(data["infiniteTowerFirstClear"]);
        }

        private InfiniteTowerInfo CreateInfiniteTowerInfo(
            Address avatarAddress,
            int clearedFloor,
            long lastResetBlockIndex)
        {
            return new InfiniteTowerInfo(new Bencodex.Types.List(
                avatarAddress.Serialize(),
                7.Serialize(),
                clearedFloor.Serialize(),
                3.Serialize(),
                0.Serialize(),
                0.Serialize(),
                lastResetBlockIndex.Serialize(),
                lastResetBlockIndex.Serialize()));
        }

        private Transaction Append(IReadOnlyList<ActionBase> actions, PrivateKey key)
        {
            var tx = _blockChain.MakeTransaction(key, actions);
            var block = _blockChain.ProposeBlock(
                _proposer,
                _blockChain.GetBlockCommit(_blockChain.Tip.Index));
            _blockChain.Append(block, GenerateBlockCommit(block.Index, block.Hash, _proposer));
            return tx;
        }

        private Task<GraphQL.ExecutionResult> ExecuteRawAsync(string query)
        {
            return GraphQLTestUtils.ExecuteQueryAsync<StateQuery>(
                query,
                source: new StateContext(
                    _blockChain.GetWorldState(_blockChain.Tip.Hash),
                    _blockChain.Tip.Index,
                    _stateMemoryCache),
                standaloneContext: _standaloneContext);
        }

        private string CreateFloorSheet(bool weighted = true)
        {
            var header =
                "Id,Floor,RequiredCp,MaxCp,ForbiddenItemSubTypes,MinItemGrade,MaxItemGrade,MinItemLevel,MaxItemLevel,GuaranteedConditionId,MinRandomConditions,MaxRandomConditions,RandomConditionId1,RandomConditionWeight1,RandomConditionId2,RandomConditionWeight2,RandomConditionId3,RandomConditionWeight3,RandomConditionId4,RandomConditionWeight4,RandomConditionId5,RandomConditionWeight5,ItemRewardId1,ItemRewardCount1,ItemRewardId2,ItemRewardCount2,ItemRewardId3,ItemRewardCount3,ItemRewardId4,ItemRewardCount4,ItemRewardId5,ItemRewardCount5,FungibleAssetRewardTicker1,FungibleAssetRewardAmount1,FungibleAssetRewardTicker2,FungibleAssetRewardAmount2,FungibleAssetRewardTicker3,FungibleAssetRewardAmount3,FungibleAssetRewardTicker4,FungibleAssetRewardAmount4,FungibleAssetRewardTicker5,FungibleAssetRewardAmount5,NcgCost,MaterialCostId,MaterialCostCount,ForbiddenRuneTypes,RequiredElementalType,hp_option,atk_option,def_option,cri_option,hit_option,spd_option";
            var fields = Enumerable.Repeat(string.Empty, 53).ToArray();
            fields[0] = "1";
            fields[1] = "1";
            fields[9] = "1";
            fields[10] = "1";
            fields[11] = "1";
            if (weighted)
            {
                fields[12] = "2";
                fields[13] = "1";
                fields[14] = "3";
                fields[15] = "1";
            }
            return header + "\n" + string.Join(",", fields);
        }

        private BlockCommit? GenerateBlockCommit(
            long height,
            BlockHash hash,
            PrivateKey validator)
        {
            return height != 0
                ? new BlockCommit(
                    height,
                    0,
                    hash,
                    ImmutableArray<Vote>.Empty.Add(new VoteMetadata(
                        height,
                        0,
                        hash,
                        DateTimeOffset.UtcNow,
                        validator.PublicKey,
                        10_000_000_000_000_000_000,
                        VoteFlag.PreCommit).Sign(validator)))
                : null;
        }
    }
}
