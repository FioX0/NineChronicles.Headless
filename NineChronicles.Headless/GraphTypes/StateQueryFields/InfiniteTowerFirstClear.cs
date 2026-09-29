#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Blockchain;
using Libplanet.Explorer.GraphTypes;
using Libplanet.Store;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using NineChronicles.Headless.GraphTypes.States;
using static NineChronicles.Headless.NCActionUtils;
using InfiniteTowerFirstClearCondition = NineChronicles.Headless.GraphTypes.States.InfiniteTowerFirstClearRules.Condition;
using InfiniteTowerFirstClearConditionCandidate = NineChronicles.Headless.GraphTypes.States.InfiniteTowerFirstClearRules.ConditionCandidate;
using InfiniteTowerFirstClearMonster = NineChronicles.Headless.GraphTypes.States.InfiniteTowerFirstClearRules.Monster;
using InfiniteTowerFirstClearRuleSet = NineChronicles.Headless.GraphTypes.States.InfiniteTowerFirstClearRules.RuleSet;
using InfiniteTowerFirstClearSeason = NineChronicles.Headless.GraphTypes.States.InfiniteTowerFirstClearRules.Season;
using InfiniteTowerFirstClearStatModifier = NineChronicles.Headless.GraphTypes.States.InfiniteTowerFirstClearRules.StatModifier;
using InfiniteTowerFirstClearWave = NineChronicles.Headless.GraphTypes.States.InfiniteTowerFirstClearRules.Wave;

namespace NineChronicles.Headless.GraphTypes
{
    public partial class StateQuery
    {
        // InfiniteTowerBattle supplies ShatterStrikeMaxDamage but omits the optional
        // InfiniteTowerSimulator turnLimit argument, so current action version uses 150.
        private const int CurrentInfiniteTowerTurnLimit = 150;

        private void RegisterInfiniteTowerFirstClearField()
        {
            Field<InfiniteTowerFirstClearResultType>(
                name: "infiniteTowerFirstClear",
                description: "Returns first-clear status for a successful InfiniteTowerBattle transaction.",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<TxIdType>>
                {
                    Name = "txId",
                    Description = "Canonical transaction ID."
                }),
                resolve: context =>
                {
                    if (_standaloneContext is null)
                    {
                        throw new ExecutionError($"{nameof(StandaloneContext)} was not set yet!");
                    }

                    var txId = context.GetArgument<TxId>("txId");
                    return ResolveInfiniteTowerFirstClear(
                        _standaloneContext,
                        context.Source,
                        txId);
                });
        }

        internal static InfiniteTowerFirstClearResult? ResolveInfiniteTowerFirstClear(
            StandaloneContext standaloneContext,
            StateContext stateContext,
            TxId txId)
        {
            if (standaloneContext.BlockChain is not BlockChain blockChain)
            {
                throw new ExecutionError(
                    $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)} was not set yet!");
            }

            if (standaloneContext.Store is not IStore store)
            {
                throw new ExecutionError(
                    $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.Store)} was not set yet!");
            }

            var blockHash = store
                .IterateTxIdBlockHashIndex(txId)
                .Where(blockChain.ContainsBlock)
                .Cast<BlockHash?>()
                .FirstOrDefault();
            if (blockHash is not { } canonicalBlockHash)
            {
                return null;
            }

            var block = blockChain[canonicalBlockHash];
            var tx = block.Transactions.FirstOrDefault(transaction => transaction.Id.Equals(txId));
            if (tx is null)
            {
                throw new ExecutionError(
                    $"Canonical block {canonicalBlockHash} does not contain transaction {txId}.");
            }

            if (tx.Actions.Count != 1 ||
                !TryGetInfiniteTowerBattle(tx.Actions[0], txId, block.Hash, out var action))
            {
                return null;
            }

            var execution = blockChain.GetTxExecution(block.Hash, txId);
            if (execution is null)
            {
                throw new ExecutionError($"TxExecution {txId} in block {block.Hash} was not found.");
            }

            if (execution.Fail)
            {
                return null;
            }

            ValidateExecutionRoots(execution.InputState, execution.OutputState, txId, block.Hash);
            var inputStateRoot = execution.InputState!.Value;
            var outputStateRoot = execution.OutputState!.Value;
            var inputWorldState = LoadExecutionState(
                () => blockChain.GetWorldState(inputStateRoot),
                "pre-execution world state",
                txId,
                block.Hash);
            var outputWorldState = LoadExecutionState(
                () => blockChain.GetWorldState(outputStateRoot),
                "post-execution world state",
                txId,
                block.Hash);
            var scheduleSheet = LoadExecutionState(
                () => inputWorldState.GetSheet<InfiniteTowerScheduleSheet>(),
                "pre-execution Infinite Tower schedule sheet",
                txId,
                block.Hash);
            var scheduleRow = scheduleSheet.Values
                .FirstOrDefault(row => row.InfiniteTowerId == action.InfiniteTowerId)
                ?? throw new ExecutionError(
                    $"Infinite Tower {action.InfiniteTowerId} schedule for transaction {txId} in block {block.Hash} was not found.");
            var inputInfo = LoadExecutionState(
                () => GetInfiniteTowerInfoForStateQuery(
                    inputWorldState,
                    action.AvatarAddress,
                    action.InfiniteTowerId),
                "pre-execution Infinite Tower progress",
                txId,
                block.Hash);
            var outputInfo = LoadExecutionState(
                () => GetInfiniteTowerInfoForStateQuery(
                    outputWorldState,
                    action.AvatarAddress,
                    action.InfiniteTowerId),
                "post-execution Infinite Tower progress",
                txId,
                block.Hash);

            if (!scheduleRow.IsActive(block.Index))
            {
                throw new ExecutionError(
                    $"Infinite Tower {action.InfiniteTowerId} schedule is not active for transaction {txId} in block {block.Hash}.");
            }

            var isFirstClear = IsInfiniteTowerFirstClearTransition(
                inputInfo,
                outputInfo,
                scheduleRow,
                action.FloorId);
            var randomSeed = ActionEvaluator.GenerateRandomSeed(
                block.PreEvaluationHash.ToByteArray(),
                tx.Signature,
                0);
            InfiniteTowerFirstClearRuleSet? rules = null;
            string? configuredRulesFingerprint = null;
            string? appliedRulesFingerprint = null;
            IReadOnlyList<Equipment>? equipments = null;
            IReadOnlyList<Costume>? costumes = null;
            IReadOnlyList<Consumable>? foods = null;
            IReadOnlyList<RuneState>? runes = null;
            IReadOnlyList<CollectionSheet.Row>? collections = null;
            IReadOnlyList<Guid> unresolvedCostumeIds = Array.Empty<Guid>();
            IReadOnlyList<Guid> unresolvedFoodIds = Array.Empty<Guid>();
            IReadOnlyList<int> collectionIds = Array.Empty<int>();
            IReadOnlyList<Nekoyume.Model.Stat.StatModifier> collectionModifiers =
                Array.Empty<Nekoyume.Model.Stat.StatModifier>();
            AvatarStateType.AvatarStateContext? avatar = null;
            Lazy<InfiniteTowerHistoricalLoadout?>? historicalLoadoutLoader = null;
            long? recordedInfiniteTowerCp = null;
            if (isFirstClear)
            {
                rules = LoadExecutionState(
                    () => BuildInfiniteTowerRuleSet(inputWorldState, action.FloorId, randomSeed),
                    $"pre-execution Infinite Tower rules for floor {action.FloorId}",
                    txId,
                    block.Hash);
                configuredRulesFingerprint = ComputeRulesFingerprint(
                    action.FloorId,
                    rules,
                    includeAppliedConditions: false);
                appliedRulesFingerprint = ComputeRulesFingerprint(
                    action.FloorId,
                    rules,
                    includeAppliedConditions: true);

                var inputAvatar = LoadExecutionState(
                    () => inputWorldState.GetAvatarState(action.AvatarAddress),
                    $"pre-execution avatar {action.AvatarAddress}",
                    txId,
                    block.Hash);
                equipments = ResolveSelectedItems(
                    inputAvatar.inventory.Items
                        .Where(item => !item.Locked)
                        .Select(item => item.item)
                        .OfType<Equipment>(),
                    action.Equipments,
                    equipment => equipment.ItemId,
                    "equipment",
                    txId,
                    block.Hash);
                var costumeResolution = ResolveOptionalSelectedItems(
                    inputAvatar.inventory.Items
                        .Where(item => !item.Locked)
                        .Select(item => item.item)
                        .OfType<Costume>(),
                    action.Costumes,
                    costume => costume.ItemId,
                    "costume",
                    txId,
                    block.Hash);
                costumes = costumeResolution.Items;
                unresolvedCostumeIds = costumeResolution.UnresolvedIds;
                var foodResolution = ResolveOptionalSelectedItems(
                    inputAvatar.inventory.Consumables,
                    action.Foods,
                    food => food.ItemId,
                    "food",
                    txId,
                    block.Hash);
                foods = foodResolution.Items;
                unresolvedFoodIds = foodResolution.UnresolvedIds;

                var allRuneState = LoadExecutionState(
                    () => inputWorldState.GetRuneState(action.AvatarAddress, out _),
                    $"pre-execution rune state for avatar {action.AvatarAddress}",
                    txId,
                    block.Hash);
                runes = ResolveRunes(allRuneState, action.RuneInfos, txId, block.Hash);

                var collectionState = LoadExecutionState(
                    () => inputWorldState.GetCollectionStates(new[] { action.AvatarAddress })
                        .GetValueOrDefault(action.AvatarAddress),
                    $"pre-execution collection state for avatar {action.AvatarAddress}",
                    txId,
                    block.Hash);
                var collectionSheet = collectionState is not null && collectionState.Ids.Count > 0
                    ? LoadExecutionState(
                        () => inputWorldState.GetSheet<CollectionSheet>(),
                        "pre-execution collection sheet",
                        txId,
                        block.Hash)
                    : null;
                var collectionDetails = ResolveCollections(
                    collectionState,
                    collectionSheet,
                    txId,
                    block.Hash);
                collectionIds = collectionDetails.Ids;
                collections = collectionDetails.Collections;
                collectionModifiers = collectionDetails.Modifiers;
                var towerCp = LoadExecutionState(
                    () => new CpState(outputWorldState
                        .GetAccountState(Addresses.GetCpAccountAddress(BattleType.InfiniteTower))
                        .GetState(action.AvatarAddress)).Cp,
                    $"recorded Infinite Tower CP for avatar {action.AvatarAddress}",
                    txId,
                    block.Hash);
                recordedInfiniteTowerCp = towerCp;
                historicalLoadoutLoader = new Lazy<InfiniteTowerHistoricalLoadout?>(
                    () => LoadExecutionState(
                        () => InfiniteTowerHistoricalLoadout.Builder.Build(
                            inputWorldState,
                            inputAvatar,
                            equipments,
                            costumes,
                            foods,
                            runes,
                            action.RuneInfos.ToArray(),
                            allRuneState,
                            collectionIds,
                            collectionModifiers,
                            towerCp),
                        "historical Infinite Tower first-clear display data",
                        txId,
                        block.Hash));
                avatar = new AvatarStateType.AvatarStateContext(
                    inputAvatar,
                    inputWorldState,
                    block.Index,
                    stateContext.StateMemoryCache);
            }

            return new InfiniteTowerFirstClearResult
            {
                IsFirstClear = isFirstClear,
                TxId = txId.ToString(),
                BlockIndex = block.Index,
                BlockTimestamp = block.Timestamp,
                BlockHash = block.Hash.ToString(),
                InputStateRoot = inputStateRoot.ToString(),
                AvatarAddress = action.AvatarAddress,
                InfiniteTowerId = action.InfiniteTowerId,
                ScheduleId = scheduleRow.Id,
                FloorId = action.FloorId,
                BuyTicketIfNeeded = action.BuyTicketIfNeeded,
                UseNcgForTicket = action.UseNcgForTicket,
                RandomSeed = randomSeed,
                ConfiguredRulesFingerprint = configuredRulesFingerprint,
                AppliedRulesFingerprint = appliedRulesFingerprint,
                Season = new InfiniteTowerFirstClearSeason
                {
                    ScheduleId = scheduleRow.Id,
                    InfiniteTowerId = scheduleRow.InfiniteTowerId,
                    StartBlockIndex = scheduleRow.StartBlockIndex,
                    EndBlockIndex = scheduleRow.EndBlockIndex,
                    DailyFreeTickets = scheduleRow.DailyFreeTickets,
                    MaxTickets = scheduleRow.MaxTickets,
                    ResetIntervalBlocks = scheduleRow.ResetIntervalBlocks,
                    FloorBegin = scheduleRow.FloorBegin,
                    FloorEnd = scheduleRow.FloorEnd,
                },
                Rules = rules,
                EquipmentIds = isFirstClear ? action.Equipments.ToArray() : Array.Empty<Guid>(),
                CostumeIds = isFirstClear ? action.Costumes.ToArray() : Array.Empty<Guid>(),
                FoodIds = isFirstClear ? action.Foods.ToArray() : Array.Empty<Guid>(),
                UnresolvedCostumeIds = unresolvedCostumeIds,
                UnresolvedFoodIds = unresolvedFoodIds,
                RuneSlotInfos = isFirstClear ? action.RuneInfos.ToArray() : Array.Empty<RuneSlotInfo>(),
                CollectionIds = collectionIds,
                CollectionModifiers = collectionModifiers,
                Equipments = equipments,
                Costumes = costumes,
                Foods = foods,
                Runes = runes,
                Collections = collections,
                HistoricalLoadoutLoader = historicalLoadoutLoader,
                RecordedInfiniteTowerCp = recordedInfiniteTowerCp,
                Avatar = avatar,
            };
        }

        internal static InfiniteTowerFirstClearRuleSet BuildInfiniteTowerRuleSet(
            Libplanet.Action.State.IWorldState inputWorldState,
            int floorId,
            int randomSeed)
        {
            var floorSheet = inputWorldState.GetSheet<InfiniteTowerFloorSheet>();
            if (!floorSheet.TryGetValue(floorId, out var floorRow))
            {
                throw new InvalidOperationException(
                    $"Infinite Tower floor {floorId} was not found.");
            }

            var conditionSheet = inputWorldState.GetSheet<InfiniteTowerConditionSheet>();
            InfiniteTowerConditionSheet.Row? guaranteedRow = null;
            if (floorRow.GuaranteedConditionId > 0 &&
                !conditionSheet.TryGetValue(floorRow.GuaranteedConditionId, out guaranteedRow))
            {
                throw new InvalidOperationException(
                    $"Guaranteed Infinite Tower condition {floorRow.GuaranteedConditionId} was not found.");
            }

            var weightedDefinitions = floorRow.GetRandomConditionsWithWeights();
            var candidates = new List<InfiniteTowerFirstClearConditionCandidate>();
            List<InfiniteTowerCondition> selectedRandomConditions;
            var random = new LocalRandom(randomSeed);
            string selectionMode;
            if (weightedDefinitions.Count > 0)
            {
                selectionMode = "WEIGHTED";
                foreach (var (conditionId, weight) in weightedDefinitions)
                {
                    if (conditionId != guaranteedRow?.Id &&
                        conditionSheet.TryGetValue(conditionId, out var conditionRow))
                    {
                        candidates.Add(new InfiniteTowerFirstClearConditionCandidate
                        {
                            Weight = weight,
                            Condition = ProjectCondition(conditionRow),
                        });
                    }
                }

                selectedRandomConditions = floorRow.GetRandomConditionsWithWeights(
                    conditionSheet,
                    random,
                    guaranteedRow?.Id);
            }
            else
            {
                selectionMode = "UNIFORM_ALL";
                candidates.AddRange(conditionSheet.Values
                    .Where(row => row.Id != guaranteedRow?.Id)
                    .Select(row => new InfiniteTowerFirstClearConditionCandidate
                    {
                        Weight = null,
                        Condition = ProjectCondition(row),
                    }));
                selectedRandomConditions = floorRow.GetRandomConditions(
                    conditionSheet,
                    random,
                    guaranteedRow?.Id);
            }

            var appliedConditions = new List<InfiniteTowerFirstClearCondition>();
            if (guaranteedRow is not null)
            {
                appliedConditions.Add(ProjectCondition(guaranteedRow));
            }

            appliedConditions.AddRange(selectedRandomConditions.Select(ProjectCondition));

            var waveSheet = inputWorldState.GetSheet<InfiniteTowerFloorWaveSheet>();
            if (!waveSheet.TryGetValue(floorId, out var waveRow))
            {
                throw new InvalidOperationException(
                    $"Infinite Tower wave configuration for floor {floorId} was not found.");
            }

            return new InfiniteTowerFirstClearRuleSet
            {
                SelectionMode = selectionMode,
                ShatterStrikeMaxDamage =
                    (int)inputWorldState.GetGameConfigState().ShatterStrikeMaxDamage,
                TurnLimit = CurrentInfiniteTowerTurnLimit,
                RequiredCp = floorRow.RequiredCp,
                MaxCp = floorRow.MaxCp,
                MinItemGrade = floorRow.MinItemGrade,
                MaxItemGrade = floorRow.MaxItemGrade,
                MinItemLevel = floorRow.MinItemLevel,
                MaxItemLevel = floorRow.MaxItemLevel,
                ForbiddenItemSubTypes = floorRow.ForbiddenItemSubTypes
                    .Select(value => value.ToString()).ToArray(),
                ForbiddenRuneTypes = floorRow.ForbiddenRuneTypes
                    .Select(value => value.ToString()).ToArray(),
                RequiredElementalTypes = floorRow.RequiredElementalTypes
                    .Select(value => value.ToString()).ToArray(),
                MinRandomConditions = floorRow.MinRandomConditions,
                MaxRandomConditions = floorRow.MaxRandomConditions,
                GuaranteedCondition = guaranteedRow is null
                    ? null
                    : ProjectCondition(guaranteedRow),
                RandomConditionCandidates = candidates,
                AppliedConditions = appliedConditions,
                EnemyInitialStatModifiers = floorRow.EnemyInitialStatModifiers
                    .Select(modifier => new InfiniteTowerFirstClearStatModifier
                    {
                        StatType = modifier.StatType.ToString(),
                        Operation = modifier.Operation.ToString(),
                        Value = modifier.Value,
                    }).ToArray(),
                Waves = waveRow.Waves.Select(wave => new InfiniteTowerFirstClearWave
                {
                    Number = wave.Number,
                    HasBoss = wave.HasBoss,
                    Monsters = wave.Monsters.Select(monster => new InfiniteTowerFirstClearMonster
                    {
                        CharacterId = monster.CharacterId,
                        Level = monster.Level,
                        Count = monster.Count,
                    }).ToArray(),
                }).ToArray(),
            };
        }

        internal static string ComputeRulesFingerprint(
            int floorId,
            InfiniteTowerFirstClearRuleSet rules,
            bool includeAppliedConditions)
        {
            var canonicalRules = new
            {
                Version = 1,
                FloorId = floorId,
                rules.SelectionMode,
                rules.ShatterStrikeMaxDamage,
                rules.TurnLimit,
                rules.RequiredCp,
                rules.MaxCp,
                rules.MinItemGrade,
                rules.MaxItemGrade,
                rules.MinItemLevel,
                rules.MaxItemLevel,
                rules.ForbiddenItemSubTypes,
                rules.ForbiddenRuneTypes,
                rules.RequiredElementalTypes,
                rules.MinRandomConditions,
                rules.MaxRandomConditions,
                rules.GuaranteedCondition,
                rules.RandomConditionCandidates,
                AppliedConditions = includeAppliedConditions
                    ? rules.AppliedConditions
                    : Array.Empty<InfiniteTowerFirstClearCondition>(),
                rules.EnemyInitialStatModifiers,
                rules.Waves,
            };
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonicalRules));
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }

        private static InfiniteTowerFirstClearCondition ProjectCondition(
            InfiniteTowerConditionSheet.Row row)
        {
            return new InfiniteTowerFirstClearCondition
            {
                Id = row.Id,
                StatType = row.StatType.ToString(),
                TargetTypes = row.TargetType.Select(value => value.ToString()).ToArray(),
                Operation = row.OperationType.ToString(),
                Value = row.Value,
            };
        }

        private static InfiniteTowerFirstClearCondition ProjectCondition(
            InfiniteTowerCondition condition)
        {
            return new InfiniteTowerFirstClearCondition
            {
                Id = condition.Id,
                StatType = condition.StatType.ToString(),
                TargetTypes = condition.TargetType.Select(value => value.ToString()).ToArray(),
                Operation = condition.OperationType.ToString(),
                Value = condition.Value,
            };
        }

        internal static bool IsInfiniteTowerFirstClearTransition(
            InfiniteTowerInfo inputInfo,
            InfiniteTowerInfo outputInfo,
            InfiniteTowerScheduleSheet.Row scheduleRow,
            int floorId)
        {
            var wasClearedInSeason =
                inputInfo.LastResetBlockIndex >= scheduleRow.StartBlockIndex &&
                inputInfo.IsCleared(floorId);
            var isClearedInSeason =
                outputInfo.LastResetBlockIndex >= scheduleRow.StartBlockIndex &&
                outputInfo.IsCleared(floorId);
            return !wasClearedInSeason && isClearedInSeason;
        }

        private static readonly IValue CurrentInfiniteTowerBattleTypeIdentifier =
            typeof(InfiniteTowerBattle).GetCustomAttribute<ActionTypeAttribute>()!.TypeIdentifier;

        internal static bool TryGetInfiniteTowerBattle(
            IValue actionValue,
            TxId txId,
            BlockHash blockHash,
            out InfiniteTowerBattle infiniteTowerBattle)
        {
            try
            {
                if (ToAction(actionValue) is InfiniteTowerBattle matchedAction)
                {
                    infiniteTowerBattle = matchedAction;
                    return true;
                }
            }
            catch (InvalidActionException e) when (IsCurrentInfiniteTowerBattlePayload(actionValue))
            {
                throw new ExecutionError(
                    $"Failed to deserialize InfiniteTowerBattle action for transaction {txId} in canonical block {blockHash}.",
                    e);
            }
            catch (InvalidActionException)
            {
                infiniteTowerBattle = null!;
                return false;
            }

            infiniteTowerBattle = null!;
            return false;
        }

        private static bool IsCurrentInfiniteTowerBattlePayload(IValue actionValue)
        {
            return actionValue is Dictionary action &&
                action.TryGetValue((Text)"type_id", out var typeIdentifier) &&
                typeIdentifier.Equals(CurrentInfiniteTowerBattleTypeIdentifier);
        }
    }
}
