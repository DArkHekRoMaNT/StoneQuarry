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
            LifeLength = 0.5f,
            MinVelocity = new Vec3f(-0.4f, -0.4f, -0.4f),
            AddVelocity = new Vec3f(0.8f, 1.2f, 0.8f),
            MinSize = 0.1f,
            MaxSize = 0.4f,
            DieOnRainHeightmap = false
        };
        float hitTime = .2f;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            var dict = new Dictionary<string, List<ItemStack>>()
            {
                {"rockpolished", new List<ItemStack>()},
                {"rock", new List<ItemStack>()},
                {"stone", new List<ItemStack>()},
                {"stonebrick", new List<ItemStack>()}
            };

            foreach (var collObj in api.World.Collectibles)
            {
                if (collObj is ItemSlabTool)
                {
                    string type = (collObj as ItemSlabTool).ToolType;
                    if (type != "")
                    {
                        dict[type].Add(new ItemStack(collObj));
                    }
                }
            }

            interactions = new WorldInteraction[] {
                new WorldInteraction(){
                    ActionLangCode = Code.Domain + ":wi-stonestorage-rockpolished",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = dict["rockpolished"].ToArray()
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
                    ActionLangCode = Code.Domain + ":wi-stonestorage-stonebrick",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = dict["stonebrick"].ToArray()
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

            if (be is BERoughCutStorage rcbe && rcbe.blockStack?.Attributes.GetInt("stonestored") > 0)
            {
                world.SpawnItemEntity(rcbe.blockStack.Clone(), pos.ToVec3d());
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var be = (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEGenericMultiblockPart)?.Core;
            if (!(be is BERoughCutStorage rcbe) || rcbe.blockStack == null) return false;

            ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (activeStack == null)
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

            if (rcbe.blockStack.Attributes.GetInt("stonestored") > 0 && activeStack.Collectible is ItemSlabTool tool)
            {
                return true;
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (activeStack.Collectible is ItemSlabTool tool)
            {
                if (api.Side == EnumAppSide.Client)
                {

                    if (activeStack != null && activeStack.Collectible.FirstCodePart() == "rubblehammer")
                    {
                        ModelTransform tf = ModelTransform.NoTransform;

                        float offset = secondsUsed / Core.Config.SlabInteractionTime;
                        tf.Translation.Set(offset * .25f, 0, offset * .5f);

                        byPlayer.Entity.Controls.UsingHeldItemTransformBefore = tf;
                    }
                    else
                    {
                        ModelTransform tf = ModelTransform.NoTransform;
                        tf.Translation.Set(secondsUsed % hitTime, 0, 0);
                        byPlayer.Entity.Controls.UsingHeldItemTransformBefore = tf;

                        int times = byPlayer.Entity.WatchedAttributes.GetInt("sq_slab_times", 1);

                        if (secondsUsed > times * hitTime)
                        {
                            interactParticles.ColorByBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                            interactParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                            world.SpawnParticles(interactParticles, byPlayer);

                            world.PlaySoundAt(new AssetLocation("game", "sounds/block/rock-hit-pickaxe"),
                                byPlayer, byPlayer, true, 32, .5f);

                            byPlayer.Entity.WatchedAttributes.SetInt("sq_slab_times", times + 1);
                        }
                    }
                }

                return secondsUsed < Core.Config.SlabInteractionTime;
            }

            return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (api.Side == EnumAppSide.Client)
            {
                byPlayer.Entity.WatchedAttributes.SetInt("sq_slab_times", 1);
            }

            ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            var tool = activeStack.Collectible as ItemSlabTool;

            if (secondsUsed < Core.Config.SlabInteractionTime || tool == null)
            {
                base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
                return;
            }


            var be = (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEGenericMultiblockPart)?.Core;
            if (!(be is BERoughCutStorage rcbe) || rcbe.blockStack == null) return;

            AssetLocation dropCode = null;
            int dropQuantity = 0;

            var toolType = tool.ToolType;
            if (toolType != "")
            {
                dropCode = new AssetLocation("game", toolType + "-" + rcbe.blockStack.Block.FirstCodePart(1));

                // Like in Vintagestory.API.Common.BlockDropItemStack.GetNextItemStack
                float num = (activeStack.Item as ItemSlabTool).Quantity.nextFloat(1, world.Rand);
                dropQuantity = (int)num + (((double)(num - (float)(int)num) > world.Rand.NextDouble()) ? 1 : 0);
            }

            if (dropCode == null) return;

            var colObj = (CollectibleObject)world.GetItem(dropCode) ?? world.GetBlock(dropCode);
            if (colObj == null)
            {
                (byPlayer as IServerPlayer)?.SendIngameError("", Lang.Get(Code.Domain + ":ingameerror-stonestorage-unknown-drop"));
                return;
            }

            var dropStack = new ItemStack(colObj, dropQuantity);

            var dropPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
            var dropVel = new Vec3d(.05 * blockSel.Face.Normalf.ToVec3d().X, .1, .05 * blockSel.Face.Normalf.ToVec3d().Z);

            if (activeStack != null && activeStack.Collectible.FirstCodePart() == "rubblehammer")
            {
                interactParticles.ColorByBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                interactParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                world.SpawnParticles(interactParticles, byPlayer);
                world.PlaySoundAt(new AssetLocation("game", "sounds/block/rock-hit-pickaxe"),
                    byPlayer, byPlayer, true, 32, .5f);
            }
            world.PlaySoundAt(new AssetLocation("game", "sounds/block/heavyice"),
                byPlayer, byPlayer, true, 32, .05f);

            world.SpawnItemEntity(dropStack, dropPos, dropVel);

            rcbe.blockStack.Attributes.SetInt("stonestored", rcbe.blockStack.Attributes.GetInt("stonestored") - 1);
            if (rcbe.blockStack.Attributes.GetInt("stonestored") <= 0 && api.Side == EnumAppSide.Server)
            {
                world.BlockAccessor.BreakBlock(blockSel.Position, byPlayer);
            }
            byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);
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