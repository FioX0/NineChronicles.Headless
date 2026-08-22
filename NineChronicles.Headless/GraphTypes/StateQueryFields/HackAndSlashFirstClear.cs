#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Action.State;
using Libplanet.Blockchain;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Explorer.GraphTypes;
using Libplanet.Store;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using NineChronicles.Headless.GraphTypes.States;
using static NineChronicles.Headless.NCActionUtils;

namespace NineChronicles.Headless.GraphTypes
{
    public partial class StateQuery
    {
        private void RegisterHackAndSlashFirstClearField()
        {
            Field<HackAndSlashFirstClearResultType>(
                name: "hackAndSlashFirstClear",
                description: "Returns first-clear status for a successful HackAndSlash transaction and pre-execution context when it is a first clear.",
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
                    return ResolveHackAndSlashFirstClear(_standaloneContext, context.Source, txId);
                });
        }

        internal static HackAndSlashFirstClearResult? ResolveHackAndSlashFirstClear(
            StandaloneContext standaloneContext,
            StateContext stateContext,
            TxId txId)
        {
            if (standaloneContext.BlockChain is not BlockChain blockChain)
            {
                throw new ExecutionError($"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)} was not set yet!");
            }

            if (standaloneContext.Store is not IStore store)
            {
                throw new ExecutionError($"{nameof(StandaloneContext)}.{nameof(StandaloneContext.Store)} was not set yet!");
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
                throw new ExecutionError($"Canonical block {canonicalBlockHash} does not contain transaction {txId}.");
            }

            if (tx.Actions.Count != 1)
            {
                return null;
            }

            if (!TryGetHackAndSlash(tx.Actions[0], txId, block.Hash, out var hackAndSlash))
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
            var inputAvatar = LoadExecutionState(
                () => inputWorldState.GetAvatarState(hackAndSlash.AvatarAddress),
                $"pre-execution avatar {hackAndSlash.AvatarAddress}",
                txId,
                block.Hash);
            var outputAvatar = LoadExecutionState(
                () => outputWorldState.GetAvatarState(hackAndSlash.AvatarAddress),
                $"post-execution avatar {hackAndSlash.AvatarAddress}",
                txId,
                block.Hash);

            if (!IsFirstClearTransition(inputAvatar, outputAvatar, hackAndSlash.StageId))
            {
                return new HackAndSlashFirstClearResult
                {
                    IsFirstClear = false,
                    TxId = txId.ToString(),
                    BlockIndex = block.Index,
                    BlockTimestamp = block.Timestamp,
                    BlockHash = block.Hash.ToString(),
                    WorldId = hackAndSlash.WorldId,
                    StageId = hackAndSlash.StageId,
                    StageBuffId = hackAndSlash.StageBuffId,
                    TotalPlayCount = hackAndSlash.TotalPlayCount,
                    AvatarAddress = hackAndSlash.AvatarAddress,
                };
            }

            var equipments = ResolveSelectedItems(
                inputAvatar.inventory.Items
                    .Where(item => !item.Locked)
                    .Select(item => item.item)
                    .OfType<Equipment>(),
                hackAndSlash.Equipments,
                equipment => equipment.ItemId,
                "equipment",
                txId,
                block.Hash);
            // ValidateCostumeV2 skips locked, absent, and wrong-type IDs.  Preserve those
            // declarations separately so the historical payload cannot silently lose them.
            var costumeResolution = ResolveOptionalSelectedItems(
                inputAvatar.inventory.Items
                    .Where(item => !item.Locked)
                    .Select(item => item.item)
                    .OfType<Costume>(),
                hackAndSlash.Costumes,
                costume => costume.ItemId,
                "costume",
                txId,
                block.Hash);
            // StageSimulator.Player.Use resolves consumables from the raw GUID list, including
            // a locked consumable; retain every typed pre-execution consumable selection here.
            var foodResolution = ResolveOptionalSelectedItems(
                inputAvatar.inventory.Consumables,
                hackAndSlash.Foods,
                food => food.ItemId,
                "food",
                txId,
                block.Hash);

            var allRuneState = LoadExecutionState(
                () => inputWorldState.GetRuneState(hackAndSlash.AvatarAddress, out _),
                $"pre-execution rune state for avatar {hackAndSlash.AvatarAddress}",
                txId,
                block.Hash);
            var runes = ResolveRunes(
                allRuneState,
                hackAndSlash.RuneInfos,
                txId,
                block.Hash);

            var collectionState = LoadExecutionState(
                () => inputWorldState.GetCollectionStates(new[] { hackAndSlash.AvatarAddress })
                    .GetValueOrDefault(hackAndSlash.AvatarAddress),
                $"pre-execution collection state for avatar {hackAndSlash.AvatarAddress}",
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

            return new HackAndSlashFirstClearResult
            {
                IsFirstClear = true,
                TxId = txId.ToString(),
                BlockIndex = block.Index,
                BlockTimestamp = block.Timestamp,
                BlockHash = block.Hash.ToString(),
                WorldId = hackAndSlash.WorldId,
                StageId = hackAndSlash.StageId,
                StageBuffId = hackAndSlash.StageBuffId,
                TotalPlayCount = hackAndSlash.TotalPlayCount,
                AvatarAddress = hackAndSlash.AvatarAddress,
                EquipmentIds = hackAndSlash.Equipments.ToList(),
                CostumeIds = hackAndSlash.Costumes.ToList(),
                FoodIds = hackAndSlash.Foods.ToList(),
                UnresolvedCostumeIds = costumeResolution.UnresolvedIds,
                UnresolvedFoodIds = foodResolution.UnresolvedIds,
                RuneSlotInfos = hackAndSlash.RuneInfos.ToList(),
                CollectionIds = collectionDetails.Ids,
                CollectionModifiers = collectionDetails.Modifiers,
                Equipments = equipments,
                Costumes = costumeResolution.Items,
                Foods = foodResolution.Items,
                Runes = runes,
                Collections = collectionDetails.Collections,
                HistoricalLoadoutLoader = new Lazy<HackAndSlashHistoricalLoadout?>(
                    () => LoadExecutionState(
                        () => HackAndSlashHistoricalLoadout.Builder.Build(
                            inputWorldState,
                            outputWorldState,
                            inputAvatar,
                            hackAndSlash.AvatarAddress,
                            equipments,
                            costumeResolution.Items,
                            foodResolution.Items,
                            runes,
                            hackAndSlash.RuneInfos.ToList(),
                            allRuneState,
                            collectionDetails.Ids,
                            collectionDetails.Modifiers),
                        "historical first-clear display data",
                        txId,
                        block.Hash)),
                Avatar = new AvatarStateType.AvatarStateContext(
                    inputAvatar,
                    inputWorldState,
                    block.Index,
                    stateContext.StateMemoryCache),
            };
        }

        internal static T LoadExecutionState<T>(
            Func<T> load,
            string stateDescription,
            TxId txId,
            BlockHash blockHash)
        {
            try
            {
                return load();
            }
            catch (ExecutionError)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ExecutionError(
                    $"Failed to load {stateDescription} for transaction {txId} in block {blockHash}.",
                    e);
            }
        }

        internal static IReadOnlyList<T> ResolveSelectedItems<T>(
            IEnumerable<T> inventoryItems,
            IEnumerable<Guid> selectedIds,
            Func<T, Guid> getItemId,
            string itemKind,
            TxId txId,
            BlockHash blockHash)
        {
            var resolution = ResolveOptionalSelectedItems(
                inventoryItems,
                selectedIds,
                getItemId,
                itemKind,
                txId,
                blockHash);
            if (resolution.UnresolvedIds.Count > 0)
            {
                throw new ExecutionError(
                    $"Selected {itemKind} {resolution.UnresolvedIds[0]} for transaction {txId} in block {blockHash} was not found in the pre-execution inventory.");
            }

            return resolution.Items;
        }

        internal static SelectedItemResolution<T> ResolveOptionalSelectedItems<T>(
            IEnumerable<T> inventoryItems,
            IEnumerable<Guid> selectedIds,
            Func<T, Guid> getItemId,
            string itemKind,
            TxId txId,
            BlockHash blockHash)
        {
            var itemsById = LoadExecutionState(
                () => inventoryItems.ToDictionary(getItemId),
                $"pre-execution {itemKind} inventory",
                txId,
                blockHash);
            var selectedItems = new List<T>();
            var unresolvedIds = new List<Guid>();
            foreach (var selectedId in selectedIds)
            {
                if (itemsById.TryGetValue(selectedId, out var item))
                {
                    selectedItems.Add(item);
                }
                else
                {
                    unresolvedIds.Add(selectedId);
                }
            }

            return new SelectedItemResolution<T>(selectedItems, unresolvedIds);
        }

        internal sealed class SelectedItemResolution<T>
        {
            public SelectedItemResolution(IReadOnlyList<T> items, IReadOnlyList<Guid> unresolvedIds)
            {
                Items = items;
                UnresolvedIds = unresolvedIds;
            }

            public IReadOnlyList<T> Items { get; }
            public IReadOnlyList<Guid> UnresolvedIds { get; }
        }

        internal static IReadOnlyList<RuneState> ResolveRunes(
            AllRuneState allRuneState,
            IEnumerable<RuneSlotInfo> runeSlotInfos,
            TxId txId,
            BlockHash blockHash)
        {
            var runes = new List<RuneState>();
            foreach (var runeId in runeSlotInfos.Select(runeSlotInfo => runeSlotInfo.RuneId))
            {
                if (!allRuneState.TryGetRuneState(runeId, out var runeState))
                {
                    throw new ExecutionError(
                        $"Selected rune {runeId} for transaction {txId} in block {blockHash} was not found in the pre-execution rune state.");
                }

                runes.Add(runeState);
            }

            return runes;
        }

        internal static (
            IReadOnlyList<int> Ids,
            IReadOnlyList<CollectionSheet.Row> Collections,
            IReadOnlyList<Nekoyume.Model.Stat.StatModifier> Modifiers) ResolveCollections(
                CollectionState? collectionState,
                CollectionSheet? collectionSheet,
                TxId txId,
                BlockHash blockHash)
        {
            if (collectionState is null || collectionState.Ids.Count == 0)
            {
                return (
                    Array.Empty<int>(),
                    Array.Empty<CollectionSheet.Row>(),
                    Array.Empty<Nekoyume.Model.Stat.StatModifier>());
            }

            if (collectionSheet is null)
            {
                throw new ExecutionError(
                    $"The pre-execution collection sheet for transaction {txId} in block {blockHash} is unavailable.");
            }

            var ids = collectionState.Ids.ToArray();
            var collections = new List<CollectionSheet.Row>();
            var modifiers = new List<Nekoyume.Model.Stat.StatModifier>();
            foreach (var collectionId in ids)
            {
                if (!collectionSheet.TryGetValue(collectionId, out var collection))
                {
                    throw new ExecutionError(
                        $"Active collection {collectionId} for transaction {txId} in block {blockHash} was not found in the pre-execution collection sheet.");
                }

                collections.Add(collection);
                modifiers.AddRange(collection.StatModifiers);
            }

            return (ids, collections, modifiers);
        }

        internal static void ValidateExecutionRoots(
            HashDigest<SHA256>? inputState,
            HashDigest<SHA256>? outputState,
            TxId txId,
            BlockHash blockHash)
        {
            if (inputState is null || outputState is null)
            {
                throw new ExecutionError(
                    $"TxExecution {txId} in block {blockHash} is missing an input or output state root.");
            }
        }

        internal static bool IsFirstClearTransition(
            AvatarState inputAvatar,
            AvatarState outputAvatar,
            int stageId)
        {
            return !inputAvatar.worldInformation.IsStageCleared(stageId) &&
                outputAvatar.worldInformation.IsStageCleared(stageId);
        }

        private static readonly IValue CurrentHackAndSlashTypeIdentifier =
            typeof(HackAndSlash).GetCustomAttribute<ActionTypeAttribute>()!.TypeIdentifier;

        internal static bool TryGetHackAndSlash(
            IValue actionValue,
            TxId txId,
            BlockHash blockHash,
            out HackAndSlash hackAndSlash)
        {
            try
            {
                var action = ToAction(actionValue);
                if (action is HackAndSlash { } matchedHackAndSlash)
                {
                    hackAndSlash = matchedHackAndSlash;
                    return true;
                }
            }
            catch (InvalidActionException e) when (IsCurrentHackAndSlashPayload(actionValue))
            {
                throw new ExecutionError(
                    $"Failed to deserialize HackAndSlash action for transaction {txId} in canonical block {blockHash}.",
                    e);
            }
            catch (InvalidActionException)
            {
                hackAndSlash = null!;
                return false;
            }

            hackAndSlash = null!;
            return false;
        }

        private static bool IsCurrentHackAndSlashPayload(IValue actionValue)
        {
            return actionValue is Dictionary action &&
                action.TryGetValue((Text)"type_id", out var payloadTypeIdentifier) &&
                payloadTypeIdentifier.Equals(CurrentHackAndSlashTypeIdentifier);
        }
    }
}
