using System.Diagnostics;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    public class ItemSlabContentSetter : Item
    {
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            interactions = new WorldInteraction[] {
                new WorldInteraction {
                    ActionLangCode = Code.Domain + ":wi-slabstacksetter-plus",
                    MouseButton = EnumMouseButton.Right
                },
                new WorldInteraction {
                    ActionLangCode = Code.Domain + ":wi-slabstacksetter-plus10",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sprint"
                },
                new WorldInteraction {
                    ActionLangCode = Code.Domain + ":wi-slabstacksetter-minus",
                    MouseButton = EnumMouseButton.Left,
                    HotKeyCode = "sneak"
                },
                new WorldInteraction {
                    ActionLangCode = Code.Domain + ":wi-slabstacksetter-minus10",
                    MouseButton = EnumMouseButton.Left,
                    HotKeyCodes = new string[] {"sprint", "sneak"}
                },
                new WorldInteraction {
                    ActionLangCode = Code.Domain + ":wi-slabstacksetter-apply",
                    MouseButton = EnumMouseButton.Right
                }
            };
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer player)
            {
                byPlayer = byEntity.World.PlayerByUid(player.PlayerUID);
            }
            if (byPlayer == null) return;


            int quantity = slot.Itemstack.Attributes.GetInt("quantity", 0);

            if (blockSel != null)
            {
                var be = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BEGenericMultiblockPart;
                if (be?.Core is BERoughCutStorage rsbe)
                {
                    if (quantity <= 0) rsbe.blockStack = null;
                    else if (rsbe.blockStack == null)
                    {
                        var block = rsbe.Block as BlockRoughCutStorage;
                        rsbe.blockStack = new ItemStack(block);
                        rsbe.blockStack.Attributes.SetInt("stonestored", quantity);
                    }
                    else rsbe.blockStack.Attributes.SetInt("stonestored", quantity);

                    handling = EnumHandHandling.PreventDefaultAction;
                    return;
                }
            }

            if (byPlayer.Entity.Controls.Sprint)
            {
                quantity += byPlayer.Entity.Controls.Sneak ? -10 : 10;
            }
            else
            {
                quantity += byPlayer.Entity.Controls.Sneak ? -1 : 1;
            }

            slot.Itemstack.Attributes.SetInt("quantity", quantity);
            (byPlayer as IClientPlayer)?.ShowChatNotification(quantity.ToString());

            handling = EnumHandHandling.PreventDefaultAction;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            int quantity = inSlot.Itemstack.Attributes.GetInt("quantity", 0);
            dsc.AppendLine(Lang.Get(Code.Domain + ":slabcontentsetter-quantity(quantity={0})", quantity));
        }
    }
}
