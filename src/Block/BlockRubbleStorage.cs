using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace StoneQuarry
{
    public class BlockRubbleStorage : Block, IMultiBlockColSelBoxes
    {
        public static AssetLocation InteractSoundLocation => new("game", "sounds/block/heavyice");
        public static AssetLocation StoneCrushSoundLocation => new("game", "sounds/effect/stonecrush");
        public static AssetLocation WaterSplashSoundLocation => new("game", "sounds/environment/largesplash1");

        public List<WorldInteraction[]>? WorldInteractionsBySel { get; private set; }
        public SimpleParticleProperties InteractParticles { get; private set; }

        private Cuboidf[] _mirroredCollisionBoxes;

#nullable disable
        public IRockManager rockManager;
#nullable restore

        public BlockRubbleStorage()
        {
            InteractParticles = new SimpleParticleProperties()
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

            _mirroredCollisionBoxes = default!;
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            rockManager = api.ModLoader.GetModSystem<RockManager>();

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


            string? selectedType = null;
            switch ((EnumStorageLock)blockSel.SelectionBoxIndex)
            {
                case EnumStorageLock.Stone: selectedType = "stone"; break;
                case EnumStorageLock.Gravel: selectedType = "gravel"; break;
                case EnumStorageLock.Sand: selectedType = "sand"; break;
            }

            var activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

            // Player is looking at one of the buttons on the crate
            if (selectedType != null)
            {
                // Player try lock a resource type
                if (byPlayer.Entity.Controls.Sneak)
                {
                    if (be.StorageLock != (EnumStorageLock)blockSel.SelectionBoxIndex)
                    {
                        be.StorageLock = (EnumStorageLock)blockSel.SelectionBoxIndex;
                    }
                    else be.StorageLock = EnumStorageLock.None;
                }

                // Player try get a resource
                else if (be.TryRemoveResource(world, blockSel, selectedType, stackQuantity))
                {
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                    world.PlaySoundAt(StoneCrushSoundLocation, byPlayer, byPlayer, true);

                    if (be.Inventory?.StoredRock != null)
                    {
                        InteractParticles.ColorByBlock = world.GetBlock(be.Inventory.StoredRock);
                    }
                    InteractParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                    world.SpawnParticles(InteractParticles, byPlayer);
                }
            }

            // Player try use a tool or add a resource
            else if (activeStack != null)
            {
                // Rubble hammer
                if (activeStack.ItemAttributes?["rubbleable"]?.AsBool() ?? false)
                {
                    if (be.TryDegrade())
                    {
                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        world.PlaySoundAt(InteractSoundLocation, byPlayer, byPlayer, true, volume: 0.25f);

                        activeStack.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot);
                    }
                }

                // Liquid container with water
                else if (activeStack.Attributes.GetTreeAttribute("contents")?.GetItemstack("0")?.Collectible.Code
                    .Equals(new AssetLocation("game:waterportion")) ?? false)
                {
                    if (be.TryDrench(world, blockSel, byPlayer))
                    {
                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        world.PlaySoundAt(WaterSplashSoundLocation, byPlayer, byPlayer, true);
                    }
                }

                // Resource
                else
                {
                    if (be.TryAddResource(byPlayer.InventoryManager.ActiveHotbarSlot, stackQuantity))
                    {
                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        world.PlaySoundAt(StoneCrushSoundLocation, byPlayer, byPlayer, true);
                    }
                }
            }

            // Player hands is empty, take all the matching blocks outs of inventory
            else if (blockSel.SelectionBoxIndex == 0 && activeStack == null)
            {

                if (be.TryAddAll(byPlayer))
                {
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                    world.PlaySoundAt(StoneCrushSoundLocation, byPlayer, byPlayer, true);
                }
            }

            be.UpdateDisplayedTop();

            return true;
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

                    be.UpdateDisplayedTop();

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
                string stoneLangCode = Core.ModId + ":info-rubblestorage-stone(count={0})";
                string gravelLangCode = Core.ModId + ":info-rubblestorage-gravel(count={0})";
                string sandLangCode = Core.ModId + ":info-rubblestorage-sand(count={0})";

                string rockName = Lang.Get(inv.StoredRock.ToString());

                dsc.AppendLine(Lang.Get(Core.ModId + ":info-rubblestorage-type(type={0})", rockName));
                dsc.AppendLine(Lang.Get(stoneLangCode, inv.StoneSlot.StackSize));
                dsc.AppendLine(Lang.Get(gravelLangCode, inv.GravelSlot.StackSize));
                dsc.AppendLine(Lang.Get(sandLangCode, inv.SandSlot.StackSize));
            }
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer)
        {
            if (WorldInteractionsBySel == null)
            {
                WorldInteractionsBySel = new List<WorldInteraction[]>() { new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = Core.ModId + ":wi-rubblestorage-hammer",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = GetRubbleHammers()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = Core.ModId + ":wi-rubblestorage-add-one",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = GetAvailableContent("stone"),
                        GetMatchingStacks = WIGetMatchingStacks_StoneType
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = Core.ModId + ":wi-rubblestorage-add-stack",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sprint",
                        Itemstacks = GetAvailableContent("stone", true),
                        GetMatchingStacks = WIGetMatchingStacks_StoneType
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = Core.ModId + ":wi-rubblestorage-add-all",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = Core.ModId + ":wi-rubblestorage-water",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = GetWaterPortionStacks()
                    }
                }};

                foreach (string type in new string[] { "sand", "gravel", "stone" })
                {

                    WorldInteractionsBySel.Add(new WorldInteraction[] {
                        new WorldInteraction()
                        {
                            ActionLangCode = Core.ModId + ":wi-rubblestorage-lock",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak"
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = Core.ModId + ":wi-rubblestorage-take-one-" + type,
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = GetAvailableContent(type),
                            GetMatchingStacks = WIGetMatchingStacks_StoneType
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = Core.ModId + ":wi-rubblestorage-take-stack-" + type,
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sprint",
                            Itemstacks = GetAvailableContent(type, true),
                            GetMatchingStacks = WIGetMatchingStacks_StoneType
                        }
                    });
                }
            }

            return WorldInteractionsBySel[blockSel.SelectionBoxIndex]
                .Append(base.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer));
        }

        private ItemStack[] GetRubbleHammers()
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

        private ItemStack[] GetAvailableContent(string type, bool fullStack = false)
        {
            var content = new List<ItemStack>();
            foreach (RockData rockData in rockManager.Data)
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

        private ItemStack[] GetWaterPortionStacks()
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

        private ItemStack[] WIGetMatchingStacks_StoneType(WorldInteraction wi, BlockSelection blockSel, EntitySelection entitySel)
        {
            var be = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BERubbleStorage;

            if (be?.Inventory?.StoredRock != null)
            {
                RockData? data = rockManager.GetValue(be.Inventory.StoredRock);

                if (data != null)
                {
                    return wi.Itemstacks
                        .Where((item) => data[item.Collectible.Code] != null)
                        .ToArray();
                }
            }

            return wi.Itemstacks;
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

        public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
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

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            if (blockAccessor.GetBlockEntity(pos) is BERubbleStorage be
                && be.Inventory?.StoredRock == null)
            {
                return new Cuboidf[] { SelectionBoxes[0] };
            }

            return base.GetSelectionBoxes(blockAccessor, pos);
        }

        public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        {
            return new Cuboidf[] { SelectionBoxes[0].RotatedCopy(0, 180, 0, new Vec3d(0.5, 0.5, 0.5)) };
        }
    }
}