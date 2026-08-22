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
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Model.Stat;
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
    public sealed class HackAndSlashFirstClearStateQueryTest
    {
        private readonly IStore _store;
        private readonly IStateStore _stateStore;
        private readonly BlockChain _blockChain;
        private readonly StandaloneContext _standaloneContext;
        private readonly StateMemoryCache _stateMemoryCache = new();
        private readonly PrivateKey _proposer = new();
        private readonly Dictionary<string, string> _sheets = TableSheetsImporter.ImportSheets();

        public HackAndSlashFirstClearStateQueryTest()
        {
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
                    }.Select((action, nonce) => Transaction.Create(nonce, new PrivateKey(), null, new[] { action.PlainValue }))
                    .ToImmutableList(),
                privateKey: new PrivateKey());

            _blockChain = BlockChain.Create(
                policy,
                new VolatileStagePolicy(),
                _store,
                _stateStore,
                genesisBlock,
                actionEvaluator);
            var currencyFactory = new CurrencyFactory(() => _blockChain.GetWorldState(_blockChain.Tip.Hash));
            _standaloneContext = new StandaloneContext
            {
                BlockChain = _blockChain,
                Store = _store,
                CurrencyFactory = currencyFactory,
                FungibleAssetValueFactory = new FungibleAssetValueFactory(currencyFactory),
            };
        }

        [Fact]
        public async Task UnknownOrStagedTransactionReturnsNull()
        {
            await AssertNullAsync(new TxId().ToString());

            var key = new PrivateKey();
            var staged = Transaction.Create(
                0,
                key,
                _blockChain.Genesis.Hash,
                ImmutableArray<IValue>.Empty);
            _blockChain.StageTransaction(staged);

            await AssertNullAsync(staged.Id.ToString());
        }

        [Fact]
        public async Task SuccessfulNonHackAndSlashReturnsNull()
        {
            var key = new PrivateKey();
            var tx = Append(new[]
            {
                new CreateAvatar
                {
                    index = 0,
                    hair = 1,
                    lens = 2,
                    ear = 3,
                    tail = 4,
                    name = "avatar",
                }
            }, key);

            Assert.False(_blockChain.GetTxExecution(_blockChain.Tip.Hash, tx.Id)!.Fail);
            await AssertNullAsync(tx.Id.ToString());
        }

        [Fact]
        public void UnknownActionPayloadReturnsFalseWithoutGraphQlError()
        {
            var action = BencodexDictionary.Empty.Add(
                (Text)"type_id",
                (Text)"not_registered_action_for_hack_and_slash_first_clear_test");

            Assert.False(StateQuery.TryGetHackAndSlash(
                action,
                new TxId(),
                _blockChain.Tip.Hash,
                out _));
        }

        [Fact]
        public void MalformedCurrentHackAndSlashPayloadRaisesGraphQlError()
        {
            var txId = new TxId();
            var blockHash = _blockChain.Tip.Hash;
            var currentTypeIdentifier = typeof(HackAndSlash)
                .GetCustomAttribute<ActionTypeAttribute>()!
                .TypeIdentifier;
            var action = BencodexDictionary.Empty.Add(
                (Text)"type_id",
                currentTypeIdentifier);

            var error = Assert.Throws<GraphQL.ExecutionError>(() => StateQuery.TryGetHackAndSlash(
                action,
                txId,
                blockHash,
                out _));

            Assert.Contains(txId.ToString(), error.Message);
            Assert.Contains(blockHash.ToString(), error.Message);
            Assert.IsType<InvalidActionException>(error.InnerException);
        }

        [Fact]
        public async Task MultiActionTransactionReturnsNull()
        {
            var key = new PrivateKey();
            var tx = Append(new ActionBase[]
            {
                new CreateAvatar
                {
                    index = 0,
                    hair = 1,
                    lens = 2,
                    ear = 3,
                    tail = 4,
                    name = "avatar0",
                },
                new CreateAvatar
                {
                    index = 1,
                    hair = 1,
                    lens = 2,
                    ear = 3,
                    tail = 4,
                    name = "avatar1",
                }
            }, key);

            await AssertNullAsync(tx.Id.ToString());
        }

        [Fact]
        public async Task FailedHackAndSlashReturnsNull()
        {
            var tx = Append(new ActionBase[]
            {
                new HackAndSlash
                {
                    AvatarAddress = new PrivateKey().Address,
                    WorldId = 1,
                    StageId = 1,
                    Costumes = new List<Guid>(),
                    Equipments = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                }
            }, new PrivateKey());

            Assert.True(_blockChain.GetTxExecution(_blockChain.Tip.Hash, tx.Id)!.Fail);
            await AssertNullAsync(tx.Id.ToString());
        }

        [Fact]
        public async Task SuccessfulNonFirstClearReturnsFalseAndStageId()
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
                    name = "repeat",
                }
            }, key);

            var action = new HackAndSlash
            {
                AvatarAddress = avatarAddress,
                WorldId = 1,
                StageId = 1,
                StageBuffId = 0,
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
            };
            var firstClear = Append(new ActionBase[] { action }, key);
            var firstExecution = _blockChain.GetTxExecution(_blockChain.Tip.Hash, firstClear.Id)!;
            Assert.False(firstExecution.Fail, string.Join(",", firstExecution.ExceptionNames ?? new List<string?>()));

            var repeat = Append(new ActionBase[] { action }, key);
            var repeatExecution = _blockChain.GetTxExecution(_blockChain.Tip.Hash, repeat.Id)!;
            Assert.False(repeatExecution.Fail, string.Join(",", repeatExecution.ExceptionNames ?? new List<string?>()));

            var result = await ExecuteRawAsync($@"{{
                hackAndSlashFirstClear(txId: ""{repeat.Id}"") {{
                    isFirstClear
                    stageId
                }}
            }}");

            Assert.Null(result.Errors);
            var data = (Dictionary<string, object?>)((ExecutionNode)result.Data!).ToValue()!;
            var actual = Assert.IsType<Dictionary<string, object?>>(data["hackAndSlashFirstClear"]);
            Assert.Equal(2, actual.Count);
            Assert.False(Assert.IsType<bool>(actual["isFirstClear"]));
            Assert.Equal(1, actual["stageId"]);

            var fullResult = await QueryAsync(repeat.Id.ToString());
            Assert.Null(fullResult.Errors);
            var fullData = (Dictionary<string, object?>)((ExecutionNode)fullResult.Data!).ToValue()!;
            var fullActual = Assert.IsType<Dictionary<string, object?>>(
                fullData["hackAndSlashFirstClear"]);
            Assert.False(Assert.IsType<bool>(fullActual["isFirstClear"]));
            Assert.Equal(1, fullActual["stageId"]);
            Assert.Null(fullActual["equipments"]);
            Assert.Null(fullActual["costumes"]);
            Assert.Null(fullActual["foods"]);
            Assert.Null(fullActual["runes"]);
            Assert.Null(fullActual["collections"]);
            Assert.Null(fullActual["avatar"]);
            Assert.Equal(0, fullActual["equipmentCount"]);
            Assert.Equal(0, fullActual["costumeCount"]);
            Assert.Equal(0, fullActual["foodCount"]);
            Assert.Equal(0, fullActual["runeCount"]);
            Assert.Equal(0, fullActual["collectionCount"]);
            Assert.Equal(0, fullActual["collectionModifierCount"]);
        }

        [Fact]
        public async Task SuccessfulFirstClearReturnsPreExecutionAvatarAndActionMetadata()
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
                    name = "clearer",
                }
            }, key);

            var avatarBeforeFight = _blockChain.GetNextWorldState().GetAvatarState(avatarAddress);
            Assert.NotEmpty(avatarBeforeFight.inventory.Equipments);
            var selectedEquipment = avatarBeforeFight.inventory.Equipments.First();

            var action = new HackAndSlash
            {
                AvatarAddress = avatarAddress,
                WorldId = 1,
                StageId = 1,
                StageBuffId = 0,
                Costumes = new List<Guid>(),
                Equipments = new List<Guid> { selectedEquipment.ItemId },
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
            };
            var tx = Append(new ActionBase[] { action }, key);
            var execution = _blockChain.GetTxExecution(_blockChain.Tip.Hash, tx.Id)!;
            Assert.False(execution.Fail, string.Join(",", execution.ExceptionNames ?? new List<string?>()));

            var inputWorldState = _blockChain.GetWorldState(execution.InputState);
            var inputAvatar = inputWorldState.GetAvatarState(avatarAddress);
            var outputAvatar = _blockChain.GetWorldState(execution.OutputState).GetAvatarState(avatarAddress);
            Assert.False(inputAvatar.worldInformation.IsStageCleared(1));
            Assert.True(outputAvatar.worldInformation.IsStageCleared(1));

            var result = await QueryAsync(tx.Id.ToString());
            Assert.Null(result.Errors);
            var data = (Dictionary<string, object?>)((ExecutionNode)result.Data!).ToValue()!;
            var actual = Assert.IsType<Dictionary<string, object?>>(data["hackAndSlashFirstClear"]);
            Assert.True(Assert.IsType<bool>(actual["isFirstClear"]));
            Assert.Equal(tx.Id.ToString(), actual["txId"]);
            Assert.Equal(_blockChain.Tip.Index, actual["blockIndex"]);
            Assert.True(DateTimeOffset.TryParse(
                Assert.IsType<string>(actual["blockTimestamp"]),
                out var returnedBlockTimestamp));
            Assert.Equal(_blockChain.Tip.Timestamp, returnedBlockTimestamp);
            Assert.Equal(_blockChain.Tip.Hash.ToString(), actual["blockHash"]);
            Assert.Equal(1, actual["worldId"]);
            Assert.Equal(1, actual["stageId"]);
            Assert.Equal(0, actual["stageBuffId"]);
            Assert.Equal(avatarAddress.ToString(), actual["avatarAddress"]);
            Assert.Equal(
                selectedEquipment.ItemId.ToString(),
                Assert.Single(Assert.IsType<object[]>(actual["equipmentIds"])));
            Assert.Empty(Assert.IsType<object[]>(actual["costumeIds"]));
            Assert.Empty(Assert.IsType<object[]>(actual["foodIds"]));
            Assert.NotNull(actual["collectionIds"]);
            Assert.NotNull(actual["collectionModifiers"]);

            Assert.Empty(Assert.IsType<object[]>(actual["runeSlotInfos"]));

            Assert.Equal(1, actual["equipmentCount"]);
            Assert.Equal(0, actual["costumeCount"]);
            Assert.Equal(0, actual["foodCount"]);
            Assert.Equal(0, actual["runeCount"]);
            Assert.Equal(0, actual["collectionCount"]);
            var equipment = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(actual["equipments"])));
            Assert.Equal(selectedEquipment.ItemId.ToString(), equipment["itemId"]);
            Assert.Equal(selectedEquipment.Id, equipment["id"]);
            Assert.Equal(selectedEquipment.level, equipment["level"]);
            Assert.Empty(Assert.IsType<object[]>(actual["costumes"]));
            Assert.Empty(Assert.IsType<object[]>(actual["foods"]));
            Assert.Empty(Assert.IsType<object[]>(actual["runes"]));
            Assert.Empty(Assert.IsType<object[]>(actual["collections"]));

            var avatar = Assert.IsType<Dictionary<string, object?>>(actual["avatar"]);
            Assert.Equal(inputAvatar.level, Convert.ToInt32(avatar["level"]));
            Assert.Equal(inputAvatar.exp, Convert.ToInt32(avatar["exp"]));
            var worldInformation = Assert.IsType<Dictionary<string, object?>>(avatar["worldInformation"]);
            var returnedStageCleared = Assert.IsType<bool>(worldInformation["isStageCleared"]);
            Assert.False(returnedStageCleared);
            Assert.Equal(inputAvatar.worldInformation.IsStageCleared(1), returnedStageCleared);
            Assert.NotEqual(outputAvatar.worldInformation.IsStageCleared(1), returnedStageCleared);

            var compactResult = await ExecuteRawAsync($@"{{
                hackAndSlashFirstClear(txId: ""{tx.Id}"") {{
                    historicalLoadout {{
                        avatar {{ name level exp actionPoint }}
                        recordedAdventureCp
                        equipments {{
                            name
                            item {{
                                itemId grade id itemSubType elementalType setId
                                stat {{ statType baseValue additionalValue totalValue }}
                                level exp
                                skills {{ id name elementalType power chance statPowerRatio referencedStatType }}
                                buffSkills {{ id name elementalType power chance statPowerRatio referencedStatType }}
                                statsMap {{ hP aTK dEF cRI hIT sPD }}
                            }}
                        }}
                        costumes {{ name item {{ itemId grade id itemSubType elementalType }} statModifiers {{ statType value }} }}
                        foods {{ name item {{ itemId grade id itemSubType elementalType mainStat }} staticStats {{ statType value }} }}
                        runes {{ slotIndex name rune {{ runeId level }} option {{ cp stats {{ statType operation rawValue effectiveValue }} skill {{ skillId name cooldown chance value valueOperation statType statReferenceType buffDuration }} }} }}
                        collectionSummary {{ activeCount rawModifierCount modifierTotals {{ statType operation value }} }}
                    }}
                }}
            }}");
            Assert.Null(compactResult.Errors);
            var compactData = (Dictionary<string, object?>)((ExecutionNode)compactResult.Data!).ToValue()!;
            var compactFirstClear = Assert.IsType<Dictionary<string, object?>>(compactData["hackAndSlashFirstClear"]);
            var compact = Assert.IsType<Dictionary<string, object?>>(compactFirstClear["historicalLoadout"]);
            var compactAvatar = Assert.IsType<Dictionary<string, object?>>(compact["avatar"]);
            Assert.Equal(inputAvatar.name, compactAvatar["name"]);
            Assert.Equal(inputAvatar.level, compactAvatar["level"]);
            var expectedAdventureCp = new CpState(
                _blockChain.GetWorldState(execution.OutputState)
                    .GetAccountState(Addresses.GetCpAccountAddress(BattleType.Adventure))
                    .GetState(avatarAddress)).Cp;
            Assert.Equal(expectedAdventureCp, compact["recordedAdventureCp"]);
            var compactEquipment = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(compact["equipments"])));
            Assert.Equal("Long Sword", compactEquipment["name"]);
            var compactItem = Assert.IsType<Dictionary<string, object?>>(compactEquipment["item"]);
            Assert.Equal(selectedEquipment.ItemId.ToString(), compactItem["itemId"]);
            Assert.Equal(selectedEquipment.Id, compactItem["id"]);
            foreach (var skillListKey in new[] { "skills", "buffSkills" })
            {
                foreach (var skill in Assert.IsType<object[]>(compactItem[skillListKey]))
                {
                    var historicalSkill = Assert.IsType<Dictionary<string, object?>>(skill);
                    Assert.NotEmpty(Assert.IsType<string>(historicalSkill["name"]));
                }
            }
            Assert.Empty(Assert.IsType<object[]>(compact["costumes"]));
            Assert.Empty(Assert.IsType<object[]>(compact["foods"]));
            Assert.Empty(Assert.IsType<object[]>(compact["runes"]));
            var compactCollections = Assert.IsType<Dictionary<string, object?>>(compact["collectionSummary"]);
            Assert.Equal(0, compactCollections["activeCount"]);
            Assert.Equal(0, compactCollections["rawModifierCount"]);
        }

        [Fact]
        public async Task SuccessfulFirstClearWithMissingCostumeAndFoodReturnsEffectiveLoadout()
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
                    name = "staleloadout",
                }
            }, key);

            var avatarBeforeFight = _blockChain.GetNextWorldState().GetAvatarState(avatarAddress);
            var selectedEquipment = avatarBeforeFight.inventory.Equipments.First();
            var missingCostumeId = Guid.NewGuid();
            var missingFoodId = Guid.NewGuid();
            var tx = Append(new ActionBase[]
            {
                new HackAndSlash
                {
                    AvatarAddress = avatarAddress,
                    WorldId = 1,
                    StageId = 1,
                    StageBuffId = 0,
                    Costumes = new List<Guid> { missingCostumeId },
                    Equipments = new List<Guid> { selectedEquipment.ItemId },
                    Foods = new List<Guid> { missingFoodId },
                    RuneInfos = new List<RuneSlotInfo>(),
                }
            }, key);
            var execution = _blockChain.GetTxExecution(_blockChain.Tip.Hash, tx.Id)!;
            Assert.False(execution.Fail, string.Join(",", execution.ExceptionNames ?? new List<string?>()));

            var result = await QueryAsync(tx.Id.ToString());

            Assert.Null(result.Errors);
            var data = (Dictionary<string, object?>)((ExecutionNode)result.Data!).ToValue()!;
            var actual = Assert.IsType<Dictionary<string, object?>>(data["hackAndSlashFirstClear"]);
            Assert.True(Assert.IsType<bool>(actual["isFirstClear"]));
            Assert.Equal(missingCostumeId.ToString(), Assert.Single(Assert.IsType<object[]>(actual["costumeIds"])));
            Assert.Equal(missingFoodId.ToString(), Assert.Single(Assert.IsType<object[]>(actual["foodIds"])));
            Assert.Equal(missingCostumeId.ToString(), Assert.Single(Assert.IsType<object[]>(actual["unresolvedCostumeIds"])));
            Assert.Equal(missingFoodId.ToString(), Assert.Single(Assert.IsType<object[]>(actual["unresolvedFoodIds"])));
            Assert.Equal(0, actual["costumeCount"]);
            Assert.Equal(0, actual["foodCount"]);
            Assert.Empty(Assert.IsType<object[]>(actual["costumes"]));
            Assert.Empty(Assert.IsType<object[]>(actual["foods"]));
        }

        [Fact]
        public void FirstClearTransitionPredicateChecksBothBoundaries()
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
                    name = "predicate",
                }
            }, key);

            var tx = Append(new ActionBase[]
            {
                new HackAndSlash
                {
                    AvatarAddress = avatarAddress,
                    WorldId = 1,
                    StageId = 1,
                    Costumes = new List<Guid>(),
                    Equipments = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                }
            }, key);
            var execution = _blockChain.GetTxExecution(_blockChain.Tip.Hash, tx.Id)!;
            Assert.False(execution.Fail, string.Join(",", execution.ExceptionNames ?? new List<string?>()));

            var inputAvatar = _blockChain.GetWorldState(execution.InputState).GetAvatarState(avatarAddress);
            var outputAvatar = _blockChain.GetWorldState(execution.OutputState).GetAvatarState(avatarAddress);

            Assert.True(StateQuery.IsFirstClearTransition(inputAvatar, outputAvatar, 1));
            Assert.False(StateQuery.IsFirstClearTransition(outputAvatar, outputAvatar, 1));
            Assert.False(StateQuery.IsFirstClearTransition(inputAvatar, inputAvatar, 1));
        }

        [Fact]
        public void MissingExecutionRootsRaiseExecutionErrorWithTransactionContext()
        {
            var txId = new TxId();
            var blockHash = _blockChain.Tip.Hash;

            var error = Assert.Throws<GraphQL.ExecutionError>(() =>
                StateQuery.ValidateExecutionRoots(null, null, txId, blockHash));

            Assert.Contains(txId.ToString(), error.Message);
            Assert.Contains(blockHash.ToString(), error.Message);
        }

        [Fact]
        public void CollectionDetailsAreOrderedByCollectionId()
        {
            var collectionSheet = new CollectionSheet();
            collectionSheet.Set(_sheets[nameof(CollectionSheet)]);
            var expectedIds = collectionSheet.OrderedList
                .Take(2)
                .Select(row => row.Id)
                .OrderBy(id => id)
                .ToArray();
            var collectionState = new CollectionState();
            foreach (var collectionId in expectedIds.Reverse())
            {
                collectionState.Ids.Add(collectionId);
            }

            var details = StateQuery.ResolveCollections(
                collectionState,
                collectionSheet,
                new TxId(),
                _blockChain.Tip.Hash);

            Assert.Equal(expectedIds, details.Ids);
            Assert.Equal(expectedIds, details.Collections.Select(row => row.Id));
            Assert.Equal(
                details.Collections.SelectMany(row => row.StatModifiers),
                details.Modifiers);
        }

        [Fact]
        public void MissingSelectedRuneRaisesContextualExecutionError()
        {
            const int runeId = 1234;
            var txId = new TxId();
            var blockHash = _blockChain.Tip.Hash;

            var error = Assert.Throws<GraphQL.ExecutionError>(() => StateQuery.ResolveRunes(
                new AllRuneState(),
                new[] { new RuneSlotInfo(2, runeId) },
                txId,
                blockHash));

            Assert.Contains(runeId.ToString(), error.Message);
            Assert.Contains(txId.ToString(), error.Message);
            Assert.Contains(blockHash.ToString(), error.Message);
        }

        [Fact]
        public void SelectedItemsPreserveActionOrder()
        {
            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            var inventory = new[]
            {
                (Id: firstId, Name: "first"),
                (Id: secondId, Name: "second"),
            };

            var selected = StateQuery.ResolveSelectedItems(
                inventory,
                new[] { secondId, firstId },
                item => item.Id,
                "test item",
                new TxId(),
                _blockChain.Tip.Hash);

            Assert.Equal(new[] { secondId, firstId }, selected.Select(item => item.Id));
        }

        [Fact]
        public void MissingSelectedItemRaisesContextualExecutionError()
        {
            var missingId = Guid.NewGuid();
            var txId = new TxId();
            var blockHash = _blockChain.Tip.Hash;

            var error = Assert.Throws<GraphQL.ExecutionError>(() => StateQuery.ResolveSelectedItems(
                Array.Empty<(Guid Id, string Name)>(),
                new[] { missingId },
                item => item.Id,
                "equipment",
                txId,
                blockHash));

            Assert.Contains(missingId.ToString(), error.Message);
            Assert.Contains(txId.ToString(), error.Message);
            Assert.Contains(blockHash.ToString(), error.Message);
        }

        [Fact]
        public void OptionalSelectedItemsPreserveResolvedAndUnresolvedActionOrder()
        {
            var presentId = Guid.NewGuid();
            var missingId = Guid.NewGuid();
            var resolution = StateQuery.ResolveOptionalSelectedItems(
                new[] { (Id: presentId, Name: "present") },
                new[] { missingId, presentId, missingId },
                item => item.Id,
                "costume",
                new TxId(),
                _blockChain.Tip.Hash);

            Assert.Equal(new[] { presentId }, resolution.Items.Select(item => item.Id));
            Assert.Equal(new[] { missingId, missingId }, resolution.UnresolvedIds);
        }

        [Fact]
        public void CorruptInventoryIndexRaisesContextualExecutionError()
        {
            var duplicatedId = Guid.NewGuid();
            var txId = new TxId();
            var blockHash = _blockChain.Tip.Hash;
            var inventory = new[]
            {
                (Id: duplicatedId, Name: "first"),
                (Id: duplicatedId, Name: "duplicate"),
            };

            var error = Assert.Throws<GraphQL.ExecutionError>(() => StateQuery.ResolveSelectedItems(
                inventory,
                new[] { duplicatedId },
                item => item.Id,
                "equipment",
                txId,
                blockHash));

            Assert.Contains("equipment", error.Message);
            Assert.Contains(txId.ToString(), error.Message);
            Assert.Contains(blockHash.ToString(), error.Message);
        }

        [Fact]
        public void ExecutionStateLoadFailuresAreContextual()
        {
            var txId = new TxId();
            var blockHash = _blockChain.Tip.Hash;
            var inner = new InvalidOperationException("corrupt state");

            var error = Assert.Throws<GraphQL.ExecutionError>(() =>
                StateQuery.LoadExecutionState<object>(
                    () => throw inner,
                    "pre-execution world state",
                    txId,
                    blockHash));

            Assert.Same(inner, error.InnerException);
            Assert.Contains("pre-execution world state", error.Message);
            Assert.Contains(txId.ToString(), error.Message);
            Assert.Contains(blockHash.ToString(), error.Message);
        }

        [Fact]
        public async Task ResultTypeProjectsNonEmptyLoadoutAndCollectionValues()
        {
            var equipmentId = Guid.NewGuid();
            var costumeId = Guid.NewGuid();
            var foodId = Guid.NewGuid();
            var result = new HackAndSlashFirstClearResult
            {
                AvatarAddress = new PrivateKey().Address,
                EquipmentIds = new[] { equipmentId },
                CostumeIds = new[] { costumeId },
                FoodIds = new[] { foodId },
                UnresolvedCostumeIds = new[] { costumeId },
                UnresolvedFoodIds = new[] { foodId },
                RuneSlotInfos = new[] { new RuneSlotInfo(2, 1234) },
                CollectionIds = new[] { 42 },
                CollectionModifiers = new[] { new StatModifier(StatType.ATK, StatModifier.OperationType.Add, 7) },
            };

            var execution = await GraphQLTestUtils.ExecuteQueryAsync<HackAndSlashFirstClearResultType>(
                "{ equipmentIds costumeIds foodIds unresolvedCostumeIds unresolvedFoodIds runeSlotInfos { slotIndex runeId } collectionIds collectionModifiers { statType operation value } stageBuffId }",
                source: result);

            Assert.Null(execution.Errors);
            var actual = Assert.IsType<Dictionary<string, object?>>(((ExecutionNode)execution.Data!).ToValue());
            Assert.Equal(equipmentId.ToString(), Assert.IsType<object[]>(actual["equipmentIds"])[0]);
            Assert.Equal(costumeId.ToString(), Assert.IsType<object[]>(actual["costumeIds"])[0]);
            Assert.Equal(foodId.ToString(), Assert.IsType<object[]>(actual["foodIds"])[0]);
            Assert.Equal(costumeId.ToString(), Assert.IsType<object[]>(actual["unresolvedCostumeIds"])[0]);
            Assert.Equal(foodId.ToString(), Assert.IsType<object[]>(actual["unresolvedFoodIds"])[0]);
            var runeSlot = Assert.IsType<Dictionary<string, object?>>(
                Assert.IsType<object[]>(actual["runeSlotInfos"])[0]);
            Assert.Equal(2, runeSlot["slotIndex"]);
            Assert.Equal(1234, runeSlot["runeId"]);
            Assert.Equal(42, Assert.IsType<object[]>(actual["collectionIds"])[0]);
            var modifier = Assert.IsType<Dictionary<string, object?>>(
                Assert.IsType<object[]>(actual["collectionModifiers"])[0]);
            Assert.Equal("ATK", modifier["statType"]);
            Assert.Equal("Add", modifier["operation"]);
            Assert.Equal(7L, modifier["value"]);
            Assert.Null(actual["stageBuffId"]);
        }

        [Fact]
        public async Task ResultTypeProjectsActualLoadoutRuneAndCollectionRecords()
        {
            var equipmentSheet = new EquipmentItemSheet();
            equipmentSheet.Set(_sheets[nameof(EquipmentItemSheet)]);
            var costumeSheet = new CostumeItemSheet();
            costumeSheet.Set(_sheets[nameof(CostumeItemSheet)]);
            var consumableSheet = new ConsumableItemSheet();
            consumableSheet.Set(_sheets[nameof(ConsumableItemSheet)]);
            var collectionSheet = new CollectionSheet();
            collectionSheet.Set(_sheets[nameof(CollectionSheet)]);

            var equipment = Assert.IsAssignableFrom<Equipment>(ItemFactory.CreateItemUsable(
                equipmentSheet.OrderedList.First(),
                Guid.NewGuid(),
                0));
            var costume = ItemFactory.CreateCostume(
                costumeSheet.OrderedList.First(),
                Guid.NewGuid());
            var food = Assert.IsAssignableFrom<Consumable>(ItemFactory.CreateItemUsable(
                consumableSheet.OrderedList.First(),
                Guid.NewGuid(),
                0));
            var rune = new RuneState(1234, 7);
            var collection = collectionSheet.OrderedList.First();
            var result = new HackAndSlashFirstClearResult
            {
                IsFirstClear = true,
                AvatarAddress = new PrivateKey().Address,
                Equipments = new[] { equipment },
                Costumes = new[] { costume },
                Foods = new[] { food },
                Runes = new[] { rune },
                Collections = new[] { collection },
                CollectionModifiers = collection.StatModifiers.ToArray(),
            };

            var execution = await GraphQLTestUtils.ExecuteQueryAsync<HackAndSlashFirstClearResultType>(
                @"{
                    isFirstClear
                    equipmentCount costumeCount foodCount runeCount collectionCount collectionModifierCount
                    equipments { itemId id level grade itemType itemSubType elementalType setId equipped }
                    costumes { itemId id grade itemType itemSubType elementalType equipped }
                    foods { itemId id grade itemType itemSubType elementalType mainStat }
                    runes { runeId level }
                    collections {
                        id
                        materials { itemId count level skillContains }
                        statModifiers { statType operation value }
                    }
                }",
                source: result);

            Assert.Null(execution.Errors);
            var actual = Assert.IsType<Dictionary<string, object?>>(((ExecutionNode)execution.Data!).ToValue());
            Assert.True(Assert.IsType<bool>(actual["isFirstClear"]));
            Assert.Equal(1, actual["equipmentCount"]);
            Assert.Equal(1, actual["costumeCount"]);
            Assert.Equal(1, actual["foodCount"]);
            Assert.Equal(1, actual["runeCount"]);
            Assert.Equal(1, actual["collectionCount"]);
            Assert.Equal(collection.StatModifiers.Count, actual["collectionModifierCount"]);

            var actualEquipment = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(actual["equipments"])));
            Assert.Equal(equipment.ItemId.ToString(), actualEquipment["itemId"]);
            Assert.Equal(equipment.Id, actualEquipment["id"]);
            Assert.Equal(equipment.level, actualEquipment["level"]);

            var actualCostume = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(actual["costumes"])));
            Assert.Equal(costume.ItemId.ToString(), actualCostume["itemId"]);
            Assert.Equal(costume.Id, actualCostume["id"]);

            var actualFood = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(actual["foods"])));
            Assert.Equal(food.ItemId.ToString(), actualFood["itemId"]);
            Assert.Equal(food.Id, actualFood["id"]);

            var actualRune = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(actual["runes"])));
            Assert.Equal(rune.RuneId, actualRune["runeId"]);
            Assert.Equal(rune.Level, actualRune["level"]);

            var actualCollection = Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<object[]>(actual["collections"])));
            Assert.Equal(collection.Id, actualCollection["id"]);
            Assert.Equal(collection.Materials.Count, Assert.IsType<object[]>(actualCollection["materials"]).Length);
            Assert.Equal(collection.StatModifiers.Count, Assert.IsType<object[]>(actualCollection["statModifiers"]).Length);
        }

        [Fact]
        public async Task GraphQlSchemaExposesExpectedResultFieldsOnly()
        {
            var result = await ExecuteRawAsync(@"{
                __schema {
                    types { name fields { name type { kind name } } }
                }
            }");
            Assert.Null(result.Errors);

            var data = (Dictionary<string, object?>)((ExecutionNode)result.Data!).ToValue()!;
            var schema = Assert.IsType<Dictionary<string, object?>>(data["__schema"]);
            var types = Assert.IsType<object[]>(schema["types"])
                .Select(type => Assert.IsType<Dictionary<string, object?>>(type))
                .ToArray();
            var type = Assert.Single(types.Where(type =>
                type["name"] is string name &&
                name.Contains("HackAndSlashFirstClearResult", StringComparison.Ordinal)));
            var fieldNodes = Assert.IsType<object[]>(type["fields"])
                .Select(field => Assert.IsType<Dictionary<string, object?>>(field))
                .ToArray();
            var fields = fieldNodes
                .Select(field => field["name"])
                .Cast<string>()
                .ToHashSet();

            Assert.Contains("isFirstClear", fields);
            Assert.Contains("txId", fields);
            Assert.Contains("blockIndex", fields);
            Assert.Contains("blockTimestamp", fields);
            Assert.Contains("blockHash", fields);
            Assert.Contains("worldId", fields);
            Assert.Contains("stageId", fields);
            Assert.Contains("stageBuffId", fields);
            var stageBuffField = Assert.Single(fieldNodes.Where(field => field["name"] as string == "stageBuffId"));
            var stageBuffType = Assert.IsType<Dictionary<string, object?>>(stageBuffField["type"]);
            Assert.Equal("SCALAR", stageBuffType["kind"]);
            Assert.Equal("Int", stageBuffType["name"]);
            Assert.Contains("totalPlayCount", fields);
            Assert.Contains("avatarAddress", fields);
            Assert.Contains("equipmentIds", fields);
            Assert.Contains("costumeIds", fields);
            Assert.Contains("foodIds", fields);
            Assert.Contains("unresolvedCostumeIds", fields);
            Assert.Contains("unresolvedFoodIds", fields);
            Assert.Contains("runeSlotInfos", fields);
            Assert.Contains("collectionIds", fields);
            Assert.Contains("collectionModifiers", fields);
            Assert.Contains("equipmentCount", fields);
            Assert.Contains("costumeCount", fields);
            Assert.Contains("foodCount", fields);
            Assert.Contains("runeCount", fields);
            Assert.Contains("collectionCount", fields);
            Assert.Contains("collectionModifierCount", fields);
            Assert.Contains("equipments", fields);
            Assert.Contains("costumes", fields);
            Assert.Contains("foods", fields);
            Assert.Contains("runes", fields);
            Assert.Contains("collections", fields);
            Assert.Contains("historicalLoadout", fields);
            Assert.Contains("avatar", fields);
            Assert.DoesNotContain("inputWorldState", fields);
            Assert.DoesNotContain("inputAvatarState", fields);
        }

        private Transaction Append(IReadOnlyList<ActionBase> actions, PrivateKey key)
        {
            var tx = _blockChain.MakeTransaction(key, actions);
            var block = _blockChain.ProposeBlock(_proposer, _blockChain.GetBlockCommit(_blockChain.Tip.Index));
            _blockChain.Append(block, GenerateBlockCommit(block.Index, block.Hash, _proposer));
            return tx;
        }


        private Task AssertNullAsync(string txId) => QueryAsync(txId).ContinueWith(task =>
        {
            Assert.Null(task.Result.Errors);
            var data = (Dictionary<string, object?>)((ExecutionNode)task.Result.Data!).ToValue()!;
            Assert.Null(data["hackAndSlashFirstClear"]);
        });

        private Task<GraphQL.ExecutionResult> QueryAsync(string txId)
        {
            var query = @$"{{
                hackAndSlashFirstClear(txId: ""{txId}"") {{
                    isFirstClear
                    txId
                    blockIndex
                    blockTimestamp
                    blockHash
                    worldId
                    stageId
                    stageBuffId
                    totalPlayCount
                    avatarAddress
                    equipmentIds
                    costumeIds
                    foodIds
                    unresolvedCostumeIds
                    unresolvedFoodIds
                    runeSlotInfos {{ slotIndex runeId }}
                    collectionIds
                    collectionModifiers {{ statType operation value }}
                    equipmentCount
                    costumeCount
                    foodCount
                    runeCount
                    collectionCount
                    collectionModifierCount
                    equipments {{ itemId id level grade itemType itemSubType elementalType setId equipped }}
                    costumes {{ itemId id grade itemType itemSubType elementalType equipped }}
                    foods {{ itemId id grade itemType itemSubType elementalType mainStat }}
                    runes {{ runeId level }}
                    collections {{
                        id
                        materials {{ itemId count level skillContains }}
                        statModifiers {{ statType operation value }}
                    }}
                    avatar {{ level exp actionPoint worldInformation {{ isStageCleared(stageId: 1) }} }}
                }}
            }}";
            return ExecuteRawAsync(query);
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

        private BlockCommit? GenerateBlockCommit(long height, BlockHash hash, PrivateKey validator)
        {
            return height != 0
                ? new BlockCommit(
                    height,
                    0,
                    hash,
                    ImmutableArray<Vote>.Empty
                        .Add(new VoteMetadata(
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
