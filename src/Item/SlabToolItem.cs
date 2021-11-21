using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace StoneQuarry
{
    public class SlabToolItem : Item
    {
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            string result = "";

            if (Attributes.KeyExists("polishedrrate")) result = Lang.Get(Code.Domain + ":slabtool-type-polished");
            else if (Attributes.KeyExists("rockrrate")) result = Lang.Get(Code.Domain + ":slabtool-type-rock");
            else if (Attributes.KeyExists("stonerrate")) result = Lang.Get(Code.Domain + ":slabtool-type-stone");
            else if (Attributes.KeyExists("brickrrate")) result = Lang.Get(Code.Domain + ":slabtool-type-brick");

            if (result != "")
            {
                dsc.AppendLine(Lang.Get(Code.Domain + ":slabtool-heldinfo(result={0})", result));
            }
        }
    }
}