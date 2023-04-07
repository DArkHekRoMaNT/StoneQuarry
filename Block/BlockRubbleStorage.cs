using CommonLib.Utils;
using System;
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
    public class BlockRubbleStorage : MultiBlockBase
    {
        private Cuboidf[] _mirroredCollisionBoxes = null!;
        private SimpleParticleProperties? _interactParticles;
        private IRockManager _rockManager = null!;

        public SimpleParticleProperties InteractParticles
        {
            get => _interactParticles ??= new()
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
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            _rockManager = api.ModLoader.GetModSystem<RockManager>();

            _mirroredCollisionBoxes = new Cuboidf[CollisionBoxes.Length];
            for (int i = 0; i < CollisionBoxes.Length; i++)
            {
                _mirroredCollisionBoxes[i] = CollisionBoxes[i].RotatedCopy(0, 180, 0, new Vec3d(0.5, 0.5, 0.5));
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            int stackQuantity = 1;

            if (byPlayer.Entity.Controls.Sprint)
            {
                stackQuantity = byPlayer.InventoryManager.ActiveHotbarSlot.MaxSlotStackSize;
            }

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BERubbleStorage be)
            {
                return true;
            }

            string? selectedType = (RubbleStorageSelectType)blockSel.SelectionBoxIndex switch
            {
                RubbleStorageSelectType.None => "none",
                RubbleStorageSelectType.Stone => "stone",
                RubbleStorageSelectType.Gravel => "gravel",
                RubbleStorageSelectType.Sand => "sand",
                _ => throw new NotImplementedException()
            };

            var activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

            if (selectedType != "none")
            {
                if (byPlayer.Entity.Controls.Sneak)
                {
                    ToggleLock();
                }
                else
                {
                    TryGetResource();
                }
            }
            else if (activeStack != null)
            {
                TryUseRubbleHammer();
                TryMakeMuddyGravel();
                TryAddResource();
            }
            else
            {
                TryTakeAll();
            }

            be.UpdateDisplayedType();

            return true;

            void ToggleLock()
            {
                if (be.LockedType != (RubbleStorageSelectType)blockSel.SelectionBoxIndex)
                {
                    be.LockedType = (RubbleStorageSelectType)blockSel.SelectionBoxIndex;
                }
                else be.LockedType = RubbleStorageSelectType.None;
            }

            void TryGetResource()
            {
                if (be.TryRemoveResource(world, blockSel, selectedType, stackQuantity))
                {
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                    world.PlaySoundAt(SQSounds.StoneCrush, byPlayer, byPlayer, true);

                    if (be.Inventory?.StoredRock != null)
                    {
                        InteractParticles.ColorByBlock = world.GetBlock(be.Inventory.StoredRock);
                    }

                    InteractParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                    world.SpawnParticles(InteractParticles, byPlayer);
                }
            }

            void TryUseRubbleHammer()
            {
                if (activeStack.ItemAttributes?["rubbleable"]?.AsBool() ?? false)
                {
                    if (be.TryDegrade())
                    {
                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        world.PlaySoundAt(SQSounds.Crack, byPlayer, byPlayer, true, volume: 0.25f);

                        activeStack.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot);
                    }
                }
            }

            void TryMakeMuddyGravel()
            {
                ITreeAttribute? contents = activeStack.Attributes.GetTreeAttribute("contents");
                CollectibleObject? portion = contents?.GetItemstack("0")?.Collectible;
                if (portion != null && portion.Code.Equals(new AssetLocation("game:waterportion")))
                {
                    if (be.TryDrench(world, blockSel, byPlayer))
                    {
                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        world.PlaySoundAt(SQSounds.WaterSplash, byPlayer, byPlayer, true);
                    }
                }
            }

            void TryAddResource()
            {
                if (be.TryAddResource(byPlayer.InventoryManager.ActiveHotbarSlot, stackQuantity))
                {
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                    world.PlaySoundAt(SQSounds.StoneCrush, byPlayer, byPlayer, true);
                }
            }

            void TryTakeAll()
            {
                if (blockSel.SelectionBoxIndex == 0 && activeStack == null)
                {
                    if (be.TryAddAll(byPlayer))
                    {
                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        world.PlaySoundAt(SQSounds.StoneCrush, byPlayer, byPlayer, true);
                    }
                }
            }
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer? byPlayer, BlockSelection blockSel, ItemStack? byItemStack)
        {
            if (base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack))
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BERubbleStorage be)
                {
                    if (byItemStack != null)
                    {
                        be.SetDataFromStack(byItemStack);
                    }

                    be.UpdateDisplayedType();
                    return true;
                }
            }

            return false;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            RubbleStorageInventory inv = new(api);
            inv.FromTreeAttributes(inSlot.Itemstack.Attributes);

            if (inv.StoredRock == null)
            {
                dsc.AppendLine(Lang.Get(Core.ModId + ":info-rubblestorage-empty"));
            }
            else
            {
                string stoneLangCode = $"{Core.ModId}:info-rubblestorage-stone(count={{0}})";
                string gravelLangCode = $"{Core.ModId}:info-rubblestorage-gravel(count={{0}})";
                string sandLangCode = $"{Core.ModId}:info-rubblestorage-sand(count={{0}})";

                string rockName = Lang.Get(inv.StoredRock.ToString());

                dsc.AppendLine(Lang.Get($"{Core.ModId}:info-rubblestorage-type(type={{0}})", rockName));
                dsc.AppendLine(Lang.Get(stoneLangCode, inv.StoneSlot.StackSize));
                dsc.AppendLine(Lang.Get(gravelLangCode, inv.GravelSlot.StackSize));
                dsc.AppendLine(Lang.Get(sandLangCode, inv.SandSlot.StackSize));
            }
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer)
        {
            string type = (RubbleStorageSelectType)blockSel.SelectionBoxIndex switch
            {
                RubbleStorageSelectType.None => "none",
                RubbleStorageSelectType.Stone => "stone",
                RubbleStorageSelectType.Gravel => "gravel",
                RubbleStorageSelectType.Sand => "sand",
                _ => throw new NotImplementedException()
            };

            return ObjectCacheUtil.GetOrCreate(world.Api, $"{Core.ModId}-rubblestorage-wi-{type}", () =>
            {
                if (type == "none")
                {
                    return new WorldInteraction[]
                    {
                        new WorldInteraction()
                        {
                            ActionLangCode = $"{Core.ModId}:wi-rubblestorage-hammer",
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = GetRubbleHammers()
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = $"{Core.ModId}:wi-rubblestorage-add-one",
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = GetAvailableContent("stone"),
                            GetMatchingStacks = GetMatchingStacks_StoneType
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = $"{Core.ModId}:wi-rubblestorage-add-stack",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sprint",
                            Itemstacks = GetAvailableContent("stone", true),
                            GetMatchingStacks = GetMatchingStacks_StoneType
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = $"{Core.ModId}:wi-rubblestorage-add-all",
                            MouseButton = EnumMouseButton.Right,
                            RequireFreeHand = true
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = $"{Core.ModId}:wi-rubblestorage-water",
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = GetWaterPortionStacks()
                        }
                    };
                }
                else
                {
                    return new WorldInteraction[]
                    {
                        new WorldInteraction()
                        {
                            ActionLangCode = $"{Core.ModId}:wi-rubblestorage-take-one-{type}",
                            MouseButton = EnumMouseButton.Right
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = $"{Core.ModId}:wi-rubblestorage-take-stack-{type}",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sprint"
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = $"{Core.ModId}:wi-rubblestorage-lock",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak"
                        }
                    };
                }
            }).Append(base.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer));

            ItemStack[] GetWaterPortionStacks()
            {
                List<ItemStack> waterPortion = api.World
                    .GetItem(new AssetLocation("game:waterportion"))
                    .GetHandBookStacks(api as ICoreClientAPI);

                foreach (ItemStack stack in waterPortion)
                {
                    stack.StackSize = 100; // 1 liter of liquid
                }

                return waterPortion.ToArray();
            }

            ItemStack[] GetRubbleHammers()
            {
                var hammers = new List<ItemStack>();
                foreach (var colObj in api.World.Collectibles)
                {
                    if (colObj.Attributes != null && colObj.Attributes["rubbleable"].Exists)
                    {
                        hammers.Add(new ItemStack(colObj));
                    }
                }
                return hammers.ToArray();
            }

            ItemStack[] GetAvailableContent(string type, bool fullStack = false)
            {
                var content = new List<ItemStack>();
                foreach (RockData rockData in _rockManager.Data)
                {
                    AssetLocation? code = rockData[type];
                    if (code != null)
                    {
                        var colObj = api.World.GetCollectibleObject(code);
                        if (colObj != null)
                        {
                            var stack = new ItemStack(colObj);
                            if (fullStack)
                            {
                                stack.StackSize = colObj.MaxStackSize;
                            }
                            content.Add(stack);
                        }
                    }
                }
                return content.ToArray();
            }

            ItemStack[] GetMatchingStacks_StoneType(WorldInteraction wi, BlockSelection subBlockSel, EntitySelection entitySel)
            {
                var be = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BERubbleStorage;
                if (be?.Inventory?.StoredRock != null)
                {
                    RockData? data = _rockManager.GetValue(be.Inventory.StoredRock);

                    if (data != null)
                    {
                        return wi.Itemstacks
                            .Where((item) => data[item.Collectible.Code] != null)
                            .ToArray();
                    }
                }
                return wi.Itemstacks;
            }
        }

        public override ItemStack[]? GetDrops(IWorldAccessor world, BlockPos pos, IPlayer? byPlayer, float dropQuantityMultiplier = 1)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BERubbleStorage be)
            {
                return new ItemStack[] { be.GetSelfStack() };
            }

            return null;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BERubbleStorage be)
            {
                return be.GetSelfStack();
            }

            return base.OnPickBlock(world, pos);
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            if (blockAccessor.GetBlockEntity(pos) is BERubbleStorage be)
            {
                Cuboidf[] collision = new Cuboidf[CollisionBoxes.Length];
                CollisionBoxes.CopyTo(collision, 0);
                collision[0].Y2 = 0.8f - 0.06f * be.CurrentContentLevel;
                return collision;
            }

            return base.GetCollisionBoxes(blockAccessor, pos);
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            if (blockAccessor.GetBlockEntity(pos) is BERubbleStorage be
                && be.Inventory?.StoredRock == null)
            {
                return new Cuboidf[] { SelectionBoxes[0] };
            }

            return base.GetSelectionBoxes(blockAccessor, pos);
        }

        public override Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        {
            if (blockAccessor.GetBlockEntity(pos + offset.AsBlockPos) is BERubbleStorage be)
            {
                Cuboidf[] collision = new Cuboidf[_mirroredCollisionBoxes.Length];
                _mirroredCollisionBoxes.CopyTo(collision, 0);
                collision[0].Y2 = 0.8f - 0.06f * be.CurrentContentLevel;
                return collision;
            }

            return _mirroredCollisionBoxes;
        }

        public override Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        {
            return new Cuboidf[] { SelectionBoxes[0].RotatedCopy(0, 180, 0, new Vec3d(0.5, 0.5, 0.5)) };
        }
    }
}
