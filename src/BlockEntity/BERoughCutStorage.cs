using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace StoneQuarry
{
    public class BERoughCutStorage : BEGenericMultiblockPart
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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            var count = blockStack != null ? blockStack.Attributes.GetInt("stonestored") : 0;
            var stone = Lang.Get("rock-" + Block.FirstCodePart(1));

            dsc.AppendLine(Lang.Get(Block.Code.Domain + ":info-stonestorage-heldinfo(count={0},stone={1})", count, stone));
        }
    }
}