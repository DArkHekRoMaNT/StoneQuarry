using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace QuarryWorks
{
    public class RubbleStorageBlock : GenericStoneStorageBlock
    {
        static SimpleParticleProperties interactParticles = new SimpleParticleProperties()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(),
            MinQuantity = 0,
            AddQuantity = 3,
            GravityEffect = .9f,
            WithTerrainCollision = true,
            ParticleModel = EnumParticleModel.Quad,
            LifeLength = 0.5f,
            MinVelocity = new Vec3f(-1, 2, -1),
            AddVelocity = new Vec3f(2, 0, 2),
            MinSize = 0.07f,
            MaxSize = 0.1f,
        };

        static RubbleStorageBlock()
        {
            interactParticles.ParticleModel = EnumParticleModel.Quad;
            interactParticles.AddPos.Set(.5, .5, .5);
            interactParticles.MinQuantity = 5;
            interactParticles.AddQuantity = 20;
            interactParticles.LifeLength = 2.5f;
            interactParticles.MinSize = 0.1f;
            interactParticles.MaxSize = 0.4f;
            interactParticles.MinVelocity.Set(-0.4f, -0.4f, -0.4f);
            interactParticles.AddVelocity.Set(0.8f, 1.2f, 0.8f);
            interactParticles.DieOnRainHeightmap = false;
        }


        //Adds sand, gravel, and muddy gravel production.
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            dsc.AppendLine("Stone Type: " + inSlot.Itemstack.Attributes.GetString("type"));
            dsc.AppendLine("Stone Amount: " + inSlot.Itemstack.Attributes.GetInt("stone").ToString());
            dsc.AppendLine("Gravel Amount: " + inSlot.Itemstack.Attributes.GetInt("gravel").ToString());
            dsc.AppendLine("Sand Amount: " + inSlot.Itemstack.Attributes.GetInt("sand").ToString());
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            RubbleStorageBE rsbe = world.BlockAccessor.GetBlockEntity(pos) as RubbleStorageBE;

            if (be is GenericStorageCapBE)
            {
                rsbe = world.BlockAccessor.GetBlockEntity((be as GenericStorageCapBE).core) as RubbleStorageBE;
            }
            if (rsbe == null)
            {
                return "";
            }
            string stonelock = "";
            string gravlock = "";
            string sandlock = "";

            switch (rsbe.storageLock)
            {
                case RubbleStorageBE.StorageLocksEnum.Stone:
                    {
                        stonelock = " : Locked";
                        break;
                    }
                case RubbleStorageBE.StorageLocksEnum.Gravel:
                    {
                        gravlock = " : Locked";
                        break;
                    }
                case RubbleStorageBE.StorageLocksEnum.Sand:
                    {
                        sandlock = " : Locked";
                        break;
                    }
                default:
                    break;
            }

            string rstring = "Type: " + rsbe.storedType +
                "\nStone Stored: " + rsbe.storedtypes["stone"].ToString() + stonelock +
                "\nGravel Stored: " + rsbe.storedtypes["gravel"].ToString() + gravlock +
                "\nSand Stored: " + rsbe.storedtypes["sand"].ToString() + sandlock;

            return rstring;
            //return base.GetPlacedBlockInfo(world, pos, forPlayer);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            int stockMod = 1;
            if (byPlayer.Entity.Controls.Sprint)
            {
                stockMod = byPlayer.InventoryManager.ActiveHotbarSlot.MaxSlotStackSize;
            }

            BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            RubbleStorageBE rcbe = world.BlockAccessor.GetBlockEntity(blockSel.Position) as RubbleStorageBE;
            if (be is GenericStorageCapBE)
            {
                rcbe = world.BlockAccessor.GetBlockEntity((be as GenericStorageCapBE).core) as RubbleStorageBE;
            }
            if (rcbe == null)
            {
                return true;
            }

            // if the player is looking at one of the buttons on the crate.
            if (blockSel.SelectionBoxIndex == 1 && byPlayer.Entity.Controls.Sneak)
            {
                setLock(rcbe, RubbleStorageBE.StorageLocksEnum.Sand);
            }
            else if (blockSel.SelectionBoxIndex == 2 && byPlayer.Entity.Controls.Sneak)
            {
                setLock(rcbe, RubbleStorageBE.StorageLocksEnum.Gravel);
            }
            else if (blockSel.SelectionBoxIndex == 3 && byPlayer.Entity.Controls.Sneak)
            {
                setLock(rcbe, RubbleStorageBE.StorageLocksEnum.Stone);
            }

            if (blockSel.SelectionBoxIndex == 1)
            {
                if (rcbe.RemoveResource(world, byPlayer, blockSel, "sand", stockMod))
                {
                    if (world.Side == EnumAppSide.Client)
                    {
                        (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                    }
                    world.PlaySoundAt(new AssetLocation("game", "sounds/effect/stonecrush"), byPlayer, byPlayer);
                    interactParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                    interactParticles.ColorByBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                    world.SpawnParticles(interactParticles, byPlayer);

                }
            }
            else if (blockSel.SelectionBoxIndex == 2)
            {
                if (rcbe.RemoveResource(world, byPlayer, blockSel, "gravel", stockMod))
                {
                    if (world.Side == EnumAppSide.Client)
                    {
                        (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                    }
                    world.PlaySoundAt(new AssetLocation("game", "sounds/effect/stonecrush"), byPlayer, byPlayer);
                    interactParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                    interactParticles.ColorByBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                    world.SpawnParticles(interactParticles, byPlayer);
                }
            }
            else if (blockSel.SelectionBoxIndex == 3)
            {
                if (rcbe.RemoveResource(world, byPlayer, blockSel, "stone", stockMod))
                {
                    if (world.Side == EnumAppSide.Client)
                    {
                        (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                    }
                    world.PlaySoundAt(new AssetLocation("game", "sounds/effect/stonecrush"), byPlayer, byPlayer);
                    interactParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                    interactParticles.ColorByBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                    world.SpawnParticles(interactParticles, byPlayer);
                }
            }

            else if (blockSel.SelectionBoxIndex == 0 && byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack != null)
            {
                // attempts to add the players resource to the block.
                if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.ItemAttributes["rubbleable"].AsBool())
                {
                    if (rcbe.Degrade())
                    {
                        if (world.Side == EnumAppSide.Client)
                        {
                            (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        }
                        world.PlaySoundAt(new AssetLocation("game", "sounds/block/heavyice"), byPlayer, byPlayer);
                    }
                }
                else if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetTreeAttribute("contents") != null
                    && byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetTreeAttribute("contents").GetItemstack("0") != null)
                {
                    ItemStack tstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetTreeAttribute("contents").GetItemstack("0");
                    if (tstack.Collectible.Code.Domain == "game" && tstack.Collectible.Code.Path == "waterportion")
                    {
                        if (rcbe.Drench(world, blockSel))
                        {
                            if (world.Side == EnumAppSide.Client)
                            {
                                (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                            }
                            world.PlaySoundAt(new AssetLocation("game", "sounds/environment/largesplash1"), byPlayer, byPlayer);
                        }
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
                        world.PlaySoundAt(new AssetLocation("game", "sounds/effect/stonecrush"), byPlayer, byPlayer);
                    }
                }
            }
            else if (blockSel.SelectionBoxIndex == 0 && byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null)
            {
                // if the players hand is empty we want to take all the matching blocks outs of their inventory.
                if (rcbe.AddAll(byPlayer))
                {
                    if (world.Side == EnumAppSide.Client)
                    {
                        (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                    }
                    world.PlaySoundAt(new AssetLocation("game", "sounds/effect/stonecrush"), byPlayer, byPlayer);
                }
            }
            rcbe.CheckDisplayVariant(world, blockSel);

            return true;
        }

        public void setLock(RubbleStorageBE rsbe, RubbleStorageBE.StorageLocksEnum toLock)
        {
            rsbe.storageLock = toLock;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            if (be is GenericStorageCapBE)
            {
                world.BlockAccessor.BreakBlock((be as GenericStorageCapBE).core, byPlayer);
                return;
            }

            RubbleStorageBE rsbe = be as RubbleStorageBE;

            if (rsbe != null)
            {
                ItemStack dropstack = new ItemStack(world.BlockAccessor.GetBlock(pos));
                dropstack.Attributes.SetString("type", rsbe.storedType);
                dropstack.Attributes.SetInt("stone", rsbe.storedtypes["stone"]);
                dropstack.Attributes.SetInt("gravel", rsbe.storedtypes["gravel"]);
                dropstack.Attributes.SetInt("sand", rsbe.storedtypes["sand"]);
                world.SpawnItemEntity(dropstack, pos.ToVec3d());
            }
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (byItemStack == null)
            {
                return false;
            }
            RubbleStorageBE rsbe = world.BlockAccessor.GetBlockEntity(blockSel.Position) as RubbleStorageBE;
            rsbe.storedType = byItemStack.Attributes.GetString("type", "");
            rsbe.storedtypes["stone"] = byItemStack.Attributes.GetInt("stone", 0);
            rsbe.storedtypes["gravel"] = byItemStack.Attributes.GetInt("gravel", 0);
            rsbe.storedtypes["sand"] = byItemStack.Attributes.GetInt("sand", 0);
            if (byItemStack.ItemAttributes.KeyExists("maxStorable"))
            {
                rsbe.maxStorable = byItemStack.ItemAttributes["maxStorable"].AsInt();
            }

            return true;
        }
    }
}