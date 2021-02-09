using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Serilog;
using System;
using System.Collections.Generic;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Headless.GraphTypes
{
    public class ActionMutation : ObjectGraphType<NineChroniclesNodeService>
    {
        public ActionMutation()
        {
            Field<NonNullGraphType<BooleanGraphType>>("createAvatar",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "avatarName",
                        Description = "The character name."
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "avatarIndex",
                        Description = "The index of character slot. 0 ~ 2"
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "hairIndex",
                        Description = "The index of character hair color. 0 ~ 8"
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "lensIndex",
                        Description = "The index of character eye color. 0 ~ 8"
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "earIndex",
                        Description = "The index of character ear color. 0 ~ 8"
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "tailIndex",
                        Description = "The index of character tail color. 0 ~ 8"
                    }
                ),
                resolve: context =>
                {
                    try
                    {
                        NineChroniclesNodeService service = context.Source;
                        PrivateKey privatekey = service.PrivateKey;
                        BlockChain<NineChroniclesActionType> blockChain = service.Swarm.BlockChain;
                        Address userAddress = privatekey.PublicKey.ToAddress();
                        var avatarName = context.GetArgument<string>("avatarName");
                        var avatarIndex = context.GetArgument<int>("avatarIndex");
                        var hairIndex = context.GetArgument<int>("hairIndex");
                        var lensIndex = context.GetArgument<int>("lensIndex");
                        var earIndex = context.GetArgument<int>("earIndex");
                        var tailIndex = context.GetArgument<int>("tailIndex");
                        var action = new CreateAvatar2
                        {
                            index = avatarIndex,
                            hair = hairIndex,
                            lens = lensIndex,
                            ear = earIndex,
                            tail = tailIndex,
                            name = avatarName,
                        };

                        var actions = new PolymorphicAction<ActionBase>[] { action };
                        blockChain.MakeTransaction(privatekey, actions);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unexpected exception occurred during {typeof(ActionMutation)}: {e}";
                        context.Errors.Add(new ExecutionError(msg, e));
                        Log.Error(msg, e);
                        return false;
                    }

                    return true;
                });

            Field<NonNullGraphType<BooleanGraphType>>("hackAndSlash",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "weeklyArenaAddress",
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "rankingArenaAddress",
                    }),
                resolve: context =>
                {
                    try
                    {
                        NineChroniclesNodeService service = context.Source;
                        PrivateKey privatekey = service.PrivateKey;
                        BlockChain<NineChroniclesActionType> blockChain = service.Swarm.BlockChain;
                        Address userAddress = privatekey.PublicKey.ToAddress();
                        Address avatarAddress = userAddress.Derive("avatar_0");
                        Address weeklyArenaAddress = new Address(context.GetArgument<string>("weeklyArenaAddress"));
                        Address rankingArenaAddress = new Address(context.GetArgument<string>("rankingArenaAddress"));

                        var action = new HackAndSlash
                        {
                            avatarAddress = avatarAddress,
                            worldId = 1,
                            stageId = 1,
                            WeeklyArenaAddress = weeklyArenaAddress,
                            RankingMapAddress = rankingArenaAddress,
                            costumes = new List<int>(),
                            equipments = new List<Guid>(),
                            foods = new List<Guid>(),
                        };

                        var actions = new PolymorphicAction<ActionBase>[] { action };
                        blockChain.MakeTransaction(privatekey, actions);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unexpected exception occurred during {typeof(ActionMutation)}: {e}";
                        context.Errors.Add(new ExecutionError(msg, e));
                        Log.Error(msg, e);
                        return false;
                    }

                    return true;
                });

            Field<NonNullGraphType<BooleanGraphType>>("combinationEquipment",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<DecimalGraphType>>
                    {
                        Name = "recipeId",
                    },
                    new QueryArgument<NonNullGraphType<DecimalGraphType>>
                    {
                        Name = "slotIndex",
                    },
                    new QueryArgument<DecimalGraphType>
                    {
                        Name = "subRecipeId",
                    }),
                resolve: context =>
                {
                    try
                    {
                        NineChroniclesNodeService service = context.Source;
                        PrivateKey privatekey = service.PrivateKey;
                        BlockChain<NineChroniclesActionType> blockChain = service.Swarm.BlockChain;
                        Address userAddress = privatekey.PublicKey.ToAddress();
                        Address avatarAddress = userAddress.Derive("avatar_0");
                        int recipeId = context.GetArgument<int>("recipeId");
                        int slotIndex = context.GetArgument<int>("slotIndex");
                        int? subRecipeId = context.GetArgument<int>("subRecipeId");

                        var action = new CombinationEquipment
                        {
                            AvatarAddress = avatarAddress,
                            RecipeId = recipeId,
                            SlotIndex = slotIndex,
                            SubRecipeId = subRecipeId
                        };

                        var actions = new PolymorphicAction<ActionBase>[] { action };
                        blockChain.MakeTransaction(privatekey, actions);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unexpected exception occurred during {typeof(ActionMutation)}: {e}";
                        context.Errors.Add(new ExecutionError(msg, e));
                        Log.Error(msg, e);
                        return false;
                    }

                    return true;
                });

            Field<NonNullGraphType<BooleanGraphType>>("itemEnhancement",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "itemId",
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "materialIds",
                    }),
                resolve: context =>
                {
                    try
                    {
                        NineChroniclesNodeService service = context.Source;
                        PrivateKey privatekey = service.PrivateKey;
                        BlockChain<NineChroniclesActionType> blockChain = service.Swarm.BlockChain;
                        Address userAddress = privatekey.PublicKey.ToAddress();
                        Address avatarAddress = userAddress.Derive("avatar_0");
                        Guid itemId = Guid.Parse(context.GetArgument<string>("itemId"));
                        Guid materialId = Guid.Parse(context.GetArgument<string>("materialIds"));

                        var action = new ItemEnhancement
                        {
                            avatarAddress = avatarAddress,
                            slotIndex = 0,
                            itemId = itemId,
                            materialIds = new[] { materialId }
                        };

                        var actions = new PolymorphicAction<ActionBase>[] { action };
                        blockChain.MakeTransaction(privatekey, actions);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unexpected exception occurred during {typeof(ActionMutation)}: {e}";
                        context.Errors.Add(new ExecutionError(msg, e));
                        Log.Error(msg, e);
                        return false;
                    }

                    return true;
                });

            Field<NonNullGraphType<BooleanGraphType>>("buy",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "sellerAgentAddress",
                    },
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "sellerAvatarAddress",
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "buyerAvatarIndex",
                        Description = "The index of character slot. 0 ~ 2"
                    },
                    new QueryArgument<NonNullGraphType<GuidGraphType>>
                    {
                        Name = "productId",
                    }),
                resolve: context =>
                {
                    try
                    {
                        NineChroniclesNodeService service = context.Source;
                        PrivateKey privateKey = service.PrivateKey;
                        BlockChain<NineChroniclesActionType> blockChain = service.Swarm.BlockChain;
                        Address userAddress = privateKey.PublicKey.ToAddress();
                        int buyerAvatarIndex = context.GetArgument<int>("buyerAvatarIndex");
                        Address avatarAddress = userAddress.Derive(string.Format(CreateAvatar2.DeriveFormat, buyerAvatarIndex));
                        Address sellerAgentAddress = context.GetArgument<Address>("sellerAgentAddress");
                        Address sellerAvatarAddress = context.GetArgument<Address>("sellerAvatarAddress");
                        Guid productId = context.GetArgument<Guid>("productId");

                        var action = new Buy4
                        {
                            buyerAvatarAddress = avatarAddress,
                            sellerAgentAddress = sellerAgentAddress,
                            sellerAvatarAddress = sellerAvatarAddress,
                            productId = productId,
                        };

                        var actions = new PolymorphicAction<ActionBase>[] { action };
                        blockChain.MakeTransaction(privateKey, actions);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unexpected exception occurred during {typeof(ActionMutation)}: {e}";
                        context.Errors.Add(new ExecutionError(msg, e));
                        Log.Error(msg, e);
                        return false;
                    }

                    return true;
                });
            Field<NonNullGraphType<BooleanGraphType>>("sell",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "sellerAvatarAddress",
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "productId",
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "itemId",
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "price",
                    }),
                resolve: context =>
                {
                    try
                    {
                        NineChroniclesNodeService service = context.Source;
                        PrivateKey privatekey = service.PrivateKey;
                        BlockChain<NineChroniclesActionType> blockChain = service.Swarm.BlockChain;
                        Address sellerAvatarAddress = new Address(context.GetArgument<string>("sellerAvatarAddress"));
                        Guid itemId = Guid.Parse(context.GetArgument<string>("itemId"));
                        var currency = new GoldCurrencyState(
                            (Dictionary)blockChain.GetState(GoldCurrencyState.Address)
                        ).Currency;
                        FungibleAssetValue price =
                            FungibleAssetValue.Parse(currency, context.GetArgument<string>("price"));

                        var action = new Sell
                        {
                            sellerAvatarAddress = sellerAvatarAddress,
                            itemId = itemId,
                            price = price
                        };

                        var actions = new PolymorphicAction<ActionBase>[] { action };
                        blockChain.MakeTransaction(privatekey, actions);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unexpected exception occurred during {typeof(ActionMutation)}: {e}";
                        context.Errors.Add(new ExecutionError(msg, e));
                        Log.Error(msg, e);
                        return false;
                    }

                    return true;
                });

            Field<NonNullGraphType<BooleanGraphType>>("dailyReward",
                resolve: context =>
                {
                    try
                    {
                        NineChroniclesNodeService service = context.Source;
                        PrivateKey privatekey = service.PrivateKey;
                        BlockChain<NineChroniclesActionType> blockChain = service.Swarm.BlockChain;
                        Address userAddress = privatekey.PublicKey.ToAddress();
                        Address avatarAddress = userAddress.Derive("avatar_0");

                        var action = new DailyReward
                        {
                            avatarAddress = avatarAddress
                        };

                        var actions = new PolymorphicAction<ActionBase>[] { action };
                        blockChain.MakeTransaction(privatekey, actions);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unexpected exception occurred during {typeof(ActionMutation)}: {e}";
                        context.Errors.Add(new ExecutionError(msg, e));
                        Log.Error(msg, e);
                        return false;
                    }

                    return true;
                });
        }
    }
}
