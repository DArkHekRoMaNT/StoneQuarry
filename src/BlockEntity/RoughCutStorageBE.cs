using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace QuarryWorks
{
    public class RoughCutStorageBE : GenericStorageCoreBE
    {
        //used to store the Item used to create this block.
        public ItemStack istack = null;

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            if (tree.HasAttribute("istack"))
            {
                istack = tree.GetItemstack("istack");
                istack.ResolveBlockOrItem(worldAccessForResolve);
            }
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if (istack != null)
            {
                tree.SetItemstack("istack", istack);
            }
            base.ToTreeAttributes(tree);
        }
    }

}