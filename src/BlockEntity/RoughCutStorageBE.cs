using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace QuarryWorks
{
    public class RoughCutStorageBE : GenericStorageCoreBE
    {
        //used to store the Item used to create this block.
        public ItemStack blockStack = null;

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            if (tree.HasAttribute("istack"))
            {
                blockStack = tree.GetItemstack("istack");
                blockStack.ResolveBlockOrItem(worldAccessForResolve);
            }
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if (blockStack != null)
            {
                tree.SetItemstack("istack", blockStack);
            }
            base.ToTreeAttributes(tree);
        }
    }

}