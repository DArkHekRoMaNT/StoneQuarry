using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace StoneQuarry
{
    /// <summary>
    /// Basic implementation of IMultiBlockModular
    /// </summary>

    public class BaseMultiblock : Block, IMultiBlockColSelBoxes
    {
        public virtual Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        {
            return GetCollisionBoxes(blockAccessor, pos + offset.AsBlockPos);
        }

        public virtual Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        {
            return GetSelectionBoxes(blockAccessor, pos + offset.AsBlockPos);
        }

        public virtual void MBDoParticalSelection(IWorldAccessor world, BlockPos pos, Vec3i offset)
        {
            DoParticalSelection(world, pos + offset.AsBlockPos);
        }

        public virtual bool MBOnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset)
        {
            var coreBlockSel = blockSel.Clone();
            coreBlockSel.Position += offset.AsBlockPos;
            return OnBlockInteractStart(world, byPlayer, coreBlockSel);
        }

        public virtual bool MBOnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset)
        {
            var coreBlockSel = blockSel.Clone();
            coreBlockSel.Position += offset.AsBlockPos;
            return OnBlockInteractStep(secondsUsed, world, byPlayer, coreBlockSel);
        }

        public virtual void MBOnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset)
        {
            var coreBlockSel = blockSel.Clone();
            coreBlockSel.Position += offset.AsBlockPos;
            OnBlockInteractStop(secondsUsed, world, byPlayer, coreBlockSel);
        }

        public virtual bool MBOnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason, Vec3i offset)
        {
            var coreBlockSel = blockSel.Clone();
            coreBlockSel.Position += offset.AsBlockPos;
            return OnBlockInteractCancel(secondsUsed, world, byPlayer, coreBlockSel, cancelReason);
        }

        public virtual ItemStack MBOnPickBlock(IWorldAccessor world, BlockPos pos, Vec3i offset)
        {
            return OnPickBlock(world, pos + offset.AsBlockPos);
        }

        public virtual WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer, Vec3i offset)
        {
            var coreBlockSel = blockSel.Clone();
            coreBlockSel.Position += offset.AsBlockPos;
            return GetPlacedBlockInteractionHelp(world, coreBlockSel, forPlayer);
        }

        public void OnBlockBroken(IWorldAccessor world, BlockPos pos, Vec3i offset, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            OnBlockBroken(world, pos + offset.AsBlockPos, byPlayer, dropQuantityMultiplier);
        }

        public void OnTesselation(ITerrainMeshPool mesher)
        {
            throw new NotImplementedException();
        }
    }
}
