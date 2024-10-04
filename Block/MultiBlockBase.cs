using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace StoneQuarry
{
    public class MultiBlockBase : Block, IMultiBlockColSelBoxes, IMultiBlockBlockBreaking, IMultiBlockInteract
    {
        public virtual Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        {
            return GetCollisionBoxes(blockAccessor, pos + offset.AsBlockPos);
        }

        public virtual Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
        {
            return GetSelectionBoxes(blockAccessor, pos + offset.AsBlockPos);
        }

        public virtual int MBGetColorWithoutTint(ICoreClientAPI capi, BlockPos pos, Vec3i offsetInv)
        {
            return GetColorWithoutTint(capi, pos + offsetInv.AsBlockPos);
        }

        public virtual int MBGetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex, Vec3i offsetInv)
        {
            return GetRandomColor(capi, pos + offsetInv.AsBlockPos, facing, rndIndex);
        }

        public virtual void MBOnBlockBroken(IWorldAccessor world, BlockPos pos, Vec3i offset, IPlayer byPlayer, float dropQuantityMultiplier)
        {
            OnBlockBroken(world, pos + offset.AsBlockPos, byPlayer, dropQuantityMultiplier);
        }

        public virtual float MBOnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter, Vec3i offsetInv)
        {
            BlockSelection coreBlockSel = blockSel.Clone();
            coreBlockSel.Position += offsetInv.AsBlockPos;
            return OnGettingBroken(player, coreBlockSel, itemslot, remainingResistance, dt, counter);
        }

        public virtual bool MBDoParticalSelection(IWorldAccessor world, BlockPos pos, Vec3i offset)
        {
            return DoParticalSelection(world, pos + offset.AsBlockPos);
        }

        public virtual WorldInteraction[] MBGetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer, Vec3i offset)
        {
            var coreBlockSel = blockSel.Clone();
            coreBlockSel.Position += offset.AsBlockPos;
            return GetPlacedBlockInteractionHelp(world, coreBlockSel, forPlayer);
        }

        public virtual bool MBOnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason, Vec3i offset)
        {
            var coreBlockSel = blockSel.Clone();
            coreBlockSel.Position += offset.AsBlockPos;
            return OnBlockInteractCancel(secondsUsed, world, byPlayer, coreBlockSel, cancelReason);
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

        public virtual ItemStack MBOnPickBlock(IWorldAccessor world, BlockPos pos, Vec3i offset)
        {
            return OnPickBlock(world, pos + offset.AsBlockPos);
        }

        public BlockSounds GetSounds(IBlockAccessor blockAccessor, BlockSelection blockSel, ItemStack stack, Vec3i offset)
        {
            BlockSelection coreBlockSel = blockSel.Clone();
            coreBlockSel.Position += offset.AsBlockPos;
            return GetSounds(blockAccessor, coreBlockSel, stack, offset);
        }
    }
}
