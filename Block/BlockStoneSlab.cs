using CommonLib.Config;
using CommonLib.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    public class BlockStoneSlab : MultiBlockBase
    {
        private IRockManager _rockManager = null!;
        private StoneSlabMeshCache _meshCache = null!;
        private Config _config = null!;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            _meshCache = api.ModLoader.GetModSystem<StoneSlabMeshCache>();
            _rockManager = api.ModLoader.GetModSystem<RockManager>();
            _config = api.ModLoader.GetModSystem<ConfigManager>().GetConfig<Config>();
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            if (!base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack))
            {
                return false;
            }

            if (byItemStack is not null)
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEStoneSlab be)
                {
                    be.ContentFromAttributes(byItemStack.Attributes.Clone(), world);
                }
            }

            return true;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEStoneSlab be)
            {
                if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative && activeStack is not null)
                {
                    if (byPlayer.Entity.Controls.Sprint)
                    {
                        be.Inventory?.TryRemoveStack(activeStack);
                        return true;
                    }
                    else if (_rockManager.IsSuitableRock(activeStack.Collectible.Code))
                    {
                        be.Inventory?.TryAddStack(activeStack);
                        return true;
                    }
                }

                if (be.Inventory != null && !be.Inventory.Empty &&
                    activeStack?.Collectible?.Attributes?["slabtool"] is not null)
                {
                    return true;
                }

                if (byPlayer.Entity.Controls.Sprint)
                {
                    be.Inventory?.NextSlot();
                    return true;
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            return MBOnBlockInteractStep(secondsUsed, world, byPlayer, blockSel, Vec3i.Zero);
        }

        public override bool MBOnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset)
        {
            ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (activeStack?.Collectible?.Attributes?["slabtool"] != null)
            {
                if (api.Side == EnumAppSide.Client)
                {
                    if (activeStack.Collectible.FirstCodePart() == "rubblehammer")
                    {
                        RubbleHammerTransform();
                    }
                    else
                    {
                        ChiselsTransform();
                    }
                }

                return secondsUsed < _config.SlabInteractionTime;
            }

            return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);

            void ChiselsTransform()
            {
                BlockPos pos = blockSel.Position + offset.AsBlockPos;
                if (world.BlockAccessor.GetBlockEntity(pos) is BEStoneSlab be)
                {
                    float hitTime = 0.2f;
                    var tf = ModelTransform.NoTransform;
                    tf.Translation.Set(secondsUsed % hitTime, 0, 0);
                    byPlayer.Entity.Controls.UsingHeldItemTransformBefore = tf;

                    int times = byPlayer.Entity.WatchedAttributes.GetInt("sq_slab_times", 1);
                    if (secondsUsed > times * hitTime)
                    {
                        be.InteractParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                        world.SpawnParticles(be.InteractParticles, byPlayer);
                        world.PlaySoundAt(SQSounds.RockHit, byPlayer, byPlayer, true, 32, .5f);
                        byPlayer.Entity.WatchedAttributes.SetInt("sq_slab_times", times + 1);
                    }
                }
            }

            void RubbleHammerTransform()
            {
                var tf = ModelTransform.NoTransform;
                float tfOffset = secondsUsed / _config.SlabInteractionTime;
                tf.Translation.Set(tfOffset * .25f, 0, tfOffset * .5f);
                byPlayer.Entity.Controls.UsingHeldItemTransformBefore = tf;
            }
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            MBOnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, Vec3i.Zero);
        }

        public override void MBOnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset)
        {
            if (api.Side == EnumAppSide.Client)
            {
                byPlayer.Entity.WatchedAttributes.SetInt("sq_slab_times", 1);
            }

            ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            JsonObject? slabtool = activeStack?.Collectible.Attributes["slabtool"];
            if (secondsUsed < _config.SlabInteractionTime || slabtool == null)
            {
                base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
                return;
            }

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position + offset.AsBlockPos) is BEStoneSlab be)
            {
                if (be.Inventory?.Empty is not true)
                {
                    RubbleHammerHit();
                    DropItem();
                }
            }

            void RubbleHammerHit()
            {
                if (activeStack?.Collectible.FirstCodePart() == "rubblehammer")
                {
                    be.InteractParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                    world.SpawnParticles(be.InteractParticles, byPlayer);
                    world.PlaySoundAt(SQSounds.RockHit, byPlayer, byPlayer, true, 32, .5f);
                }
            }

            void DropItem()
            {
                if (api.Side == EnumAppSide.Server)
                {
                    string? dropType = slabtool["type"]?.AsString();
                    NatFloat quantity = slabtool["quantity"]?.AsObject<NatFloat>() ?? NatFloat.One;

                    if (dropType != null)
                    {
                        var dropStack = be.Inventory!.GetContent(byPlayer, dropType, quantity);
                        if (dropStack != null)
                        {
                            var dropPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                            var dropVel = new Vec3d(.05 * blockSel.Face.Normalf.ToVec3d().X, .1, .05 * blockSel.Face.Normalf.ToVec3d().Z);

                            world.PlaySoundAt(SQSounds.Crack, byPlayer, byPlayer, true, 32, .05f);

                            world.SpawnItemEntity(dropStack, dropPos, dropVel);

                            if (be.Inventory!.Empty)
                            {
                                world.BlockAccessor.BreakBlock(blockSel.Position, byPlayer);
                            }

                            activeStack?.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);
                        }
                    }
                }
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            string langKey = Core.ModId + ":info-stoneslab-heldinfo(count={0},stone={1})";

            int qslots = inSlot.Itemstack.Attributes.GetInt("qslots", 0);
            if (qslots > 0)
            {
                var tree = inSlot.Itemstack.Attributes.GetTreeAttribute("slots");
                for (int i = 0; i < qslots; i++)
                {
                    var stack = tree.GetItemstack(i + "");
                    if (stack?.StackSize > 0)
                    {
                        stack.ResolveBlockOrItem(world);
                        string rock = Lang.Get(stack.Collectible.Code.ToString());
                        dsc.AppendLine(Lang.Get(langKey, stack.StackSize, rock));
                    }
                }
            }
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return ObjectCacheUtil.GetOrCreate(world.Api, $"{Core.ModId}-wi-stoneslab-base", () => new[]
            {
                new WorldInteraction(){
                    ActionLangCode = Core.ModId + ":wi-stoneslab-rockpolished",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = GetTools("rockpolished")
                },
                new WorldInteraction(){
                    ActionLangCode = Core.ModId + ":wi-stoneslab-rock",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = GetTools("rock")
                },
                new WorldInteraction(){
                    ActionLangCode = Core.ModId + ":wi-stoneslab-stone",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = GetTools("stone")
                },
                new WorldInteraction(){
                    ActionLangCode = Core.ModId + ":wi-stoneslab-stonebrick",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks =  GetTools("stonebrick")
                },
                new WorldInteraction()
                {
                    ActionLangCode = Core.ModId + ":wi-stoneslab-changerock",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sprint"
                }
            }).AppendIf(forPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative,
                ObjectCacheUtil.GetOrCreate(world.Api, $"{Core.ModId}-wi-stoneslab-creative", () =>
                {
                    ItemStack[] rocks = _rockManager.Data
                        .Select((data) =>
                        {
                            Block block = api.World.GetBlock(data.Rock) ?? api.World.GetBlock(0);
                            return new ItemStack(block);
                        })
                        .ToArray();

                    return new WorldInteraction[]
                        {
                            new WorldInteraction()
                            {
                                ActionLangCode = Core.ModId + ":wi-stoneslab-addrock",
                                MouseButton = EnumMouseButton.Right,
                                Itemstacks = rocks
                            },
                            new WorldInteraction()
                            {
                                ActionLangCode = Core.ModId + ":wi-stoneslab-removerock",
                                MouseButton = EnumMouseButton.Right,
                                HotKeyCode = "sprint",
                                Itemstacks = rocks
                            }
                    };
                })
            ).Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));

            ItemStack[] GetTools(string type)
            {
                var tools = new List<ItemStack>();

                foreach (CollectibleObject collObj in api.World.Collectibles)
                {
                    if (collObj?.Attributes?.KeyExists("slabtool") == true)
                    {
                        if (collObj.Attributes["slabtool"]["type"]?.AsString() == type)
                        {
                            tools.Add(new ItemStack(collObj));
                        }
                    }
                }

                return tools.ToArray();
            }
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new[] { OnPickBlock(world, pos) };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos) as BEStoneSlab;
            ItemStack? stack = be?.GetSelfDrop();
            return stack ?? base.OnPickBlock(world, pos);
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
            renderinfo.ModelRef = _meshCache.GetInventoryMeshRef(itemstack, this);
        }
    }
}
