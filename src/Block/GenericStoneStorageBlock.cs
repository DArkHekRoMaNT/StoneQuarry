using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace QuarryWorks
{
    public class GenericStoneStorageBlock : Block
    {
        static SimpleParticleProperties breakparticle = new SimpleParticleProperties()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(),
            MinQuantity = 0,
            AddQuantity = 3,
            GravityEffect = 1f,
            WithTerrainCollision = true,
            ParticleModel = EnumParticleModel.Quad,
            LifeLength = 0.5f,
            MinVelocity = new Vec3f(-1, 2, -1),
            AddVelocity = new Vec3f(2, 0, 2),
            MinSize = 0.07f,
            MaxSize = 0.1f,
        };

        static GenericStoneStorageBlock()
        {
            breakparticle.ParticleModel = EnumParticleModel.Quad;
            breakparticle.AddPos.Set(1, 1, 1);
            breakparticle.MinQuantity = 2;
            breakparticle.AddQuantity = 12;
            breakparticle.LifeLength = 2f;
            breakparticle.MinSize = 0.2f;
            breakparticle.MaxSize = 0.5f;
            breakparticle.MinVelocity.Set(-0.4f, -0.4f, -0.4f);
            breakparticle.AddVelocity.Set(0.8f, 1.2f, 0.8f);
            breakparticle.DieOnRainHeightmap = false;
        }

        public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
        {
            BlockFacing[] sughv = SuggestedHVOrientation(byPlayer, blockSel);
            Block ablock = world.GetBlock(CodeWithVariant("dir", sughv[0].Code));

            if (ablock.Attributes != null && ablock.Attributes.KeyExists("caps"))
            {
                for (int i = 0; i < ablock.Attributes["caps"].AsArray().Length; i++)
                {
                    BlockPos checkspot = blockSel.Position.Copy() + new BlockPos(ablock.Attributes["caps"].AsArray()[i]["x"].AsInt(), ablock.Attributes["caps"].AsArray()[i]["y"].AsInt(), ablock.Attributes["caps"].AsArray()[i]["z"].AsInt());
                    if (world.BlockAccessor.GetBlock(checkspot).Id != 0)
                    {
                        return false;
                    }
                }
            }
            return base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode);
        }
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            BlockFacing[] sughv = SuggestedHVOrientation(byPlayer, blockSel);

            Block ablock = world.GetBlock(CodeWithVariant("dir", sughv[0].Code));
            world.BlockAccessor.SetBlock(ablock.Id, blockSel.Position);

            world.BlockAccessor.MarkBlockDirty(blockSel.Position);
            world.BlockAccessor.MarkBlockEntityDirty(blockSel.Position);

            if (ablock.Attributes != null && ablock.Attributes.KeyExists("caps"))
            {
                for (int i = 0; i < ablock.Attributes["caps"].AsArray().Length; i++)
                {
                    Dictionary<string, string> rdict = new Dictionary<string, string>();

                    foreach (JsonObject obj in ablock.Attributes["caps"].AsArray()[i]["varType"].AsArray())
                    {
                        rdict.Add(obj.AsArray()[0].AsString(), obj.AsArray()[1].AsString());
                    }

                    //Block capBlock = world.GetBlock(CodeWithVariant("dir", ablock.Attributes["caps"].AsArray()[0]["var"].AsString()));
                    Block capBlock = world.GetBlock(CodeWithVariants(rdict));
                    BlockPos capPos = blockSel.Position.Copy() + new BlockPos(ablock.Attributes["caps"].AsArray()[i]["x"].AsInt(), ablock.Attributes["caps"].AsArray()[i]["y"].AsInt(), ablock.Attributes["caps"].AsArray()[i]["z"].AsInt());
                    world.BlockAccessor.ExchangeBlock(capBlock.Id, capPos);

                    world.BlockAccessor.SpawnBlockEntity("StoneStorageCapBE", capPos);
                    (world.BlockAccessor.GetBlockEntity(capPos) as GenericStorageCapBE).core = blockSel.Position;
                    (world.BlockAccessor.GetBlockEntity(blockSel.Position) as GenericStorageCoreBE).caps.Add(capPos);

                    world.BlockAccessor.MarkBlockDirty(capPos);
                    world.BlockAccessor.MarkBlockEntityDirty(capPos);
                }
            }
            return true;
        }
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            BlockPos masterpos = null;

            if (be is GenericStorageCapBE)
            {
                masterpos = (be as GenericStorageCapBE).core;
                be = world.BlockAccessor.GetBlockEntity((be as GenericStorageCapBE).core);
            }
            else
            {
                masterpos = pos;
            }

            if (be == null)
            {
                world.BlockAccessor.SetBlock(0, pos);
                return;
            }

            GenericStorageCoreBE core = be as GenericStorageCoreBE;
            foreach (BlockPos cap in core.caps)
            {
                breakparticle.MinPos = cap.ToVec3d();
                breakparticle.ColorByBlock = world.BlockAccessor.GetBlock(masterpos);
                world.BlockAccessor.SetBlock(0, cap);
                world.BlockAccessor.RemoveBlockEntity(cap);
                world.SpawnParticles(breakparticle, byPlayer);
            }

            world.BlockAccessor.SetBlock(0, masterpos);

            //base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public bool SwitchVariant(IWorldAccessor world, BlockSelection blockSel, Dictionary<string, string> switchArray)
        {
            //Switches multiblock to a seperate variants.
            Block tempv = world.GetBlock(CodeWithVariants(switchArray));
            BlockPos corpos = blockSel.Position;

            List<BlockPos> hlightspots = new List<BlockPos>();
            if (tempv != null)
            {
                BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
                if (be is GenericStorageCapBE)
                {
                    blockSel.Position = (be as GenericStorageCapBE).core;
                    (world.BlockAccessor.GetBlock((be as GenericStorageCapBE).core) as GenericStoneStorageBlock).SwitchVariant(world, blockSel, switchArray);
                    return true;
                }


                foreach (BlockPos slave in (be as GenericStorageCoreBE).caps)
                {
                    world.BlockAccessor.SetBlock(0, slave);
                }
                (be as GenericStorageCoreBE).caps = new List<BlockPos>();


                Block bactual = world.BlockAccessor.GetBlock(corpos);
                Block bfull = world.GetBlock(bactual.CodeWithVariants(switchArray));

                if (tempv.Attributes != null && tempv.Attributes.KeyExists("caps") && bfull != null)
                {
                    world.BlockAccessor.ExchangeBlock(bfull.Id, corpos);
                    foreach (JsonObject b in tempv.Attributes["caps"].AsArray())
                    {
                        Dictionary<string, string> rdict = new Dictionary<string, string>();
                        foreach (JsonObject bv in b["varType"].AsArray())
                        {
                            rdict.Add(bv.AsArray()[0].AsString(), bv.AsArray()[1].AsString());
                        }
                        Block capblock = world.GetBlock(bfull.CodeWithVariants(rdict));
                        BlockPos capspot = new BlockPos(b["x"].AsInt(), b["y"].AsInt(), b["z"].AsInt()) + corpos;
                        world.BlockAccessor.ExchangeBlock(capblock.Id, capspot);
                        world.BlockAccessor.SpawnBlockEntity("StoneStorageCapBE", capspot);
                        (world.BlockAccessor.GetBlockEntity(capspot) as GenericStorageCapBE).core = corpos;
                        (be as GenericStorageCoreBE).caps.Add(capspot);
                    }
                }
            }

            return true;
        }
    }
}