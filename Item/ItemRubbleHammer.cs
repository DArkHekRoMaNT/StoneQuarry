using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace StoneQuarry
{
    public class ItemRubbleHammer : Item
    {
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (blockSel == null)
            {
                base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
                return;
            }

            if (TryGetConvertedBlock(blockSel.Position, out var block))
            {
                handling = EnumHandHandling.Handled;
                return;
            }

            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null)
                return false;

            if (TryGetConvertedBlock(blockSel.Position, out var block))
            {
                if (((EntityPlayer)byEntity)?.Player is IPlayer player)
                {
                    var reinforcementSystem = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
                    var reinforcment = reinforcementSystem.GetReinforcment(blockSel.Position);
                    if (reinforcment != null && reinforcment.Strength > 0)
                    {
                        if (secondsPassed > 1)
                        {
                            api.World.PlaySoundAt(new AssetLocation("sounds/tool/breakreinforced"), blockSel.Position, 0.0, player);
                            if (!player.HasPrivilege("denybreakreinforced") && api.Side == EnumAppSide.Server)
                            {
                                reinforcementSystem.ConsumeStrength(blockSel.Position, 1);
                                api.World.BlockAccessor.MarkBlockDirty(blockSel.Position);
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else if (api.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
                    {
                        api.World.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
                        api.World.BlockAccessor.MarkBlockModified(blockSel.Position);
                    }
                }
                return false;
            }
            return true;
        }

        private bool TryGetConvertedBlock(BlockPos pos, [NotNullWhen(true)] out Block? block)
        {
            var selectedBlock = api.World.BlockAccessor.GetBlock(pos);
            var rockManager = (IRockManager)api.ModLoader.GetModSystem<RockManager>();
            if (rockManager.TryResolveCode(selectedBlock.Code, out string? type, out AssetLocation? rock))
            {
                AssetLocation? newBlockCode = type switch
                {
                    "rock" => rockManager.GetValue(rock, "gravel"),
                    "gravel" => rockManager.GetValue(rock, "sand"),
                    _ => null
                };

                if (newBlockCode != null)
                {
                    block = api.World.GetBlock(newBlockCode);
                    return block != null;
                }
            }
            block = null;
            return false;
        }
    }
}
