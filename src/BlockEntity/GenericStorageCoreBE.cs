using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace QuarryWorks
{
    public class GenericStorageCoreBE : BlockEntity
    {
        public List<BlockPos> caps = new List<BlockPos>(); // where the other half of this structure is.

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            if (tree.HasAttribute("capCount"))
            {
                for (int i = 0; i < tree.GetInt("capCount"); i++)
                {
                    caps.Add(new BlockPos(tree.GetInt("cap" + i + "x"), tree.GetInt("cap" + i + "y"), tree.GetInt("cap" + i + "z")));
                }
            }
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if (caps.Count > 0)
            {
                tree.SetInt("capCount", caps.Count);
                for (int i = 0; i < caps.Count; i++)
                {
                    tree.SetInt("cap" + i + "x", caps[i].X);
                    tree.SetInt("cap" + i + "y", caps[i].Y);
                    tree.SetInt("cap" + i + "z", caps[i].Z);
                }
            }
            base.ToTreeAttributes(tree);
        }
    }

}