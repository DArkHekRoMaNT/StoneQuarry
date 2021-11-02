using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace QuarryWorks
{
    /// <summary> Used to store what is dropped from the quarry </summary>
    public class RoughCutStorageBlock : GenericStoneStorageBlock
    {
        static readonly SimpleParticleProperties interactParticles = new SimpleParticleProperties()
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

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            if (!base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack)) return false;

            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack != null)
            {
                RoughCutStorageBE be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as RoughCutStorageBE;
                be.blockStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Clone();
                be.blockStack.StackSize = 1;
            }

            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            if (be is GenericStorageCapBE)
            {
                be = world.BlockAccessor.GetBlockEntity((be as GenericStorageCapBE).core);
            }

            var rcbe = be as RoughCutStorageBE;
            if (rcbe != null && rcbe.blockStack.Attributes.GetInt("stonestored") > 0)
            {
                world.SpawnItemEntity(rcbe.blockStack.Clone(), pos.ToVec3d());
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            AssetLocation interactsound = new AssetLocation("game", "sounds/block/heavyice");
            BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be is GenericStorageCapBE)
            {
                be = world.BlockAccessor.GetBlockEntity((be as GenericStorageCapBE).core);
            }

            var rcbe = be as RoughCutStorageBE;
            if (rcbe == null || rcbe.blockStack == null) return false;

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



            if (world.Side == EnumAppSide.Client)
            {
                (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
            }

            ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            AssetLocation dropCode = null;
            int dropRate = 0;

            interactParticles.ColorByBlock = world.BlockAccessor.GetBlock(blockSel.Position);
            interactParticles.MinPos = blockSel.Position.ToVec3d() + blockSel.HitPosition;
            world.SpawnParticles(interactParticles, byPlayer);

            world.PlaySoundAt(interactsound, byPlayer, byPlayer, true, 32, .5f);

            if (activeStack.ItemAttributes != null)
            {
                if (activeStack.ItemAttributes.KeyExists("polishedrrate"))
                {
                    dropCode = new AssetLocation("game", "rockpolished-" + rcbe.blockStack.Block.FirstCodePart(1));
                    dropRate = activeStack.ItemAttributes["polishedrrate"].AsInt();
                }
                else if (activeStack.ItemAttributes.KeyExists("brickrrate"))
                {
                    dropCode = new AssetLocation("game", "stonebrick-" + rcbe.blockStack.Block.FirstCodePart(1));
                    dropRate = activeStack.ItemAttributes["brickrrate"].AsInt();
                }
                else if (activeStack.ItemAttributes.KeyExists("stonerrate"))
                {
                    dropCode = new AssetLocation("game", "stone-" + rcbe.blockStack.Block.FirstCodePart(1));
                    dropRate = activeStack.ItemAttributes["stonerrate"].AsInt();
                }
                else if (activeStack.ItemAttributes.KeyExists("rockrrate"))
                {
                    dropCode = new AssetLocation("game", "rock-" + rcbe.blockStack.Block.FirstCodePart(1));
                    dropRate = activeStack.ItemAttributes["rockrrate"].AsInt();
                }
            }


            if (dropCode != null)
            {
                var colObj = (CollectibleObject)world.GetItem(dropCode) ?? world.GetBlock(dropCode);
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
            }

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
    }
}