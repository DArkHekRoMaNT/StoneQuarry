using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace StoneQuarry
{
    public class BlockGenericMultiblockPart : Block
    {
        readonly SimpleParticleProperties breakParticle = new SimpleParticleProperties()
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
            Block block = world.GetBlock(CodeWithVariant("dir", sughv[0].Code));

            if (block?.Attributes?.KeyExists("caps") ?? false)
            {
                for (int i = 0; i < block.Attributes["caps"].AsArray().Length; i++)
                {
                    var capBlockSel = blockSel.Clone();
                    capBlockSel.Position += new BlockPos(
                        block.Attributes["caps"].AsArray()[i]["x"].AsInt(),
                        block.Attributes["caps"].AsArray()[i]["y"].AsInt(),
                        block.Attributes["caps"].AsArray()[i]["z"].AsInt()
                    );

                    if (!base.CanPlaceBlock(world, byPlayer, capBlockSel, ref failureCode))
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

                        Block capBlock = world.GetBlock(CodeWithVariants(rdict));
                        int x = ablock.Attributes["caps"].AsArray()[i]["x"].AsInt();
                        int y = ablock.Attributes["caps"].AsArray()[i]["y"].AsInt();
                        int z = ablock.Attributes["caps"].AsArray()[i]["z"].AsInt();
                        BlockPos capPos = blockSel.Position.Copy() + new BlockPos(x, y, z);
                        world.BlockAccessor.ExchangeBlock(capBlock.Id, capPos);

                        world.BlockAccessor.SpawnBlockEntity("GenericMultiblockPart", capPos);
                        (world.BlockAccessor.GetBlockEntity(capPos) as BEGenericMultiblockPart).CorePos = blockSel.Position;
                        (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEGenericMultiblockPart).Caps.Add(capPos);

                        world.BlockAccessor.MarkBlockDirty(capPos);
                        world.BlockAccessor.MarkBlockEntityDirty(capPos);
                    }
                }
            }
            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var core = (world.BlockAccessor.GetBlockEntity(pos) as BEGenericMultiblockPart)?.Core;
            if (core == null)
            {
                world.BlockAccessor.SetBlock(0, pos);
                return;
            }


            foreach (BlockPos cap in core.Caps)
            {
                breakParticle.MinPos = cap.ToVec3d();
                breakParticle.ColorByBlock = world.BlockAccessor.GetBlock(core.Pos);
                world.BlockAccessor.SetBlock(0, cap);
                world.BlockAccessor.RemoveBlockEntity(cap);
                world.SpawnParticles(breakParticle, byPlayer);
            }
            world.BlockAccessor.SetBlock(0, core.Pos);
        }

        /// <summary> Switches multiblock to a separate variants </summary>
        public bool SwitchVariant(IWorldAccessor world, BlockSelection blockSel, Dictionary<string, string> newVariants)
        {
            var corePos = blockSel.Position;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEGenericMultiblockPart be)
            {
                // If it is cap, call the core
                if (!be.IsCore)
                {
                    blockSel.Position = be.CorePos;
                    (be.Core.Block as BlockGenericMultiblockPart).SwitchVariant(world, blockSel, newVariants);
                    return true;
                }

                // Otherwise, remove all caps
                foreach (BlockPos slave in be.Caps)
                {
                    world.BlockAccessor.SetBlock(0, slave);
                }
                be.Caps = new List<BlockPos>();


                Block newBlock = world.GetBlock(CodeWithVariants(newVariants));
                if (newBlock?.Attributes?.KeyExists("caps") ?? false)
                {
                    world.BlockAccessor.ExchangeBlock(newBlock.Id, corePos);
                    foreach (JsonObject block in newBlock.Attributes["caps"].AsArray())
                    {
                        Dictionary<string, string> rightVariants = new Dictionary<string, string>();
                        foreach (JsonObject variant in block["varType"].AsArray())
                        {
                            rightVariants.Add(variant.AsArray()[0].AsString(), variant.AsArray()[1].AsString());
                        }
                        Block capBlock = world.GetBlock(newBlock.CodeWithVariants(rightVariants));
                        BlockPos capPos = new BlockPos(block["x"].AsInt(), block["y"].AsInt(), block["z"].AsInt()) + corePos;
                        world.BlockAccessor.ExchangeBlock(capBlock.Id, capPos);
                        world.BlockAccessor.SpawnBlockEntity("GenericMultiblockPart", capPos);
                        (world.BlockAccessor.GetBlockEntity(capPos) as BEGenericMultiblockPart).CorePos = corePos;
                        be.Caps.Add(capPos);
                    }
                }
            }

            return true;
        }
    }
}