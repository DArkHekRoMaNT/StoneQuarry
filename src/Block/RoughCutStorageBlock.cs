using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace QuarryWorks
{
    public class RoughCutStorageBlock : GenericStoneStorageBlock
    {
        //used to store what is dropped from the quarry.

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

        static RoughCutStorageBlock()
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



        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            if (!base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack)) { return false; };
            RoughCutStorageBE be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as RoughCutStorageBE;
            be.istack = byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            if (be is GenericStorageCapBE)
            {
                be = world.BlockAccessor.GetBlockEntity((be as GenericStorageCapBE).core);
            }
            RoughCutStorageBE rcbe = be as RoughCutStorageBE;

            if (rcbe != null && rcbe.istack.Attributes.GetInt("stonestored") > 0)
            {
                world.SpawnItemEntity(rcbe.istack.Clone(), pos.ToVec3d());
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public int dropcount(int chances, int rate)
        {
            int rcount = 0;
            Random r = new Random(); //TODO:  runtime init random
            for (int i = 1; i <= chances; i++)
            {
                if (r.Next(0, 100) <= rate)
                {
                    rcount += 1;
                }
            }
            return rcount;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            AssetLocation interactsound = new AssetLocation("game", "sounds/block/heavyice");
            BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be is GenericStorageCapBE)
                be = world.BlockAccessor.GetBlockEntity((be as GenericStorageCapBE).core);
            RoughCutStorageBE rcbe = be as RoughCutStorageBE; // casts the block entity as a roughcut block entity.

            if (rcbe == null || rcbe.istack == null)
            {
                return false;
            }

            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null)
            {
                if (rcbe.istack.Attributes.HasAttribute("stonestored"))
                {
                    if (world.Side == EnumAppSide.Client)
                    {
                        (byPlayer as IClientPlayer).ShowChatNotification("Contains: " + rcbe.istack.Attributes["stonestored"].ToString() + " stone");
                    }
                    //rcbe.istack.Attributes.SetInt("stonestored", rcbe.istack.Attributes.GetInt("stonestored") - 1);
                }
                else
                {
                    return false;
                }
            }

            else
            {
                ItemStack pistack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

                AssetLocation dropItemString = null;
                ItemStack dropStack = null;


                if (world.Side == EnumAppSide.Client)
                {
                    (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                }

                if (rcbe.istack.Attributes.GetInt("stonestored") <= 0)
                {
                    return false;
                }

                interactParticles.ColorByBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                interactParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
                world.SpawnParticles(interactParticles, byPlayer);

                world.PlaySoundAt(interactsound, byPlayer, byPlayer, true, 32, .5f);
                if (pistack.ItemAttributes.KeyExists("polishedrrate"))
                {
                    dropItemString = new AssetLocation("game", "rockpolished-" + rcbe.istack.Block.FirstCodePart(1));
                    Block tblock = world.GetBlock(dropItemString);

                    if (tblock != null)
                    {
                        dropStack = new ItemStack(world.GetBlock(dropItemString), dropcount(pistack.ItemAttributes["rchances"].AsInt(), pistack.ItemAttributes["polishedrrate"].AsInt()));
                        world.SpawnItemEntity(dropStack, blockSel.Position.ToVec3d() + blockSel.HitPosition, new Vec3d(.05 * blockSel.Face.Normalf.ToVec3d().X, .1, .05 * blockSel.Face.Normalf.ToVec3d().Z));
                        rcbe.istack.Attributes.SetInt("stonestored", rcbe.istack.Attributes.GetInt("stonestored") - 1);
                        if (rcbe.istack.Attributes.GetInt("stonestored") <= 0)
                        {
                            world.BlockAccessor.BreakBlock(blockSel.Position, byPlayer);
                        }
                        byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);

                    }
                }

                else if (pistack.ItemAttributes.KeyExists("brickrrate"))
                {
                    dropItemString = new AssetLocation("game", "stonebrick-" + rcbe.istack.Block.FirstCodePart(1));
                    Item titem = world.GetItem(dropItemString);

                    if (titem != null)
                    {
                        dropStack = new ItemStack(world.GetItem(dropItemString), dropcount(pistack.ItemAttributes["rchances"].AsInt(), pistack.ItemAttributes["brickrrate"].AsInt()));
                        world.SpawnItemEntity(dropStack, blockSel.Position.ToVec3d() + blockSel.HitPosition, new Vec3d(.05 * blockSel.Face.Normalf.ToVec3d().X, .1, .05 * blockSel.Face.Normalf.ToVec3d().Z));
                        rcbe.istack.Attributes.SetInt("stonestored", rcbe.istack.Attributes.GetInt("stonestored") - 1);
                        if (rcbe.istack.Attributes.GetInt("stonestored") <= 0)
                        {
                            world.BlockAccessor.BreakBlock(blockSel.Position, byPlayer);
                        }
                        byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);

                    }
                }

                else if (pistack.ItemAttributes.KeyExists("stonerrate"))
                {
                    dropItemString = new AssetLocation("game", "stone-" + rcbe.istack.Block.FirstCodePart(1));
                    Item titem = world.GetItem(dropItemString);

                    if (titem != null)
                    {
                        dropStack = new ItemStack(world.GetItem(dropItemString), dropcount(pistack.ItemAttributes["rchances"].AsInt(), pistack.ItemAttributes["stonerrate"].AsInt()));
                        world.SpawnItemEntity(dropStack, blockSel.Position.ToVec3d() + blockSel.HitPosition, new Vec3d(.05 * blockSel.Face.Normalf.ToVec3d().X, .1, .05 * blockSel.Face.Normalf.ToVec3d().Z));
                        rcbe.istack.Attributes.SetInt("stonestored", rcbe.istack.Attributes.GetInt("stonestored") - 1);
                        if (rcbe.istack.Attributes.GetInt("stonestored") <= 0)
                        {
                            world.BlockAccessor.BreakBlock(blockSel.Position, byPlayer);
                        }
                        byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);
                    }
                }

                else if (pistack.ItemAttributes.KeyExists("rockrrate"))
                {
                    dropItemString = new AssetLocation("game", "rock-" + rcbe.istack.Block.FirstCodePart(1));
                    Block tblock = world.GetBlock(dropItemString);

                    if (tblock != null)
                    {
                        dropStack = new ItemStack(world.GetBlock(dropItemString), dropcount(pistack.ItemAttributes["rchances"].AsInt(), pistack.ItemAttributes["rockrrate"].AsInt()));
                        world.SpawnItemEntity(dropStack, blockSel.Position.ToVec3d() + blockSel.HitPosition, new Vec3d(.05 * blockSel.Face.Normalf.ToVec3d().X, .1, .05 * blockSel.Face.Normalf.ToVec3d().Z));
                        rcbe.istack.Attributes.SetInt("stonestored", rcbe.istack.Attributes.GetInt("stonestored") - 1);
                        if (rcbe.istack.Attributes.GetInt("stonestored") <= 0)
                        {
                            world.BlockAccessor.BreakBlock(blockSel.Position, byPlayer);
                        }
                        byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);
                    }
                }
            }

            return true;
        }
    }

}