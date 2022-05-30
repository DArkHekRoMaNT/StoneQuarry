using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace StoneQuarry
{
    public class ItemRubbleHammer : Item
    {
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (blockSel != null)
            {
                Block selectedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
                IRockManager rockManager = api.ModLoader.GetModSystem<RockManager>();

                if (rockManager.TryResolveCode(selectedBlock.Code, out string? type, out AssetLocation? rock))
                {
                    AssetLocation? newBlockCode = null;

                    switch (type)
                    {
                        case "rock": newBlockCode = rockManager.GetValue(rock, "gravel"); break;
                        case "gravel": newBlockCode = rockManager.GetValue(rock, "sand"); break;
                    }

                    if (newBlockCode != null)
                    {
                        Block? block = api.World.GetBlock(newBlockCode);
                        if (block != null)
                        {                            
                            if (api.Side == EnumAppSide.Server)
                            {
                                IServerPlayer? player = ((EntityPlayer)byEntity)?.Player as IServerPlayer;
                                if (player != null && api.World.IsPlayerCanBreakBlock(blockSel.Position, player))
                                {
                                    api.World.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
                                    api.World.BlockAccessor.MarkBlockModified(blockSel.Position);
                                }
                            }
                            handling = EnumHandHandling.Handled;
                            return;
                        }
                    }
                }
            }

            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
        }
    }
}
