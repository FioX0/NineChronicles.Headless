#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Explorer.GraphTypes;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using NineChronicles.Headless.GraphTypes.States;
using Nekoyume.TableData.Pet;
using Nekoyume.Helper;
using Nekoyume.Model.Stat;
using Nekoyume.Module;
using Nekoyume.TableData.Rune;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.TableData.AdventureBoss;
using Nekoyume.Battle.AdventureBoss;
using System.Diagnostics;
using Serilog;
using Libplanet.Action.State;
using System.Text;
using Nekoyume.Model.BattleStatus.Arena;

namespace NineChronicles.Headless.GraphTypes
{
    public class SimultionQuery : ObjectGraphType<StateContext>
    {
        public SimultionQuery()
        {
            Name = "SimultionQuery";
            
            Field<StageResultInfoStateType>(
                name: "stagePercentageCalculator",
                description: "State for championShip arena.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "avatarAddress",
                        Description = "Avatar address."
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "stageId",
                        Description = "ID of stage"
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "worldId",
                        Description = "ID of World"
                    },
                    new QueryArgument<ListGraphType<GuidGraphType>>
                    {
                        Description = "list of food id.",
                        DefaultValue = new List<Guid>(),
                        Name = "foodIds",
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "simulationCount",
                        Description = "Amount of simulations, between 1 and 1000"
                    }
                ),
                resolve: context =>
                {
                    Address myAvatarAddress = context.GetArgument<Address>("avatarAddress");
                    int StageId = context.GetArgument<int>("stageId");
                    int WorldId = context.GetArgument<int>("worldId");
                    var Foods = context.GetArgument<List<Guid>>("foodIds");
                    int? StageBuffId = 1;
                    int simulationCount = context.GetArgument<int>("simulationCount");

                    //sheets
                    var sheets = context.Source.WorldState.GetSheets(
                        containQuestSheet: true,
                        containSimulatorSheets: true,
                        sheetTypes: new[]
                        {
                            typeof(WorldSheet),
                            typeof(StageSheet),
                            typeof(StageWaveSheet),
                            typeof(EnemySkillSheet),
                            typeof(CostumeStatSheet),
                            typeof(SkillSheet),
                            typeof(QuestRewardSheet),
                            typeof(QuestItemRewardSheet),
                            typeof(EquipmentItemRecipeSheet),
                            typeof(WorldUnlockSheet),
                            typeof(MaterialItemSheet),
                            typeof(ItemRequirementSheet),
                            typeof(EquipmentItemRecipeSheet),
                            typeof(EquipmentItemSubRecipeSheetV2),
                            typeof(EquipmentItemOptionSheet),
                            typeof(CrystalStageBuffGachaSheet),
                            typeof(CrystalRandomBuffSheet),
                            typeof(StakeActionPointCoefficientSheet),
                            typeof(RuneListSheet),
                            typeof(CollectionSheet),
                            typeof(RuneLevelBonusSheet),
                            typeof(BuffLimitSheet),
                            typeof(BuffLinkSheet),
                        });

                    var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
                    var characterSheet = sheets.GetSheet<CharacterSheet>();
                    if (!sheets.GetSheet<StageSheet>().TryGetValue(StageId, out var stageRow))
                    {
                        throw new SheetRowNotFoundException(nameof(StageSheet), StageId);
                    }

                    //MyAvatar  
                    var myAvatar = context.Source.WorldState.GetAvatarState(myAvatarAddress);

                    if (!characterSheet.TryGetValue(myAvatar.characterId, out var characterRow))
                    {
                        throw new SheetRowNotFoundException("CharacterSheet", myAvatar.characterId);
                    }

                    var myAvatarEquipments = myAvatar.inventory.Equipments;
                    var myAvatarCostumes = myAvatar.inventory.Costumes;

                    List<Guid> myEquipementList = myAvatarEquipments.Where(f=>f.equipped).Select(n => n.ItemId).ToList();
                    List<Guid> myCostumeList = myAvatarCostumes.Where(f=>f.equipped).Select(n => n.ItemId).ToList();

                    var runeSlotStateAddress = RuneSlotState.DeriveAddress(myAvatarAddress, BattleType.Adventure);
                    var runeSlotState = context.Source.WorldState.TryGetLegacyState(runeSlotStateAddress, out List rawRuneSlotState)
                        ? new RuneSlotState(rawRuneSlotState)
                        : new RuneSlotState(BattleType.Adventure);
                    var runeListSheet = sheets.GetSheet<RuneListSheet>();

                    var runeStates = context.Source.WorldState.GetRuneState(myAvatarAddress, out var migrateRequired);

                    //Crystal Buffs//
                    var skillStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(myAvatarAddress);
                    var isNotClearedStage = !myAvatar.worldInformation.IsStageCleared(StageId);
                    var skillsOnWaveStart = new List<Skill>();
                    CrystalRandomSkillState? skillState = null;
                    skillState = context.Source.WorldState.TryGetLegacyState<List>(skillStateAddress, out var serialized)
                        ? new CrystalRandomSkillState(skillStateAddress, serialized)
                        : new CrystalRandomSkillState(skillStateAddress, StageId);

                    if (skillState.SkillIds.Any())
                    {
                        var crystalRandomBuffSheet = sheets.GetSheet<CrystalRandomBuffSheet>();
                        var skillSheet = sheets.GetSheet<SkillSheet>();
                        int selectedId;
                        if (StageBuffId.HasValue && skillState.SkillIds.Contains(StageBuffId.Value))
                        {
                            selectedId = StageBuffId.Value;
                        }
                        else
                        {
                            selectedId = skillState.GetHighestRankSkill(crystalRandomBuffSheet);
                        }
                        var skill = CrystalRandomSkillState.GetSkill(
                            selectedId,
                            crystalRandomBuffSheet,
                            skillSheet);
                        skillsOnWaveStart.Add(skill);
                    }

                    var collectionStates = context.Source.WorldState.GetCollectionStates(new[] { myAvatarAddress });
                    var collectionExist = collectionStates.Count > 0;

                    var modifiers = new Dictionary<Address, List<StatModifier>>
                    {
                        [myAvatarAddress] = new(),
                    };
                    if (collectionExist)
                    {
                        var collectionSheet = sheets.GetSheet<CollectionSheet>();
#pragma warning disable LAA1002
                        foreach (var (address, state) in collectionStates)
#pragma warning restore LAA1002
                        {
                            var modifier = modifiers[address];
                            foreach (var collectionId in state.Ids)
                            {
                                modifier.AddRange(collectionSheet[collectionId].StatModifiers);
                            }
                        }
                    }

                    System.Random rnd  =new System.Random();

                    var simulatorSheets = sheets.GetSimulatorSheets();
                    var BuffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
                    var buffLinkSheet = sheets.GetSheet<BuffLinkSheet>();
                    var gameConfigState = context.Source.WorldState.GetGameConfigState();

                    int Wave0 = 0;
                    int Wave1 = 0;
                    int Wave2 = 0;
                    int Wave3 = 0;

                    for (var i = 0; i <= simulationCount; i++)
                    {
                        LocalRandom random = new LocalRandom(rnd.Next());
                        var simulator = new StageSimulator(
                            random,
                            myAvatar,
                            i == 0 ? Foods : new List<Guid>(),
                            runeStates,
                            runeSlotState,
                            i == 0 ? skillsOnWaveStart : new List<Skill>(),
                            WorldId,
                            StageId,
                            stageRow,
                            sheets.GetSheet<StageWaveSheet>()[StageId],
                            myAvatar.worldInformation.IsStageCleared(StageId),
                            StageRewardExpHelper.GetExp(myAvatar.level, StageId),
                            simulatorSheets,
                            sheets.GetSheet<EnemySkillSheet>(),
                            sheets.GetSheet<CostumeStatSheet>(),
                            StageSimulator.GetWaveRewards(random, stageRow, materialItemSheet),
                            modifiers[myAvatarAddress],
                            BuffLimitSheet,
                            buffLinkSheet,
                            false,
                            shatterStrikeMaxDamage: gameConfigState.ShatterStrikeMaxDamage
                            );

                        simulator.Simulate();

                        switch(simulator.Log.clearedWaveNumber)
                        {
                            case 1:
                                Wave1++;
                                break;
                            case 2:
                                Wave2++;
                                break;
                            case 3:
                                Wave3++;
                                break;
                            default:
                                Wave0++;
                                break;
                        }
                    }

                    var StageResult = new StageResultInfo
                    {
                        AvatarAddress = myAvatarAddress,
                        Stage = StageId,
                        Wave0 = Math.Round(((double)Wave0 / simulationCount) * 100, 2),
                        Wave1 = Math.Round(((double)Wave1 / simulationCount) * 100, 2),
                        Wave2 = Math.Round(((double)Wave2 / simulationCount) * 100, 2),
                        Wave3 = Math.Round(((double)Wave3 / simulationCount) * 100, 2)
                    };
                    return StageResult;
                });

            Field<NonNullGraphType<ArenaSimulationStateType>>(
                name: "arenaPercentageCalculator",
                description: "State for championShip arena.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "avatarAddress",
                        Description = "Avatar address."
                    },
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "enemyAvatarAddress",
                        Description = "Enemy Avatar address."
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "simulationCount",
                        Description = "Amount of simulations, between 1 and 1000"
                    }
                ),
                resolve: context =>
                {
                    Address myAvatarAddress = context.GetArgument<Address>("avatarAddress");
                    Address enemyAvatarAddress = context.GetArgument<Address>("enemyAvatarAddress");
                    int simulationCount = context.GetArgument<int>("simulationCount");

                    var sheets = context.Source.WorldState.GetSheets(containArenaSimulatorSheets: true, sheetTypes: new[]
                    {
                            typeof(WorldSheet),
                            typeof(StageSheet),
                            typeof(StageWaveSheet),
                            typeof(EnemySkillSheet),
                            typeof(CostumeStatSheet),
                            typeof(SkillSheet),
                            typeof(QuestRewardSheet),
                            typeof(QuestItemRewardSheet),
                            typeof(EquipmentItemRecipeSheet),
                            typeof(WorldUnlockSheet),
                            typeof(MaterialItemSheet),
                            typeof(ItemRequirementSheet),
                            typeof(EquipmentItemRecipeSheet),
                            typeof(EquipmentItemSubRecipeSheetV2),
                            typeof(EquipmentItemOptionSheet),
                            typeof(CrystalStageBuffGachaSheet),
                            typeof(CrystalRandomBuffSheet),
                            typeof(StakeActionPointCoefficientSheet),
                            typeof(RuneListSheet),
                            typeof(CollectionSheet),
                            typeof(RuneLevelBonusSheet),
                            typeof(BuffLimitSheet),
                            typeof(BuffLinkSheet),
                    });

                    if(simulationCount < 1 || simulationCount > 1000)
                    {
                        throw new Exception("arenaPercentageCalculator - Invalid simulationCount");
                    }

                    var myAvatar = context.Source.WorldState.GetAvatarState(myAvatarAddress);
                    var enemyAvatar = context.Source.WorldState.GetAvatarState(enemyAvatarAddress);

                    //sheets
                    var arenaSheets = sheets.GetArenaSimulatorSheets();
                    var characterSheet = sheets.GetSheet<CharacterSheet>();

                    if (!characterSheet.TryGetValue(myAvatar.characterId, out var characterRow) || !characterSheet.TryGetValue(enemyAvatar.characterId, out var characterRow2))
                    {
                        throw new SheetRowNotFoundException("CharacterSheet", myAvatar.characterId);
                    }


                    var gameConfigState = context.Source.WorldState.GetGameConfigState();

                    //MyAvatar                
                    var myArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(myAvatarAddress);
                    if (!context.Source.WorldState.TryGetArenaAvatarState(myArenaAvatarStateAdr, out var myArenaAvatarState))
                    {
                        throw new ArenaAvatarStateNotFoundException(
                            $"[{nameof(BattleArena)}] my avatar address : {myAvatarAddress}");
                    }
                    var myAvatarEquipments = myAvatar.inventory.Equipments;
                    var myAvatarCostumes = myAvatar.inventory.Costumes;
                    List<Guid> myArenaEquipementList = myAvatarEquipments.Where(f=>myArenaAvatarState.Equipments.Contains(f.ItemId)).Select(n => n.ItemId).ToList();
                    List<Guid> myArenaCostumeList = myAvatarCostumes.Where(f=>myArenaAvatarState.Costumes.Contains(f.ItemId)).Select(n => n.ItemId).ToList();

                    var myRuneSlotStateAddress = RuneSlotState.DeriveAddress(myAvatarAddress, BattleType.Arena);
                    var myRuneSlotState = context.Source.WorldState.TryGetLegacyState(myRuneSlotStateAddress, out List myRawRuneSlotState)
                        ? new RuneSlotState(myRawRuneSlotState)
                        : new RuneSlotState(BattleType.Arena);
                    var myRuneStates = context.Source.WorldState.GetRuneState(myAvatarAddress, out var migrateRequired);

                    //Enemy
                    var enemyArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(enemyAvatarAddress);
                    if (!context.Source.WorldState.TryGetArenaAvatarState(enemyArenaAvatarStateAdr, out var enemyArenaAvatarState))
                    {
                        throw new ArenaAvatarStateNotFoundException(
                            $"[{nameof(BattleArena)}] my avatar address : {enemyAvatarAddress}");
                    }
                    var enemyAvatarEquipments = enemyAvatar.inventory.Equipments;
                    var enemyAvatarCostumes = enemyAvatar.inventory.Costumes;
                    List<Guid> enemyArenaEquipementList = enemyAvatarEquipments.Where(f=>enemyArenaAvatarState.Equipments.Contains(f.ItemId)).Select(n => n.ItemId).ToList();
                    List<Guid> enemyArenaCostumeList = enemyAvatarCostumes.Where(f=>enemyArenaAvatarState.Costumes.Contains(f.ItemId)).Select(n => n.ItemId).ToList();

                    var enemyRuneSlotStateAddress = RuneSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
                    var enemyRuneSlotState = context.Source.WorldState.TryGetLegacyState(enemyRuneSlotStateAddress, out List enemyRawRuneSlotState)
                        ? new RuneSlotState(enemyRawRuneSlotState)
                        : new RuneSlotState(BattleType.Arena);

                    var enemyRuneStates = context.Source.WorldState.GetRuneState(enemyAvatarAddress, out _);

                    var myArenaPlayerDigest = new ArenaPlayerDigest(
                        myAvatar,
                        myArenaEquipementList,
                        myArenaCostumeList,
                        myRuneStates,
                        myRuneSlotState
                        );

                    var enemyArenaPlayerDigest = new ArenaPlayerDigest(
                        enemyAvatar,
                        enemyArenaEquipementList,
                        enemyArenaCostumeList,
                        enemyRuneStates,
                        enemyRuneSlotState
                        );

                    var collectionStates = context.Source.WorldState.GetCollectionStates(new[] { myAvatarAddress, enemyAvatarAddress });
                    var collectionExist = collectionStates.Count > 0;

                    var modifiers = new Dictionary<Address, List<StatModifier>>
                    {
                        [myAvatarAddress] = new(),
                        [enemyAvatarAddress] = new(),
                    };
                    if (collectionExist)
                    {
                        var collectionSheet = sheets.GetSheet<CollectionSheet>();
#pragma warning disable LAA1002
                        foreach (var (address, state) in collectionStates)
#pragma warning restore LAA1002
                        {
                            var modifier = modifiers[address];
                            foreach (var collectionId in state.Ids)
                            {
                                modifier.AddRange(collectionSheet[collectionId].StatModifiers);
                            }
                        }
                    }

                    var BuffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
                    System.Random rnd  =new System.Random();          

                    int win = 0;
                    int loss = 0;

                    List<ArenaSimulationResult> arenaResultsList = new List<ArenaSimulationResult>();
                    ArenaSimulationState arenaSimulationState = new ArenaSimulationState();
                    arenaSimulationState.blockIndex = context.Source.BlockIndex;
                    var buffLinkSheet = sheets.GetSheet<BuffLinkSheet>();
                    var buffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
                    
                    for (var i = 0; i < simulationCount; i++)
                    {
                        ArenaSimulationResult arenaResult = new ArenaSimulationResult();
                        arenaResult.seed = rnd.Next();
                        LocalRandom iRandom = new LocalRandom(arenaResult.seed);

                        var simulator = new ArenaSimulator(
                            iRandom,
                            5,
                            gameConfigState.ShatterStrikeMaxDamage
                        );

                        var log = simulator.Simulate(
                            myArenaPlayerDigest,
                            enemyArenaPlayerDigest,
                            arenaSheets,
                            modifiers[myAvatarAddress],
                            modifiers[enemyAvatarAddress],
                            BuffLimitSheet,
                            buffLinkSheet,
                            true);
                            
                        if(log.Result.ToString() == "Win")
                        {
                            arenaResult.win = true;
                            win++;
                        }
                        else
                        {
                            loss++;
                            arenaResult.win = false;
                        }
                        arenaResultsList.Add(arenaResult);
                    }
                    arenaSimulationState.winPercentage = Math.Round(((decimal)win / simulationCount) * 100m, 2);
                    arenaSimulationState.result = arenaResultsList;
                    return arenaSimulationState;
                });
            
            Field<NonNullGraphType<ArenaSimulationStateType>>(
                name: "arenaPercentageCalculatorRevamp",
                description: "State for championShip arena.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "avatarAddress",
                        Description = "Avatar address."
                    },
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "enemyAvatarAddress",
                        Description = "Enemy Avatar address."
                    },
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "agentAddress",
                        Description = "Agent address."
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "simulationCount",
                        Description = "Amount of simulations, between 1 and 1000"
                    }
                ),
                resolve: context =>
                {
                    Address myAvatarAddress = context.GetArgument<Address>("avatarAddress");
                    Address myAgentAddress = context.GetArgument<Address>("agentAddress");
                    Address enemyAvatarAddress = context.GetArgument<Address>("enemyAvatarAddress");
                    int simulationCount = context.GetArgument<int>("simulationCount");
                    var sw = Stopwatch.StartNew();

                    var states = context.Source.WorldState;

                    var addressesHex = GetSignerAndOtherAddressesHex(
                        myAgentAddress,
                        myAvatarAddress,
                        enemyAvatarAddress
                    );

                    var myAvatarState = MeasureAndLog(
                        "Validate and get avatar state",
                        sw,
                        () => ValidateAndGetMyAvatarState(states, myAgentAddress, myAvatarAddress, enemyAvatarAddress)
                    );

                    var collectionStates = MeasureAndLog(
                        "Get collection states",
                        sw,
                        () => states.GetCollectionStates(new[] { myAvatarAddress, enemyAvatarAddress })
                    );

                    var gameConfigState = MeasureAndLog(
                        "Load gameConfig",
                        sw,
                        () => states.GetGameConfigState()
                    );

                    var sheets = MeasureAndLog(
                        "Load sheets",
                        sw,
                        () => LoadSheetsArenaBattle(states, collectionStates.Any())
                    );

                    var collectionModifiers = new Dictionary<Address, List<StatModifier>>
                    {
                        [myAvatarAddress] = new(),
                        [enemyAvatarAddress] = new(),
                    };

                    if (collectionStates.Any())
                    {
                        var collectionSheet = sheets.GetSheet<CollectionSheet>();
                        foreach (var (address, state) in collectionStates)
                        {
                            collectionModifiers[address] = state.GetModifiers(collectionSheet);
                        }
                    }

                    var myLoadout = MeasureAndLog(
                        "Get my spec",
                        sw,
                        () =>
                            PrepareMyLoadout(
                                states,
                                sheets,
                                myAvatarState,
                                context.Source.BlockIndex,
                                addressesHex,
                                gameConfigState,
                                collectionModifiers,
                                myAvatarAddress
                            )
                    );
                    var (updatedStates, myItemSlotState, myRuneSlotState, myRuneStates, myCp) = myLoadout;

                    var enemyLoadout = MeasureAndLog(
                        "Get enemy spec",
                        sw,
                        () => PrepareEnemyLoadout(states, enemyAvatarAddress)
                    );

                    System.Random rnd = new System.Random();
                    int win = 0;
                    int loss = 0;

                    List<ArenaSimulationResult> arenaResultsList = new List<ArenaSimulationResult>();
                    ArenaSimulationState arenaSimulationState = new ArenaSimulationState();
                    arenaSimulationState.blockIndex = context.Source.BlockIndex;

                    for (var i = 0; i < simulationCount; i++)
                    {
                        ArenaSimulationResult arenaResult = new ArenaSimulationResult();
                        arenaResult.seed = rnd.Next();
                        LocalRandom iRandom = new LocalRandom(arenaResult.seed);

                        var resultLog = MeasureAndLog(
                        "Simulate battle",
                        sw,
                        () =>
                            Simulate(
                                states,
                                sheets,
                                myAvatarState,
                                iRandom,
                                gameConfigState,
                                collectionModifiers,
                                (myItemSlotState, myRuneSlotState, myRuneStates),
                                enemyLoadout,
                                myAvatarAddress,
                                enemyAvatarAddress
                            )
                        );

                        if(resultLog.Result.ToString() == "Win")
                        {
                            arenaResult.win = true;
                            win++;
                        }
                        else
                        {
                            loss++;
                            arenaResult.win = false;
                        }
                        arenaResultsList.Add(arenaResult);
                    }
                    
                    arenaSimulationState.winPercentage = Math.Round(((decimal)win / simulationCount) * 100m, 2);
                    arenaSimulationState.result = arenaResultsList;
                    return arenaSimulationState;
                });

            Field<NonNullGraphType<CombinationSimulationStateType>>(
               name: "combinationSimulator",
               description: "State for championShip arena.",
               arguments: new QueryArguments(
                   new QueryArgument<NonNullGraphType<AddressType>>
                   {
                       Name = "agentAdress",
                       Description = "Avatar address."
                   },
                   new QueryArgument<NonNullGraphType<AddressType>>
                   {
                       Name = "avatarAddress",
                       Description = "Avatar address."
                   },
                   new QueryArgument<NonNullGraphType<StringGraphType>>
                   {
                       Name = "recipeId",
                       Description = "Enemy Avatar address."
                   },
                   new QueryArgument<NonNullGraphType<StringGraphType>>
                   {
                       Name = "subRecipeId",
                       Description = "Enemy Avatar address."
                   },
                   new QueryArgument<NonNullGraphType<IntGraphType>>
                   {
                       Name = "simulationCount",
                       Description = "Amount of simulations, between 1 and 1000"
                   }
               ),
               resolve: context =>
               {
                   Address agentAdress = context.GetArgument<Address>("agentAdress");
                   Address avatarAddress = context.GetArgument<Address>("avatarAddress");
                   int recipeId = context.GetArgument<int>("recipeId");
                   int subRecipeId = context.GetArgument<int>("subRecipeId");
                   int simulationCount = context.GetArgument<int>("simulationCount");

                   var states = context.Source;

                   var sheets = context.Source.WorldState.GetSheets(sheetTypes: new[]
                   {
                        typeof(EquipmentItemRecipeSheet),
                        typeof(EquipmentItemSheet),
                        typeof(MaterialItemSheet),
                        typeof(EquipmentItemSubRecipeSheetV2),
                        typeof(EquipmentItemOptionSheet),
                        typeof(SkillSheet),
                        typeof(CrystalMaterialCostSheet),
                        typeof(CrystalFluctuationSheet),
                        typeof(CrystalHammerPointSheet),
                        typeof(PetOptionSheet),
                        typeof(ConsumableItemRecipeSheet),                    
                   });

                   var agentState = context.Source.WorldState.GetAgentState(agentAdress);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                   if (!agentState.address.Equals(agentAdress))
                   {
                       throw new Exception("arenaPercentageCalculator - Invalid simulationCount");
                   }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                   var avatarState = context.Source.WorldState.GetAvatarState(avatarAddress);

                   // Validate RecipeId
                   var equipmentItemRecipeSheet = sheets.GetSheet<EquipmentItemRecipeSheet>();
                   if (!equipmentItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
                   {
                       throw new Exception("arenaPercentageCalculator - Invalid simulationCount");
                   }
                   // ~Validate RecipeId

                   // Validate Recipe ResultEquipmentId
                   var equipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>();
                   if (!equipmentItemSheet.TryGetValue(recipeRow.ResultEquipmentId, out var equipmentRow))
                   {
                       throw new Exception("arenaPercentageCalculator - Invalid simulationCount");
                   }
                   // ~Validate Recipe ResultEquipmentId

                   // Validate Recipe Material
                   var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
                   if (!materialItemSheet.TryGetValue(recipeRow.MaterialId, out var materialRow))
                   {
                       throw new Exception("arenaPercentageCalculator - Invalid simulationCount");
                   }

                   var requiredFungibleItems = new Dictionary<int, int>();

                   if (requiredFungibleItems.ContainsKey(materialRow.Id))
                   {
                       requiredFungibleItems[materialRow.Id] += recipeRow.MaterialCount;
                   }
                   else
                   {
                       requiredFungibleItems[materialRow.Id] = recipeRow.MaterialCount;
                   }

                   // Validate Recipe Unlocked.
                   if (equipmentItemRecipeSheet[recipeId].CRYSTAL != 0)
                   {
                       var unlockedRecipeIdsAddress = avatarAddress.Derive("recipe_ids");
                       if (!context.Source.WorldState.TryGetLegacyState(unlockedRecipeIdsAddress, out List rawIds))
                       {
                           throw new FailedLoadStateException("can't find UnlockedRecipeList.");
                       }

                       var unlockedIds = rawIds.ToList(StateExtensions.ToInteger);
                       if (!unlockedIds.Contains(recipeId))
                       {
                           throw new InvalidRecipeIdException($"unlock {recipeId} first.");
                       }

                       if (!avatarState.worldInformation.IsStageCleared(recipeRow.UnlockStage))
                       {
                           avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                           throw new FailedLoadStateException("can't find UnlockedRecipeList.");
                       }
                   }
                   // ~Validate Recipe Unlocked

                   // Validate SubRecipeId
                   EquipmentItemSubRecipeSheetV2.Row? subRecipeRow = null;
                   if (subRecipeId > 0)
                   {
                       if (!recipeRow.SubRecipeIds.Contains(subRecipeId))
                       {
                           throw new Exception("arenaPercentageCalculator - Invalid simulationCount");
                       }

                       var equipmentItemSubRecipeSheetV2 = sheets.GetSheet<EquipmentItemSubRecipeSheetV2>();
                       if (!equipmentItemSubRecipeSheetV2.TryGetValue(subRecipeId, out subRecipeRow))
                       {
                           throw new Exception("arenaPercentageCalculator - Invalid simulationCount");
                       }

                       // Validate SubRecipe Material
                       for (var i = subRecipeRow.Materials.Count; i > 0; i--)
                       {
                           var materialInfo = subRecipeRow.Materials[i - 1];
                           if (!materialItemSheet.TryGetValue(materialInfo.Id, out materialRow))
                           {
                               throw new Exception("arenaPercentageCalculator - Invalid simulationCount");
                           }

                           if (requiredFungibleItems.ContainsKey(materialRow.Id))
                           {
                               requiredFungibleItems[materialRow.Id] += materialInfo.Count;
                           }
                           else
                           {
                               requiredFungibleItems[materialRow.Id] = materialInfo.Count;
                           }
                       }
                   }

                   var existHammerPointSheet =
                       sheets.TryGetSheet(out CrystalHammerPointSheet hammerPointSheet);
                   var hammerPointAddress =
                       Addresses.GetHammerPointStateAddress(avatarAddress, recipeId);
                   var hammerPointState = new HammerPointState(hammerPointAddress, recipeId);
                   CrystalHammerPointSheet.Row? hammerPointRow = null;
                   if (existHammerPointSheet)
                   {
                       if (context.Source.WorldState.TryGetLegacyState(hammerPointAddress, out List serialized))
                       {
                           hammerPointState =
                               new HammerPointState(hammerPointAddress, serialized);
                       }

                       // Validate HammerPointSheet by recipeId
                       if (!hammerPointSheet.TryGetValue(recipeId, out hammerPointRow))
                       {
                           throw new Exception("arenaPercentageCalculator - Invalid simulationCount");
                       }
                   }
                   long endBlockIndex = 99999999999;
                   //var isMimisbrunnrSubRecipe = subRecipeRow?.IsMimisbrunnrSubRecipe ??
                   //    subRecipeId.HasValue && recipeRow.SubRecipeIds[2] == subRecipeId.Value;
                   var petOptionSheet = sheets.GetSheet<PetOptionSheet>();
                   //bool useHammerPoint = false;
                   //if (useHammerPoint)
                   //{
                   //    if (!existHammerPointSheet)
                   //    {
                   //        throw new FailedLoadSheetException(typeof(CrystalHammerPointSheet));
                   //    }

                   //    states = UseAssetsBySuperCraft(
                   //        states,
                   //        context,
                   //        hammerPointRow,
                   //        hammerPointState);
                   //}
                   //else
                   //{
                   //    states = UseAssetsByNormalCombination(
                   //        states,
                   //        context,
                   //        avatarState,
                   //        hammerPointState,
                   //        petState,
                   //        sheets,
                   //        materialItemSheet,
                   //        hammerPointSheet,
                   //        petOptionSheet,
                   //        recipeRow,
                   //        subRecipeRow,
                   //        requiredFungibleItems,
                   //        addressesHex);
                   //}
                   PetState? petState = null;

                   List<CombinationSimulationResult> combinationResultsList = new List<CombinationSimulationResult>();
                   CombinationSimulationState combinationSimulationState = new CombinationSimulationState();
                   combinationSimulationState.blockIndex = context.Source.BlockIndex;

                   int oneStar = 0;
                   int twoStar = 0;
                   int threeStar = 0;
                   int fourStar = 0;
                   int spell = 0;
                
                   for(int i = 0; i < simulationCount; i++)
                   {
                       CombinationSimulationResult combinationResult = new CombinationSimulationResult();
                       // Create Equipment
                       var equipment = (Equipment)ItemFactory.CreateItemUsable(
                            equipmentRow,
                            Guid.NewGuid(),
                            endBlockIndex,
                            madeWithMimisbrunnrRecipe: false
                       );
                       System.Random random = new System.Random();
                       int seed = random.Next(1, 1000000);
                       LocalRandom random1 = new LocalRandom(seed);

                       if (!(subRecipeRow is null))
                       {
                           AddAndUnlockOption(
                               agentState,
                               petState,
                               equipment,
                               random1,
                               subRecipeRow,
                               sheets.GetSheet<EquipmentItemOptionSheet>(),
                               petOptionSheet,
                               sheets.GetSheet<SkillSheet>()
                           );
                           endBlockIndex = 99999999;
                       }

                       if(equipment.Skills.Any())
                       {
                          spell++;
                          combinationResult.spellChance = equipment.Skills[0].Chance;
                          combinationResult.spellPower = equipment.Skills[0].Power;
                       }

                       switch(equipment.optionCountFromCombination)
                       {
                           case 1:
                              oneStar++;
                              break;

                           case 2:
                              twoStar++;
                              break;

                           case 3:
                              threeStar++;
                              break;

                           case 4:
                              fourStar++;
                              combinationResult.spellChance = equipment.Skills[0].Chance;
                              combinationResult.spellPower = equipment.Skills[0].Power;
                              break;
                       }

                       combinationResult.seed = seed;
                       combinationResult.starCount = equipment.optionCountFromCombination;
                       combinationResultsList.Add(combinationResult);
                   }
                   combinationSimulationState.oneStarPercentage = Math.Round(((decimal)oneStar / simulationCount) * 100m, 2);
                   combinationSimulationState.twoStarPercentage = Math.Round(((decimal)twoStar / simulationCount) * 100m, 2);
                   combinationSimulationState.threeStarPercentage = Math.Round(((decimal)threeStar / simulationCount) * 100m, 2);
                   combinationSimulationState.fourStarPercentage = Math.Round(((decimal)fourStar / simulationCount) * 100m, 2);
                   combinationSimulationState.spellPercentage = Math.Round(((decimal)spell / simulationCount) * 100m, 2);
                   combinationSimulationState.result = combinationResultsList;
                   return combinationSimulationState;
               }
            );

            Field<NonNullGraphType<AdventureBossSimulationStateType>>(
                name: "adventureBossPercentageCalculator",
                description: "State for championShip arena.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "agentAddress",
                        Description = "Agent address."
                    },
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "avatarAddress",
                        Description = "Avatar address."
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "simulationCount",
                        Description = "Amount of simulations, between 1 and 1000"
                    }
                ),
                resolve: context =>
                {
                    Address myAgentAddress = context.GetArgument<Address>("agentAddress");
                    Address myAvatarAddress = context.GetArgument<Address>("avatarAddress");
                    List<Guid> FoodsList = new List<Guid>();


                    int simulationCount = context.GetArgument<int>("simulationCount");

                    var states = context.Source.WorldState;

                    // Validation
                    var season = states.GetLatestAdventureBossSeason();

                    if (!states.TryGetAvatarState(myAgentAddress, myAvatarAddress, out var avatarState))
                    {
                        throw new FailedLoadStateException(
                            $"Aborted as the avatar state of the signer was failed to load.");
                    }

                    var exploreBoard = states.GetExploreBoard(season.Season);

                    Explorer explorer;
                    if (states.TryGetExplorer(season.Season, myAvatarAddress, out var exp))
                    {
                        explorer = exp;
                    }
                    else
                    {
                        explorer = new Explorer(myAvatarAddress, avatarState.name);
                        var explorerList = states.GetExplorerList(season.Season);
                        explorerList.AddExplorer(myAvatarAddress, avatarState.name);
                        exploreBoard.ExplorerCount = explorerList.Explorers.Count;
                    }

                    //if (explorer.Floor == explorer.MaxFloor)
                    //{
                    //    throw new InvalidOperationException("Reached to locked floor. Unlock floor first.");
                    //}

                    //if (explorer.Floor == UnlockFloor.TotalFloor)
                    //{
                    //    throw new InvalidOperationException("Already cleared all floors");
                    //}
                    var sheets = states.GetSheets(
                        containSimulatorSheets: true,
                        sheetTypes: new[]
                        {
                            typeof(AdventureBossSheet),
                            typeof(AdventureBossFloorSheet),
                            typeof(AdventureBossFloorWaveSheet),
                            typeof(CollectionSheet),
                            typeof(EnemySkillSheet),
                            typeof(CostumeStatSheet),
                            typeof(BuffLimitSheet),
                            typeof(BuffLinkSheet),
                            typeof(ItemRequirementSheet),
                            typeof(EquipmentItemRecipeSheet),
                            typeof(EquipmentItemSubRecipeSheetV2),
                            typeof(EquipmentItemOptionSheet),
                            typeof(RuneListSheet),
                            typeof(RuneLevelBonusSheet),
                        });
                    var materialSheet = sheets.GetSheet<MaterialItemSheet>();
#pragma warning disable CS8604 // Possible null reference argument.
                    var material =
                            materialSheet.OrderedList.First(row => row.ItemSubType == ItemSubType.ApStone);
#pragma warning restore CS8604 // Possible null reference argument.
                    System.Random rndom = new System.Random();
                    
                    //var selector = new WeightedSelector<AdventureBossFloorSheet.RewardData>((IRandom)rndom);
                    var rewardList = new List<AdventureBossSheet.RewardAmountData>();

                    // Validate
                    var gameConfigState = states.GetGameConfigState();
                    //if (gameConfigState is null)
                    //{
                    //    throw new FailedLoadStateException(
                    //        $"{addressesHex}Aborted as the game config state was failed to load.");
                    //}

                    //MyAvatar                
                    var myAvatar = context.Source.WorldState.GetAvatarState(myAvatarAddress);

                    var myAvatarEquipments = myAvatar.inventory.Equipments;
                    var myAvatarCostumes = myAvatar.inventory.Costumes;

                    List<Guid> myEquipementList = myAvatarEquipments.Where(f => f.equipped).Select(n => n.ItemId).ToList();
                    List<Guid> myCostumeList = myAvatarCostumes.Where(f => f.equipped).Select(n => n.ItemId).ToList();

                    // update rune slot
                    var runeSlotStateAddress =
                        RuneSlotState.DeriveAddress(myAvatarAddress, BattleType.Adventure);
                    var runeSlotState =
                        states.TryGetLegacyState(runeSlotStateAddress, out List rawRuneSlotState)
                            ? new RuneSlotState(rawRuneSlotState)
                            : new RuneSlotState(BattleType.Adventure);
                    var runeListSheet = sheets.GetSheet<RuneListSheet>();

                    // update item slot
                    var itemSlotStateAddress =
                        ItemSlotState.DeriveAddress(myAvatarAddress, BattleType.Adventure);
                    var itemSlotState =
                        states.TryGetLegacyState(itemSlotStateAddress, out List rawItemSlotState)
                            ? new ItemSlotState(rawItemSlotState)
                            : new ItemSlotState(BattleType.Adventure);

                    // Get data for simulator
                    var runeStates = states.GetRuneState(myAvatarAddress, out var migrateRequired);

                    var collectionExist =
                        states.TryGetCollectionState(myAvatarAddress, out var collectionState) &&
                        collectionState.Ids.Any();
                    var collectionModifiers = new List<StatModifier>();
                    if (collectionExist)
                    {
                        var collectionSheet = sheets.GetSheet<CollectionSheet>();
                        collectionModifiers = collectionState.GetModifiers(collectionSheet);
                    }

                    var floorSheet = sheets.GetSheet<AdventureBossFloorSheet>();
                    var floorWaveSheet = sheets.GetSheet<AdventureBossFloorWaveSheet>();
                    var simulatorSheets = sheets.GetSimulatorSheets();
                    var enemySkillSheet = sheets.GetSheet<EnemySkillSheet>();
                    var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
                    var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
                    var buffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
                    var buffLinkSheet = sheets.GetSheet<BuffLinkSheet>();

                    var bossId = states.GetSheet<AdventureBossSheet>().Values
                        .First(row => row.BossId == season.BossId).Id;
                    var floorRows = states.GetSheet<AdventureBossFloorSheet>().Values
                        .Where(row => row.AdventureBossId == bossId).ToList();
                    var firstRewardSheet = states.GetSheet<AdventureBossFloorFirstRewardSheet>();
                    var pointSheet = states.GetSheet<AdventureBossFloorPointSheet>();

                    AdventureBossSimulator? simulator = null;
                    var firstFloorId = 0;
                    var floorIdList = new List<int>();

                    // Claim floors from last failed
#pragma warning disable CS8604 // Possible null reference argument.
                    var exploreAp = sheets.GetSheet<AdventureBossSheet>().OrderedList
                            .First(row => row.BossId == season.BossId).ExploreAp;
#pragma warning restore CS8604 // Possible null reference argument.

                    List<AdventureBossSimulationResult> adventureBossResultsList = new List<AdventureBossSimulationResult>();
                    AdventureBossSimulationState adventureBossSimulationState = new AdventureBossSimulationState();
                    adventureBossSimulationState.blockIndex = context.Source.BlockIndex;

                    for (var fl = 1; fl < 20+1; fl++)
                    {
                        AdventureBossSimulationResult adventureBossResults = new AdventureBossSimulationResult();

                        // Get Data for simulator
                        var floorRow = floorRows.FirstOrDefault(row => row.Floor == fl);

                        if (floorRow is null)
                        {
                            throw new FailedLoadStateException(
                                $"Aborted as the game config state was failed to load.");
                        }

                        if (firstFloorId == 0)
                        {
                            firstFloorId = floorRow.Id;
                        }
                        
                        int win = 0;
                        LocalRandom random = new LocalRandom(rndom.Next());
                        var rewards = AdventureBossSimulator.GetWaveRewards(random, floorRow, materialItemSheet);

                        for(var y = 0; y < simulationCount; y++)
                        {
                            
                            random = new LocalRandom(rndom.Next());
                            simulator = new AdventureBossSimulator(
                                bossId: season.BossId,
                                floorId: floorRow.Id,
                                random,
                                avatarState,
                                FoodsList,
                                runeStates,
                                runeSlotState,
                                floorRow,
                                floorWaveSheet[floorRow.Id],
                                simulatorSheets,
                                enemySkillSheet,
                                costumeStatSheet,
                                rewards,
                                collectionModifiers,
                                buffLimitSheet,
                                buffLinkSheet,
                                false,
                                gameConfigState.ShatterStrikeMaxDamage
                            );

                            simulator.Simulate();

                            // Get Reward if cleared
                            if (simulator.Log.IsClear)
                            {
                                win++;
                            }
                        }

                        adventureBossResults.floor = fl;
                        adventureBossResults.winPercentage = Math.Round(((decimal)win / simulationCount) * 100m, 2);
                        adventureBossResultsList.Add(adventureBossResults);
                    }
                    adventureBossSimulationState.result = adventureBossResultsList;
                    return adventureBossSimulationState;
                }
            );
        }
        
        public static void AddAndUnlockOption(
            AgentState agentState,
            PetState? petState,
            Equipment equipment,
            IRandom random,
            EquipmentItemSubRecipeSheetV2.Row subRecipe,
            EquipmentItemOptionSheet optionSheet,
            PetOptionSheet petOptionSheet,
            SkillSheet skillSheet
        )
        {
            foreach (var optionInfo in subRecipe.Options
                .OrderByDescending(e => e.Ratio)
                .ThenBy(e => e.RequiredBlockIndex)
                .ThenBy(e => e.Id))
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                var value = random.Next(1, GameConfig.MaximumProbability + 1);
                var ratio = optionInfo.Ratio;

                // Apply pet bonus if possible
                if (!(petState is null))
                {
                    ratio = PetHelper.GetBonusOptionProbability(
                        ratio,
                        petState,
                        petOptionSheet);
                }

                if (value > ratio)
                {
                    continue;
                }

                if (optionRow.StatType != StatType.NONE)
                {
                    var stat = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(stat.StatType, stat.BaseValue);
                    equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                    equipment.optionCountFromCombination++;
                }
                else
                {
                    var skill = CombinationEquipment.GetSkill(optionRow, skillSheet, random);
                    if (!(skill is null))
                    {
                        equipment.Skills.Add(skill);
                        equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                        equipment.optionCountFromCombination++;
                    }
                }
            }
        }
        private T MeasureAndLog<T>(string process, Stopwatch stopwatch, Func<T> action)
        {
            stopwatch.Restart();
            var result = action();
            stopwatch.Stop();
            Log.Verbose(
                "{Process} completed in {Elapsed} ms",
                process,
                stopwatch.Elapsed.TotalMilliseconds
            );
            return result;
        }
        private Dictionary<Type, (Address address, ISheet sheet)> LoadSheetsArenaBattle(
            IWorldState states,
            bool collectionExist
        )
        {
            var sheetTypes = new List<Type>
            {
                typeof(ArenaSheet),
                typeof(ItemRequirementSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(MaterialItemSheet),
                typeof(RuneListSheet),
                typeof(RuneLevelBonusSheet),
                typeof(BuffLimitSheet),
                typeof(BuffLinkSheet),
                typeof(CharacterSheet),
                typeof(CostumeStatSheet),
            };

            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }

            var sheets = states.GetSheets(
                containArenaSimulatorSheets: true,
                sheetTypes: sheetTypes
            );
            return sheets;
        }

        private AvatarState ValidateAndGetMyAvatarState(IWorldState states, Address signer, Address myAvatarAddress, Address enemyAvatarAddress)
        {
            if (myAvatarAddress.Equals(enemyAvatarAddress))
            {
                throw new InvalidAddressException("Battle initiated with identical addresses.");
            }

            if (!states.TryGetAvatarState(signer, myAvatarAddress, out var myAvatarState))
            {
                throw new FailedLoadStateException("Failed to load avatar state for signer.");
            }

            return myAvatarState;
        }

        protected string GetSignerAndOtherAddressesHex(Address agentAddress, params Address[] addresses)
        {
            StringBuilder sb = new StringBuilder($"[{agentAddress.ToHex()}");

            foreach (Address address in addresses)
            {
                sb.Append($", {address.ToHex()}");
            }

            sb.Append("]");
            return sb.ToString();
        }

        private (
            IWorldState UpdatedStates,
            ItemSlotState ItemSlotState,
            RuneSlotState RuneSlotState,
            AllRuneState RuneStates,
            int Cp
        ) PrepareMyLoadout(
            IWorldState states,
            Dictionary<Type, (Address address, ISheet sheet)> sheets,
            AvatarState myAvatarState,
            long? blockIndex,
            string addressesHex,
            GameConfigState gameConfigState,
            Dictionary<Address, List<StatModifier>> collectionModifiers,
            Address myAvatarAddress
        )
        {

            var myArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(myAvatarAddress);
            if (!states.TryGetArenaAvatarState(myArenaAvatarStateAdr, out var myArenaAvatarState))
            {
                throw new ArenaAvatarStateNotFoundException(
                $"[{nameof(BattleArena)}] my avatar address : {myAvatarAddress}");
            }

            var myAvatarEquipments = myAvatarState.inventory.Equipments;
            var myAvatarCostumes = myAvatarState.inventory.Costumes;
            List<Guid> equipments = myAvatarEquipments.Where(f=>myArenaAvatarState.Equipments.Contains(f.ItemId)).Select(n => n.ItemId).ToList();
            List<Guid> costumes = myAvatarCostumes.Where(f=>myArenaAvatarState.Costumes.Contains(f.ItemId)).Select(n => n.ItemId).ToList();

            if(blockIndex is null)
            {
                throw new InvalidAddressException("BlockIndex is NULL.");
            }

            long blockIndexF = (long)blockIndex;

            var (equipmentItems, costumeItems) = myAvatarState.ValidEquipmentAndCostumeV2(
                costumes,
                equipments,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                blockIndexF,
                addressesHex,
                gameConfigState
            );

            var myRuneSlotStateAddress = RuneSlotState.DeriveAddress(
                myAvatarAddress,
                BattleType.Arena
            );
            var myRuneSlotState = states.TryGetLegacyState(
                myRuneSlotStateAddress,
                out List rawRuneSlotState
            )
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);

            var runeListSheet = sheets.GetSheet<RuneListSheet>();

            var myItemSlotStateAddress = ItemSlotState.DeriveAddress(
                myAvatarAddress,
                BattleType.Arena
            );
            var myItemSlotState = states.TryGetLegacyState(
                myItemSlotStateAddress,
                out List rawItemSlotState
            )
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Arena);

            myItemSlotState.UpdateEquipment(equipments);
            myItemSlotState.UpdateCostumes(costumes);

            var myRuneStates = states.GetRuneState(myAvatarAddress, out var migrateRequired);

            var characterSheet = sheets.GetSheet<CharacterSheet>();
            if (!characterSheet.TryGetValue(myAvatarState.characterId, out var myCharacterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", myAvatarState.characterId);
            }

            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var myRuneOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeInfo in myRuneSlotState.GetEquippedRuneSlotInfos())
            {
                if (!myRuneStates.TryGetRuneState(runeInfo.RuneId, out var runeState))
                {
                    continue;
                }

                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.RuneId);
                }

                if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.Level);
                }

                myRuneOptions.Add(option);
            }

            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var runeLevelBonusSheet = sheets.GetSheet<RuneLevelBonusSheet>();
            var myRuneLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                myRuneStates,
                runeListSheet,
                runeLevelBonusSheet
            );
            var myCp = CPHelper.TotalCP(
                equipmentItems,
                costumeItems,
                myRuneOptions,
                myAvatarState.level,
                myCharacterRow,
                costumeStatSheet,
                collectionModifiers[myAvatarAddress],
                myRuneLevelBonus
            );

            return (states, myItemSlotState, myRuneSlotState, myRuneStates, myCp);
        }

        private (
            ItemSlotState ItemSlotState,
            RuneSlotState RuneSlotState,
            AllRuneState RuneStates
        ) PrepareEnemyLoadout(IWorldState states, Address enemyAvatarAddress)
        {
            var enemyItemSlotStateAddress = ItemSlotState.DeriveAddress(
                enemyAvatarAddress,
                BattleType.Arena
            );
            var enemyItemSlotState = states.TryGetLegacyState(
                enemyItemSlotStateAddress,
                out List rawEnemyItemSlotState
            )
                ? new ItemSlotState(rawEnemyItemSlotState)
                : new ItemSlotState(BattleType.Arena);

            var enemyRuneSlotStateAddress = RuneSlotState.DeriveAddress(
                enemyAvatarAddress,
                BattleType.Arena
            );
            var enemyRuneSlotState = states.TryGetLegacyState(
                enemyRuneSlotStateAddress,
                out List enemyRawRuneSlotState
            )
                ? new RuneSlotState(enemyRawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);

            var enemyRuneStates = states.GetRuneState(enemyAvatarAddress, out _);

            return (enemyItemSlotState, enemyRuneSlotState, enemyRuneStates);
        }

        private ArenaLog Simulate(
            IWorldState states,
            Dictionary<Type, (Address address, ISheet sheet)> sheets,
            AvatarState myAvatarState,
            IRandom random,
            GameConfigState gameConfigState,
            Dictionary<Address, List<StatModifier>> collectionModifiers,
            (
                ItemSlotState ItemSlotState,
                RuneSlotState RuneSlotState,
                AllRuneState RuneStates
            ) mySpec,
            (
                ItemSlotState ItemSlotState,
                RuneSlotState RuneSlotState,
                AllRuneState RuneStates
            ) enemySpec,
            Address myAvatarAddress,
            Address enemyAvatarAddress
        )
        {
            var myArenaPlayerDigest = new ArenaPlayerDigest(
                myAvatarState,
                mySpec.ItemSlotState.Equipments,
                mySpec.ItemSlotState.Costumes,
                mySpec.RuneStates,
                mySpec.RuneSlotState
            );
            var enemyAvatarState = states.GetEnemyAvatarState(enemyAvatarAddress);
            var enemyArenaPlayerDigest = new ArenaPlayerDigest(
                enemyAvatarState,
                enemySpec.ItemSlotState.Equipments,
                enemySpec.ItemSlotState.Costumes,
                enemySpec.RuneStates,
                enemySpec.RuneSlotState
            );

            var buffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
            var buffLinkSheet = sheets.GetSheet<BuffLinkSheet>();
            var simulator = new ArenaSimulator(
                random,
                5,
                gameConfigState.ShatterStrikeMaxDamage
            );
            return simulator.Simulate(
                myArenaPlayerDigest,
                enemyArenaPlayerDigest,
                sheets.GetArenaSimulatorSheets(),
                collectionModifiers[myAvatarAddress],
                collectionModifiers[enemyAvatarAddress],
                buffLimitSheet,
                buffLinkSheet,
                true
            );
        }
    }
}
