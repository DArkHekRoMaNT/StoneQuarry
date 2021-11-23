using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace StoneQuarry
{
    public class BEGenericMultiblockPart : BlockEntity
    {
        public BlockPos CorePos { get; set; } = null;
        public List<BlockPos> Caps { get; set; } = new List<BlockPos>();

        /// <summary>
        /// Returns the core BE of the given multiblock or this BE (if this is a core or if the core does not exist)
        /// </summary>
        public BEGenericMultiblockPart Core
        {
            get
            {
                if (!IsCore)
                {
                    if (Api.World.BlockAccessor.GetBlockEntity(CorePos) is BEGenericMultiblockPart core)
                    {
                        return core;
                    }
                }
                return this;
            }
        }
        public bool IsCore => CorePos == null || CorePos == Pos;


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            if (tree.HasAttribute("capCount"))
            {
                for (int i = 0; i < tree.GetInt("capCount"); i++)
                {
                    Caps.Add(new BlockPos(tree.GetInt("cap" + i + "x"), tree.GetInt("cap" + i + "y"), tree.GetInt("cap" + i + "z")));
                }
            }
            if (tree.HasAttribute("capx"))
            {
                CorePos = new BlockPos(tree.GetInt("capx"), tree.GetInt("capy"), tree.GetInt("capz"));
            }
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if (Caps.Count > 0)
            {
                tree.SetInt("capCount", Caps.Count);
                for (int i = 0; i < Caps.Count; i++)
                {
                    tree.SetInt("cap" + i + "x", Caps[i].X);
                    tree.SetInt("cap" + i + "y", Caps[i].Y);
                    tree.SetInt("cap" + i + "z", Caps[i].Z);
                }
            }
            if (CorePos != null)
            {
                tree.SetInt("capx", CorePos.X);
                tree.SetInt("capy", CorePos.Y);
                tree.SetInt("capz", CorePos.Z);
            }
            base.ToTreeAttributes(tree);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (!IsCore)
            {
                Core.GetBlockInfo(forPlayer, dsc);
            }
            else base.GetBlockInfo(forPlayer, dsc);
        }
    }

}