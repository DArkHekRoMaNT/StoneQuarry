using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace StoneQuarry
{
    public class RubbleStorageItemSlot : ItemSlot
    {
        public string Type { get; }

        private AssetLocation? RockName => ((RubbleStorageInventory)inventory).StoredRock;
        private IRockManager RockManager => ((RubbleStorageInventory)inventory).RockManager;

        public RubbleStorageItemSlot(int slotId, RubbleStorageInventory self) : base(self)
        {
            Type = slotId switch
            {
                0 => "stone",
                1 => "gravel",
                2 => "sand",
                _ => throw new ArgumentOutOfRangeException(nameof(slotId),
                    "RubbleStorageInventory should have no more than 3 slots")
            };
        }

        public void AddIn(int quantity)
        {
            if (RockName != null)
            {
                AssetLocation? code = RockManager.GetValue(RockName, Type);
                if (code != null)
                {
                    if (Empty)
                    {
                        CollectibleObject collObj = inventory.Api.World.GetCollectibleObject(code);
                        if (collObj != null)
                        {
                            Itemstack = new(collObj);
                            Itemstack.StackSize = quantity;
                        }
                    }
                    else
                    {
                        Itemstack.StackSize += quantity;
                    }
                }
            }
        }

        public bool TryGetFrom(ItemSlot? fromSlot, int quantity)
        {
            if (fromSlot == null)
            {
                return false;
            }

            ItemStack? fromStack = fromSlot.Itemstack;
            if (fromStack != null && fromStack.StackSize > 0)
            {
                if (Itemstack == null || fromStack.Collectible.Equals(fromStack, Itemstack, GlobalConstants.IgnoredStackAttributes))
                {
                    ItemStack stack = fromSlot.TakeOut(quantity);
                    if (Itemstack == null)
                    {
                        Itemstack = stack;
                    }
                    else
                    {
                        Itemstack.StackSize += stack.StackSize;
                    }
                    MarkDirty();
                    return true;
                }
            }

            return false;
        }
    }
}
