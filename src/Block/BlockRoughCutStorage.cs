using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    /// <summary> Used to store what is dropped from the quarry </summary>
    public class BlockRoughCutStorage : BlockGenericMultiblockPart
    {
        WorldInteraction[] interactions;
        readonly SimpleParticleProperties interactParticles = new SimpleParticleProperties()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(.5, .5, .5),
            MinQuantity = 5,
            AddQuantity = 20,
            GravityEffect = .9f,
            WithTerrainCollision = true,
            ParticleModel = EnumParticleModel.Quad,
            LifeLength = 2.5f,
            MinVelocity = new Vec3f(-0.4f, -0.4f, -0.4f),
            AddVelocity = new Vec3f(0.8f, 1.2f, 0.8f),
            MinSize = 0.1f,
            MaxSize = 0.4f,
            DieOnRainHeightmap = false
        };

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            var dict = new Dictionary<string, List<ItemStack>>()
            {
                {"polished", new List<ItemStack>()},
                {"rock", new List<ItemStack>()},
                {"stone", new List<ItemStack>()},
                {"brick", new List<ItemStack>()}
            };

            foreach (var obj in api.World.Collectibles)
            {
                if (obj is ItemSlabTool)
                {
                    string type = (obj as ItemSlabTool).GetToolType();
                    if (type != "")
                    {
                        dict[type].Add(new ItemStack(obj));
                    }
                }
            }

            interactions = new WorldInteraction[] {
                new WorldInteraction(){
                    ActionLangCode = Code.Domain + ":wi-stonestorage-polished",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = dict["polished"].ToArray()
                },
                new WorldInteraction(){
                    ActionLangCode = Code.Domain + ":wi-stonestorage-rock",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = dict["rock"].ToArray()
                },
                new WorldInteraction(){
                    ActionLangCode = Code.Domain + ":wi-stonestorage-stone",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = dict["stone"].ToArray()
                },
                new WorldInteraction(){
                    ActionLangCode = Code.Domain + ":wi-stonestorage-brick",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = dict["brick"].ToArray()
                }
            };
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            if (!base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack)) return false;

            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack != null)
            {
                BERoughCutStorage be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BERoughCutStorage;
                be.blockStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Clone();
                be.blockStack.StackSize = 1;
            }

            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var be = (world.BlockAccessor.GetBlockEntity(pos) as BEGenericMultiblockPart)?.Core;

            if (be is BERoughCutStorage rcbe && rcbe.blockStack.Attributes.GetInt("stonestored") > 0)
            {
                world.SpawnItemEntity(rcbe.blockStack.Clone(), pos.ToVec3d());
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var be = (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEGenericMultiblockPart)?.Core;
            if (!(be is BERoughCutStorage rcbe) || rcbe.blockStack == null) return false;


            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null)
            {
                if (rcbe.blockStack.Attributes.HasAttribute("stonestored"))
                {
                    if (world.Side == EnumAppSide.Client)
                    {
                        (byPlayer as IClientPlayer).ShowChatNotification("Contains: " + rcbe.blockStack.Attributes["stonestored"].ToString() + " stone");
                    }
                }

                return false;
            }

            if (rcbe.blockStack.Attributes.GetInt("stonestored") <= 0) return false;


            ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            AssetLocation dropCode = null;
            int dropRate = 0;

            if (activeStack.Collectible is ItemSlabTool tool)
            {
                var toolType = tool.GetToolType();
                if (toolType != "")
                {
                    dropCode = new AssetLocation("game", "rock-" + rcbe.blockStack.Block.FirstCodePart(1));
                    dropRate = activeStack.ItemAttributes[toolType + "rate"].AsInt();
                }
            }

            if (dropCode == null) return false;

            var colObj = (CollectibleObject)world.GetItem(dropCode) ?? world.GetBlock(dropCode);
            if (colObj == null)
            {
                (byPlayer as IServerPlayer)?.SendIngameError("", Lang.Get(Code.Domain + ":ingameerror-stonestorage-unknown-drop"));
                return true;
            }

            var stackSize = DropCount(activeStack.ItemAttributes["rchances"].AsInt(), dropRate, world.Rand);
            var dropStack = new ItemStack(colObj, stackSize);

            var dropPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
            var dropVel = new Vec3d(.05 * blockSel.Face.Normalf.ToVec3d().X, .1, .05 * blockSel.Face.Normalf.ToVec3d().Z);
            world.SpawnItemEntity(dropStack, dropPos, dropVel);

            rcbe.blockStack.Attributes.SetInt("stonestored", rcbe.blockStack.Attributes.GetInt("stonestored") - 1);
            if (rcbe.blockStack.Attributes.GetInt("stonestored") <= 0)
            {
                world.BlockAccessor.BreakBlock(blockSel.Position, byPlayer);
            }
            byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);


            if (world.Side == EnumAppSide.Client)
            {
                (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
            }
            interactParticles.ColorByBlock = world.BlockAccessor.GetBlock(blockSel.Position);
            interactParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
            world.SpawnParticles(interactParticles, byPlayer);
            world.PlaySoundAt(new AssetLocation("game", "sounds/block/heavyice"), byPlayer, byPlayer, true, 32, .5f);

            return true;
        }

        public int DropCount(int chances, int rate, Random rand) //TODO: Simplify and embeds it???
        {
            int rcount = 0;
            for (int i = 1; i <= chances; i++)
            {
                if (rand.Next(0, 100) <= rate)
                {
                    rcount += 1;
                }
            }
            return rcount;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            var count = inSlot.Itemstack.Attributes.GetInt("stonestored");
            var stone = Lang.Get("rock-" + FirstCodePart(1));
            dsc.AppendLine(Lang.Get(Code.Domain + ":info-stonestorage-heldinfo(count={0},stone={1})", count, stone));
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}