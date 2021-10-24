using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace QuarryWorks
{
    public class ChunksItem : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            IClientPlayer spry = byPlayer as IClientPlayer;

            IWorldAccessor world = byEntity.World;
            IBlockAccessor bacc = world.BlockAccessor;

            List<BlockPos> plist = new List<BlockPos>();



            if (bacc.GetBlockId(blockSel.Position + blockSel.Face.Normali.AsBlockPos) == 0)
            {
                Debug.WriteLine("airblock good to place thing here");
                Dictionary<string, string> rplace = new Dictionary<string, string>();
                rplace.Add("rock", "clay");
                rplace.Add("size", "oogle");
                Debug.WriteLine(CodeWithVariants(rplace));
            }


            plist.Add(blockSel.Position + blockSel.Face.Normali.AsBlockPos);
            byEntity.World.HighlightBlocks(byPlayer, 0, plist);

            if (spry == null)
            {
                Debug.WriteLine("spry is null");
                return;
            }
            spry.ShowChatNotification(slot.Itemstack.Attributes.GetInt("stonestored").ToString());


            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }
    }
}