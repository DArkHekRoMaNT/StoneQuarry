using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace QuarryWorks
{
    public class GenericStoneStorageBlock : Block
    {
        static readonly SimpleParticleProperties breakParticle = new SimpleParticleProperties()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(1, 1, 1),
            MinQuantity = 2,
            AddQuantity = 12,
            GravityEffect = 1f,
            WithTerrainCollision = true,
            ParticleModel = EnumParticleModel.Quad,
            LifeLength = 2f,
            MinVelocity = new Vec3f(-0.4f, -0.4f, -0.4f),
            AddVelocity = new Vec3f(0.8f, 1.2f, 0.8f),
            MinSize = 0.2f,
            MaxSize = 0.5f,
            DieOnRainHeightmap = false
        };

        public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
        {
            BlockFacing[] sughv = SuggestedHVOrientation(byPlayer, blockSel);
            Block ablock = world.GetBlock(CodeWithVariant("dir", sughv[0].Code));

            if (ablock != null && ablock.Attributes != null && ablock.Attributes.KeyExists("caps"))
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

            if (ablock != null)
            {
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
            }
            return true;
        }
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            BlockPos masterPos = pos;

            if (be is GenericStorageCapBE)
            {
                masterPos = (be as GenericStorageCapBE).core;
                be = world.BlockAccessor.GetBlockEntity(masterPos);
            }

            if (be == null)
            {
                world.BlockAccessor.SetBlock(0, pos);
                return;
            }


            GenericStorageCoreBE core = be as GenericStorageCoreBE;
            foreach (BlockPos cap in core.caps)
            {
                breakParticle.MinPos = cap.ToVec3d();
                breakParticle.ColorByBlock = world.BlockAccessor.GetBlock(masterPos);
                world.BlockAccessor.SetBlock(0, cap);
                world.BlockAccessor.RemoveBlockEntity(cap);
                world.SpawnParticles(breakParticle, byPlayer);
            }

            world.BlockAccessor.SetBlock(0, masterPos);
        }

        public bool SwitchVariant(IWorldAccessor world, BlockSelection blockSel, Dictionary<string, string> switchArray)
        {
            //Switches multiblock to a separate variants.
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