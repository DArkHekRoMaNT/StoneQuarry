using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace StoneQuarry
{
    public class RubbleStorageBE : GenericStorageCoreBE
    {
        public static readonly string[] allowedTypes = {
            "andesite",
            "chalk" ,
            "chert",
            "conglomerate",
            "limestone",
            "claystone",
            "granite",
            "sandstone",
            "shale",
            "basalt",
            "peridotite",
            "phyllite",
            "slate",
            "bauxite"
        };

        public string storedType = "";
        public EnumStorageLock storageLock = EnumStorageLock.None;
        public string lastAdded = "";
        public int MaxStorable { get { return Block.Attributes["maxStorable"].AsInt(0); } }

        public IDictionary<string, int> storage = new Dictionary<string, int>()
        {
            { "sand", 0 },
            { "gravel", 0 },
            { "stone", 0 },
        };

        public void CheckDisplayVariant(IWorldAccessor world, BlockSelection blocksel)
        {
            //Set's the displayed block to the type that has the largest amount of stored material.
            RubbleStorageBlock cblock = world.BlockAccessor.GetBlock(blocksel.Position) as RubbleStorageBlock;


            string bshouldbe = "empty";
            string maxstored = "";
            int maxAmountStored = 0;

            Dictionary<string, string> changeDict;

            foreach (KeyValuePair<string, int> i in storage)
            {
                if (i.Value > maxAmountStored)
                {
                    maxAmountStored = i.Value;
                    maxstored = i.Key;
                }
            }

            if (maxAmountStored > 0)
            {
                bshouldbe = maxstored;

                changeDict = new Dictionary<string, string>()
                { { "type", bshouldbe }, { "stone", storedType} };
            }
            else
            {
                changeDict = new Dictionary<string, string>()
                { { "type", bshouldbe }, { "stone", storedType} };

                storedType = "";
            }


            if (bshouldbe != cblock.FirstCodePart(2))
            {
                cblock.SwitchVariant(world, blocksel, changeDict);
            }
        }

        public bool RemoveResource(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, string stoneType, int quant)
        {
            if (storedType == "" || storage[stoneType] <= 0)
            {
                return false;
            }
            if (quant > storage[stoneType])
            {
                quant = storage[stoneType];
            }

            ItemStack givestack;
            if (stoneType == "sand" || stoneType == "gravel")
            {
                Block filler = world.GetBlock(new AssetLocation("game", stoneType + "-" + storedType));
                givestack = new ItemStack(filler, Math.Min(filler.MaxStackSize, quant));
            }
            else
            {
                Item filler = world.GetItem(new AssetLocation("game", stoneType + "-" + storedType));
                givestack = new ItemStack(filler, Math.Min(filler.MaxStackSize, quant));
            }

            if (!byPlayer.InventoryManager.TryGiveItemstack(givestack.Clone()))
            {
                world.SpawnItemEntity(givestack.Clone(), blockSel.HitPosition + (blockSel.HitPosition.Normalize() * .5) + blockSel.Position.ToVec3d(), blockSel.HitPosition.Normalize() * .05);
            }
            storage[stoneType] -= givestack.StackSize;
            return true;
        }

        public bool AddResource(ItemSlot slot, int quantity)
        {
            string blockType = "";
            string rockType = "";
            string lastType = "";

            if (storage["stone"] + storage["gravel"] + storage["sand"] + quantity > MaxStorable)
            {
                quantity = MaxStorable - (storage["stone"] + storage["gravel"] + storage["sand"]);
                if (quantity == 0)
                { return false; }
            }
            if (slot.Itemstack.Item == null)
            {
                rockType = slot.Itemstack.Block.FirstCodePart(1);
                blockType = slot.Itemstack.Block.FirstCodePart(0);
                lastType = slot.Itemstack.Block.Code.Path;
            }
            else
            {
                rockType = slot.Itemstack.Item.FirstCodePart(1);
                blockType = slot.Itemstack.Item.FirstCodePart(0);
                lastType = slot.Itemstack.Item.Code.Path;
            }

            if (storedType == "" && allowedTypes.Any(rockType.Contains))
            {
                storedType = rockType;
            }
            if (storedType == rockType)
            {
                if (slot.Itemstack.StackSize - quantity < 0)
                {
                    quantity = slot.Itemstack.StackSize;
                }
                switch (blockType)
                {
                    case "sand":
                        storage["sand"] += quantity;
                        break;

                    case "gravel":
                        storage["gravel"] += quantity;
                        break;

                    case "stone":
                        storage["stone"] += quantity;
                        break;

                    default:
                        return false;
                }
                slot.TakeOut(quantity);
                lastAdded = lastType;
                return true;
            }
            return false;
        }

        public bool AddAll(IPlayer byPlayer)
        {
            // will attempt to add all of a set item type from the players inventory.
            bool psound = false;
            foreach (ItemSlot slot in byPlayer.InventoryManager.GetHotbarInventory())
            {
                if (slot.Itemstack != null && slot.Itemstack.Collectible.Code.Path == lastAdded)
                {
                    if (AddResource(slot, slot.StackSize))
                    {
                        psound = true;
                    }
                }
            }
            foreach (KeyValuePair<string, IInventory> inv in byPlayer.InventoryManager.Inventories)
            {
                if (!inv.Key.Contains("creative"))
                {
                    foreach (ItemSlot slot in inv.Value)
                    {
                        if (slot.Itemstack != null && slot.Itemstack.Collectible.Code.Path == lastAdded)
                        {
                            if (AddResource(slot, slot.StackSize))
                            {
                                psound = true;
                            }
                        }
                    }
                }
            }

            return psound;
        }

        public bool Degrade()
        {
            switch (storageLock)
            {
                case EnumStorageLock.Stone:
                    return Degrade("gravel", "sand");

                case EnumStorageLock.Gravel:
                    return Degrade("stone", "gravel");

                case EnumStorageLock.Sand:
                    return Degrade("gravel", "sand") || Degrade("stone", "sand", false);

                default:
                    return Degrade("stone", "sand", true) || Degrade("gravel", "sand");

            }
        }

        private bool Degrade(string from, string to, bool split = true)
        {
            if (from == "stone" && storage["stone"] > 1)
            {
                if (to == "gravel")
                {
                    storage["gravel"] += 1;
                }
                else if (to == "sand")
                {
                    bool toSand = storage["gravel"] > storage["sand"] * 2 && split && !(to == "gravel");
                    storage[toSand ? "sand" : "gravel"] += 1;
                }
                else return false;

                storage["stone"] -= 2;
                return true;
            }

            if (from == "gravel" && to == "sand" && storage["gravel"] > 0)
            {
                storage["gravel"] -= 1;
                storage["sand"] += 1;
                return true;
            }

            return false;
        }

        public bool TryDrench(IWorldAccessor world, BlockSelection blockSel, IPlayer byPlayer)
        {
            var portion = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetTreeAttribute("contents");
            ItemStack portionStack = portion.GetItemstack("0");

            if (portionStack.Collectible.Code.Domain == "game" && portionStack.Collectible.Code.Path == "waterportion")
            {
                Block dropblock = world.GetBlock(new AssetLocation("game", "muddygravel"));
                if (storage["gravel"] > 0)
                {
                    ItemStack dropStack = new ItemStack(dropblock, Math.Min(dropblock.MaxStackSize / 4, storage["gravel"]));//Deviding max stack drop by four because not doing so created a mess.
                    world.SpawnItemEntity(dropStack.Clone(), blockSel.Position.ToVec3d() + blockSel.HitPosition, (blockSel.Face.Normalf.ToVec3d() * .05) + new Vec3d(0, .08, 0));
                    storage["gravel"] -= Math.Min(dropblock.MaxStackSize / 4, storage["gravel"]);

                    portionStack.StackSize--;
                    if (portionStack.StackSize <= 0)
                    {
                        portionStack = null;
                    }

                    portion.SetItemstack("0", portionStack);
                    return true;
                }
            }

            return false;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            storedType = tree.GetString("storedType", "");
            storage["sand"] = tree.GetInt("sand", 0);
            storage["gravel"] = tree.GetInt("gravel", 0);
            storage["stone"] = tree.GetInt("stone", 0);
            storageLock = (EnumStorageLock)tree.GetInt("storageLock", 0);

            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetString("storedType", storedType);
            tree.SetInt("sand", storage["sand"]);
            tree.SetInt("gravel", storage["gravel"]);
            tree.SetInt("stone", storage["stone"]);
            tree.SetInt("storageLock", (int)storageLock);
            base.ToTreeAttributes(tree);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            string stone = Lang.Get(Block.Code.Domain + ":rubblestorage-stone(count={0})", storage["stone"]);
            string gravel = Lang.Get(Block.Code.Domain + ":rubblestorage-gravel(count={0})", storage["gravel"]);
            string sand = Lang.Get(Block.Code.Domain + ":rubblestorage-sand(count={0})", storage["sand"]);

            string locked = Lang.Get(Block.Code.Domain + ":rubblestorage-locked");
            switch (storageLock)
            {
                case EnumStorageLock.Stone:
                    stone += locked;
                    break;
                case EnumStorageLock.Gravel:
                    gravel += locked;
                    break;
                case EnumStorageLock.Sand:
                    sand += locked;
                    break;
            }

            string rockType = storedType == "" ? Lang.Get(Block.Code.Domain + ":rubblestorage-none") : Lang.Get("rock-" + storedType);
            dsc.AppendLine(Lang.Get(Block.Code.Domain + ":rubblestorage-type(type={0})", rockType));

            dsc.AppendLine(stone);
            dsc.AppendLine(gravel);
            dsc.AppendLine(sand);
        }
    }

    public enum EnumStorageLock : int
    {
        None = 0,
        Sand = 1,
        Gravel = 2,
        Stone = 3
    }
}