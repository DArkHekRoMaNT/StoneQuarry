using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace StoneQuarry
{
    public class ItemSlabTool : Item
    {
        public string ToolType => Attributes["slabtool"]?["type"]?.AsString() ?? "";
        public NatFloat Quantity => Attributes["slabtool"]?["quantity"]?.AsObject(NatFloat.One);


        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (ToolType != "")
            {
                var result = Lang.Get(Code.Domain + ":info-slabtool-type-" + ToolType);
                dsc.AppendLine(Lang.Get(Code.Domain + ":info-slabtool-heldinfo(result={0})", result));
            }
        }
    }
}