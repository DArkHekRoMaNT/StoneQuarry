using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    public class StoneSlabInventory : InventoryGeneric
    {
        public BaseAllowedCodes AllowedCodes { get; }

        private int _currentSlotId = -1;
        public int CurrentSlotId
        {
            get => _currentSlotId;
            set
            {
                _currentSlotId = value;
                MarkBEDirty();
            }
        }

        public string CurrentRock
        {
            get
            {
                if (slots.Length > CurrentSlotId && CurrentSlotId >= 0)
                {
                    if (!this[CurrentSlotId].Empty)
                    {
                        return this[CurrentSlotId].Itemstack.Collectible.Code.ToString();
                    }
                }

                CurrentSlotId = -1;
                return "";
            }
        }

        public StoneSlabPreset RenderPreset { get; private set; }

        public StoneSlabInventory(ICoreAPI api, BlockPos pos, BaseAllowedCodes allowedCodes, int quantitySlots = 0)
            : base(quantitySlots, "SQ_StoneSlab", pos?.ToString() ?? "-fake", api, OnNewSlot)
        {
            Pos = pos;
            AllowedCodes = allowedCodes;
        }

        public void NextSlot()
        {
            if (CurrentSlotId == -1)
            {
                var slot = FirstNonEmptySlot;
                if (slot != null)
                {
                    CurrentSlotId = GetSlotId(slot);
                }
            }
            else
            {
                int prevSlotId = CurrentSlotId;
                do // Cycle through all slots (including the current slot in the last iteration)
                {
                    CurrentSlotId++;
                    if (CurrentSlotId >= slots.Length)
                    {
                        CurrentSlotId = 0;
                    }

                    if (!this[CurrentSlotId].Empty)
                    {
                        return;
                    }

                } while (prevSlotId != CurrentSlotId);
                CurrentSlotId = -1; // If all slots are empty
            }
        }

        public ItemStack GetContent(IPlayer byPlayer, string type, NatFloat quantity, string rock = null)
        {
            if (rock == null)
            {
                if (CurrentSlotId == -1)
                {
                    NextSlot();
                    if (CurrentSlotId == -1)
                    {
                        return null;
                    }
                }

                rock = CurrentRock;
            }

            if (!AllowedCodes[rock].ContainsKey(type))
            {
                string errorCode = Core.ModId + ":ingameerror-stoneslab-unknown-tool";
                (byPlayer as IServerPlayer)?.SendIngameError("", Lang.Get(errorCode));
                return null;
            }

            foreach (var slot in slots)
            {
                if (!slot.Empty && slot.Itemstack.Collectible.Code.ToString() == rock)
                {
                    if (TryGetNextTypedStack(type, rock, quantity, out ItemStack stack))
                    {
                        slot.TakeOut(1);
                        slot.MarkDirty();

                        if (slot.Empty)
                        {
                            NextSlot();
                        }

                        MarkBEDirty();
                        return stack;
                    }
                    else
                    {
                        string errorCode = Core.ModId + ":ingameerror-stoneslab-unknown-drop";
                        (byPlayer as IServerPlayer)?.SendIngameError("", Lang.Get(errorCode));
                        return null;
                    }
                }
            }

            return null;
        }

        private void MarkBEDirty()
        {
            if (Pos != null && Api.World.BlockAccessor.GetBlockEntity(Pos) is BEStoneSlab be)
            {
                be?.RenderPreset?.Update(this, be.Block);
                be?.MarkDirty(true);
            }
        }

        private bool TryGetNextTypedStack(string type, string rock, NatFloat quantity, out ItemStack stack)
        {
            var dropCode = new AssetLocation(AllowedCodes[rock, type]);
            var dropCollectible = Api.World.GetCollectibleObject(dropCode);

            if (dropCollectible == null)
            {
                stack = null;
                return false;
            }

            var dropStack = new ItemStack(dropCollectible);
            var blockDrop = new BlockDropItemStack(dropStack) { Quantity = quantity };

            stack = blockDrop.GetNextItemStack();
            return true;
        }

        public bool TryAddStack(ItemStack byStack)
        {
            if (byStack == null)
            {
                return false;
            }

            foreach (var slot in slots)
            {
                if (!slot.Empty && slot.Itemstack.Collectible.Code.Equals(byStack.Collectible.Code))
                {
                    slot.Itemstack.StackSize += byStack.StackSize;
                    slot.MarkDirty();
                    MarkBEDirty();
                    return true;
                }
            }

            foreach (var slot in slots)
            {
                if (slot.Empty)
                {
                    slot.Itemstack = byStack.Clone();
                    slot.MarkDirty();
                    MarkBEDirty();
                    return true;
                }
            }

            var newSlot = NewSlot(slots.Length);
            slots = slots.Append(newSlot);
            newSlot.Itemstack = byStack.Clone();
            newSlot.MarkDirty();
            MarkBEDirty();
            return true;
        }

        public bool TryRemoveStack(ItemStack byStack)
        {
            if (byStack == null)
            {
                var slot = FirstNonEmptySlot;
                if (slot != null)
                {
                    slot.Itemstack = null;
                    slot.MarkDirty();
                    MarkBEDirty();
                    return true;
                }
                return false;
            }

            foreach (var slot in slots)
            {
                if (!slot.Empty && slot.Itemstack.Collectible.Code.Equals(byStack.Collectible.Code))
                {
                    if (slot.StackSize <= byStack.StackSize)
                    {
                        slot.Itemstack = null;
                    }
                    else
                    {
                        slot.Itemstack.StackSize -= byStack.StackSize;
                    }
                    slot.MarkDirty();
                    MarkBEDirty();
                    return true;
                }
            }

            return false;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("currentslotid", CurrentSlotId);
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            base.FromTreeAttributes(tree);
            CurrentSlotId = tree.GetInt("currentslotid", -1);
        }

        public static StoneSlabInventory StacksToTreeAttributes(List<ItemStack> stacks, ITreeAttribute tree, ICoreAPI api, BaseAllowedCodes allowedCodes)
        {
            var inv = new StoneSlabInventory(api, null, allowedCodes, stacks.Count);

            for (int i = 0; i < stacks.Count; i++)
            {
                inv[i].Itemstack = stacks[i];
                inv.MarkSlotDirty(i);
            }

            inv.ToTreeAttributes(tree);
            return inv;
        }

        private static ItemSlot OnNewSlot(int slotId, InventoryGeneric self)
        {
            return new ItemSlotUniversal(self)
            {
                MaxSlotStackSize = int.MaxValue
            };
        }
    }
}
