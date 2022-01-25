using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    public class BlockRubbleStorage : BlockGenericMultiblockPart
    {
        readonly List<WorldInteraction[]> interactionsBySel = new List<WorldInteraction[]>();
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

            if (api.Side == EnumAppSide.Client)
            {
                var hammers = new List<ItemStack>();
                foreach (var colObj in api.World.Collectibles)
                {
                    if (colObj.Attributes != null && colObj.Attributes["rubbleable"].Exists)
                    {
                        hammers.Add(new ItemStack(colObj));
                    }
                }


                var dict = new Dictionary<string, List<ItemStack>>{
                    { "sand", new List<ItemStack>() },
                    { "gravel", new List<ItemStack>() },
                    { "stone", new List<ItemStack>() }
                };

                foreach (var type in BERubbleStorage.allowedTypes)
                {
                    dict["stone"].Add(new ItemStack(
                        api.World.GetItem(new AssetLocation("game:stone-" + type))
                    ));
                    dict["gravel"].Add(new ItemStack(
                        api.World.GetBlock(new AssetLocation("game:gravel-" + type))
                    ));
                    dict["sand"].Add(new ItemStack(
                       api.World.GetBlock(new AssetLocation("game:sand-" + type))
                    ));
                }

                var waterPortion = api.World.GetItem(new AssetLocation("game:waterportion")).GetHandBookStacks(api as ICoreClientAPI);
                waterPortion[0].StackSize = 100;


                interactionsBySel.Add(new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = Code.Domain + ":wi-rubblestorage-hammer",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = hammers.ToArray()
                    },
                    new WorldInteraction() {
                        ActionLangCode = Code.Domain + ":wi-rubblestorage-add-one",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = dict["stone"].ToArray(),
                        GetMatchingStacks = GetMatchingStacks_StoneType
                    },
                    new WorldInteraction() {
                        ActionLangCode = Code.Domain + ":wi-rubblestorage-add-stack",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sprint",
                        Itemstacks = dict["stone"].Select((i)=>{
                            var r = i.Clone(); r.StackSize = r.Collectible.MaxStackSize; return r;
                        }).ToArray(),
                        GetMatchingStacks = GetMatchingStacks_StoneType
                    },
                    new WorldInteraction() {
                        ActionLangCode = Code.Domain + ":wi-rubblestorage-add-all",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = Code.Domain + ":wi-rubblestorage-water",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = waterPortion.ToArray()
                    }
                });

                foreach (var type in new string[] { "sand", "gravel", "stone" })
                {

                    interactionsBySel.Add(new WorldInteraction[] {
                        new WorldInteraction()
                        {
                            ActionLangCode = Code.Domain + ":wi-rubblestorage-lock",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak"
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = Code.Domain + ":wi-rubblestorage-take-one-" + type,
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = dict[type].ToArray(),
                            GetMatchingStacks = GetMatchingStacks_StoneType
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = Code.Domain + ":wi-rubblestorage-take-stack-" + type,
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sprint",
                            Itemstacks = dict[type].Select((i)=>{
                                var r = i.Clone(); r.StackSize = r.Collectible.MaxStackSize; return r;
                            }).ToArray(),
                            GetMatchingStacks = GetMatchingStacks_StoneType
                        }
                    });
                }
            }
        }

        private ItemStack[] GetMatchingStacks_StoneType(WorldInteraction wi, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (Variant["type"] == "empty") return wi.Itemstacks;
            return wi.Itemstacks.Where((i) => i.Collectible.LastCodePart() == Variant["stone"]).ToArray();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            int stockMod = 1;

            if (byPlayer.Entity.Controls.Sprint)
            {
                stockMod = byPlayer.InventoryManager.ActiveHotbarSlot.MaxSlotStackSize;
            }

            var be = (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEGenericMultiblockPart)?.Core;
            if (!(be is BERubbleStorage rcbe))
            {
                return true;
            }


            string selectedType = "";
            switch ((EnumStorageLock)blockSel.SelectionBoxIndex)
            {
                case EnumStorageLock.Stone: selectedType = "stone"; break;
                case EnumStorageLock.Gravel: selectedType = "gravel"; break;
                case EnumStorageLock.Sand: selectedType = "sand"; break;
            }


            var activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

            // If the player is looking at one of the buttons on the crate.
            if (byPlayer.Entity.Controls.Sneak && selectedType != "")
            {
                if (rcbe.storageLock != (EnumStorageLock)blockSel.SelectionBoxIndex)
                {
                    rcbe.storageLock = (EnumStorageLock)blockSel.SelectionBoxIndex;
                }
                else rcbe.storageLock = EnumStorageLock.None;
            }

            // If the player try get a resource
            else if (selectedType != "" && rcbe.RemoveResource(world, byPlayer, blockSel, selectedType, stockMod))
            {
                if (world.Side == EnumAppSide.Client)
                {
                    (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                }
                world.PlaySoundAt(new AssetLocation("game", "sounds/effect/stonecrush"), byPlayer, byPlayer, true);

                interactParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                interactParticles.ColorByBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                world.SpawnParticles(interactParticles, byPlayer);
            }

            // If the player try use a tool or add a resource
            else if (selectedType == "" && activeStack != null)
            {
                // Is a rubble hammer?
                if (activeStack.ItemAttributes?["rubbleable"]?.AsBool() ?? false)
                {
                    if (rcbe.Degrade())
                    {
                        if (world.Side == EnumAppSide.Client)
                        {
                            (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        }
                        world.PlaySoundAt(new AssetLocation("game", "sounds/block/heavyice"), byPlayer, byPlayer, true, volume: 0.25f);

                        activeStack.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot);
                    }
                }
                else if (activeStack.Attributes.GetTreeAttribute("contents") != null
                    && activeStack.Attributes.GetTreeAttribute("contents").GetItemstack("0") != null)
                {
                    if (rcbe.TryDrench(world, blockSel, byPlayer))
                    {
                        if (world.Side == EnumAppSide.Client)
                        {
                            (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        }
                        world.PlaySoundAt(new AssetLocation("game", "sounds/environment/largesplash1"),
                            byPlayer, byPlayer, true);
                    }
                }
                else
                {
                    if (rcbe.AddResource(byPlayer.InventoryManager.ActiveHotbarSlot, stockMod))
                    {
                        if (world.Side == EnumAppSide.Client)
                        {
                            (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        }
                        world.PlaySoundAt(new AssetLocation("game", "sounds/effect/stonecrush"),
                            byPlayer, byPlayer, true);
                    }
                }
            }

            // If the players hand is empty we want to take all the matching blocks outs of their inventory
            else if (blockSel.SelectionBoxIndex == 0 && activeStack == null)
            {

                if (rcbe.AddAll(byPlayer))
                {
                    if (world.Side == EnumAppSide.Client)
                    {
                        (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                    }
                    world.PlaySoundAt(new AssetLocation("game", "sounds/effect/stonecrush"),
                        byPlayer, byPlayer, true);
                }
            }
            rcbe.CheckDisplayVariant(world, blockSel);

            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (!(world.BlockAccessor.GetBlockEntity(pos) is BEGenericMultiblockPart be))
            {
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                return;
            }
            if (!be.IsCore)
            {
                world.BlockAccessor.BreakBlock(be.CorePos, byPlayer);
                return;
            }


            if (be is BERubbleStorage rsbe)
            {
                ItemStack dropstack = new ItemStack(world.BlockAccessor.GetBlock(pos));
                dropstack.Attributes.SetString("type", rsbe.storedType);
                dropstack.Attributes.SetInt("stone", rsbe.storage["stone"]);
                dropstack.Attributes.SetInt("gravel", rsbe.storage["gravel"]);
                dropstack.Attributes.SetInt("sand", rsbe.storage["sand"]);
                world.SpawnItemEntity(dropstack, pos.ToVec3d());
            }
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (byItemStack == null) return false;


            var rsbe = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BERubbleStorage;

            rsbe.storedType = byItemStack.Attributes.GetString("type", "");
            rsbe.storage["stone"] = byItemStack.Attributes.GetInt("stone", 0);
            rsbe.storage["gravel"] = byItemStack.Attributes.GetInt("gravel", 0);
            rsbe.storage["sand"] = byItemStack.Attributes.GetInt("sand", 0);

            return true;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            string rockType = inSlot.Itemstack.Attributes.GetString("type");
            int stoneCount = inSlot.Itemstack.Attributes.GetInt("stone");
            int gravelCount = inSlot.Itemstack.Attributes.GetInt("gravel");
            int sandCount = inSlot.Itemstack.Attributes.GetInt("sand");

            if (string.IsNullOrEmpty(rockType)) rockType = Lang.Get(Code.Domain + ":info-rubblestorage-none");

            dsc.AppendLine(Lang.Get(Code.Domain + ":info-rubblestorage-type(type={0})", rockType));
            dsc.AppendLine(Lang.Get(Code.Domain + ":info-rubblestorage-stone(count={0})", stoneCount));
            dsc.AppendLine(Lang.Get(Code.Domain + ":info-rubblestorage-gravel(count={0})", gravelCount));
            dsc.AppendLine(Lang.Get(Code.Domain + ":info-rubblestorage-sand(count={0})", sandCount));
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer)
        {
            return interactionsBySel[blockSel.SelectionBoxIndex].Append(base.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer));
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var be = (world.BlockAccessor.GetBlockEntity(pos) as BEGenericMultiblockPart)?.Core;
            if (be is BERubbleStorage rsbe)
            {
                ItemStack dropstack = new ItemStack(world.BlockAccessor.GetBlock(pos));
                dropstack.Attributes.SetString("type", rsbe.storedType);
                dropstack.Attributes.SetInt("stone", rsbe.storage["stone"]);
                dropstack.Attributes.SetInt("gravel", rsbe.storage["gravel"]);
                dropstack.Attributes.SetInt("sand", rsbe.storage["sand"]);
                return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier).Append(dropstack);
            }
            return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }
}