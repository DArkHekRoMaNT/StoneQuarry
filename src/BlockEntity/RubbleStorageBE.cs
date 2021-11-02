using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace QuarryWorks
{
    public class RubbleStorageBE : GenericStorageCoreBE
    {
        string[] allowedTypes = {
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
        public enum StorageLocksEnum : int
        {
            None = 0,
            Sand = 1,
            Gravel = 2,
            Stone = 3
        }
        public string storedType = "";
        public StorageLocksEnum storageLock = StorageLocksEnum.None;
        public string lastAdded = "";
        public int maxStorable = 0;

        public IDictionary<string, int> storedtypes = new Dictionary<string, int>()
        {
            { "sand", 0},
            { "gravel", 0},
            { "stone", 0},
        };

        public void CheckDisplayVariant(IWorldAccessor world, BlockSelection blocksel)
        {
            //Set's the displayed block to the type that has the largest amount of stored material.
            RubbleStorageBlock cblock = world.BlockAccessor.GetBlock(blocksel.Position) as RubbleStorageBlock;


            string bshouldbe = "empty";
            string maxstored = "";
            int maxAmountStored = 0;

            Dictionary<string, string> changeDict;

            foreach (KeyValuePair<string, int> i in storedtypes)
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

        public bool RemoveResource(IWorldAccessor world, IPlayer byplayer, BlockSelection blockSel, string stype, int quant)
        {
            if (storedType == "" || storedtypes[stype] <= 0)
            {
                return false;
            }
            if (quant > storedtypes[stype])
            {
                quant = storedtypes[stype];
            }

            ItemStack givestack;
            if (stype == "sand" || stype == "gravel")
            {
                Block filler = world.GetBlock(new AssetLocation("game", stype + "-" + storedType));
                givestack = new ItemStack(filler, Math.Min(filler.MaxStackSize, quant));
            }
            else
            {
                Item filler = world.GetItem(new AssetLocation("game", stype + "-" + storedType));
                givestack = new ItemStack(filler, Math.Min(filler.MaxStackSize, quant));
            }

            if (!byplayer.InventoryManager.TryGiveItemstack(givestack.Clone()))
            {
                world.SpawnItemEntity(givestack.Clone(), blockSel.HitPosition + (blockSel.HitPosition.Normalize() * .5) + blockSel.Position.ToVec3d(), blockSel.HitPosition.Normalize() * .05);
            }
            storedtypes[stype] -= givestack.StackSize;
            return true;
        }

        public bool AddResource(ItemSlot islot, int quant)
        {
            string btype = "";
            string rtype = "";
            string lstype = "";

            if (storedtypes["stone"] + storedtypes["gravel"] + storedtypes["sand"] + quant > maxStorable)
            {
                quant = maxStorable - (storedtypes["stone"] + storedtypes["gravel"] + storedtypes["sand"]);
                if (quant == 0)
                { return false; }
            }
            if (islot.Itemstack.Item == null)
            {
                rtype = islot.Itemstack.Block.FirstCodePart(1);
                btype = islot.Itemstack.Block.FirstCodePart(0);
                lstype = islot.Itemstack.Block.Code.Path;
            }
            else
            {
                rtype = islot.Itemstack.Item.FirstCodePart(1);
                btype = islot.Itemstack.Item.FirstCodePart(0);
                lstype = islot.Itemstack.Item.Code.Path;
            }

            if (storedType == "" && allowedTypes.Any(rtype.Contains))
            {
                storedType = rtype;
            }
            if (storedType == rtype)
            {
                if (islot.Itemstack.StackSize - quant < 0)
                {
                    quant = islot.Itemstack.StackSize;
                }
                switch (btype)
                {
                    case "sand":
                        {
                            storedtypes["sand"] += quant;
                            break;
                        }
                    case "gravel":
                        {
                            storedtypes["gravel"] += quant;
                            break;
                        }
                    case "stone":
                        {
                            storedtypes["stone"] += quant;
                            break;
                        }
                    default:
                        return false;
                }
                islot.TakeOut(quant);
                lastAdded = lstype;
                return true;
            }
            return false;
        }

        public bool AddAll(IPlayer byPlayer)
        {
            // will attempt to add all of a set item type from the players inventory.
            bool psound = false;
            foreach (ItemSlot isl in byPlayer.InventoryManager.GetHotbarInventory())
            {
                if (isl.Itemstack != null && isl.Itemstack.Collectible.Code.Path == lastAdded)
                {
                    if (AddResource(isl, isl.StackSize))
                    {
                        psound = true;
                    }
                }
            }
            foreach (KeyValuePair<string, IInventory> inv in byPlayer.InventoryManager.Inventories)
            {
                if (!inv.Key.Contains("creative"))
                {
                    foreach (ItemSlot isl in inv.Value)
                    {
                        if (isl.Itemstack != null && isl.Itemstack.Collectible.Code.Path == lastAdded)
                        {
                            if (AddResource(isl, isl.StackSize))
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
            if (storageLock == StorageLocksEnum.Stone)
            {
                return false;
            }
            else if (storageLock == StorageLocksEnum.Gravel)
            {
                if (storedtypes["stone"] > 1)
                {
                    storedtypes["stone"] -= 2;
                    storedtypes["gravel"] += 1;
                    return true;

                }
            }
            else if (storageLock == StorageLocksEnum.Sand)
            {
                if (storedtypes["gravel"] > 0)
                {
                    storedtypes["gravel"] -= 1;
                    storedtypes["sand"] += 1;
                    return true;

                }
                else if (storedtypes["stone"] > 0)
                {
                    storedtypes["stone"] -= 1;
                    storedtypes["gravel"] += 1;
                    return true;

                }
            }
            else
            {
                if (storedtypes["gravel"] > 0)
                {
                    storedtypes["gravel"] -= 1;
                    storedtypes["sand"] += 1;
                    return true;

                }
                else if (storedtypes["stone"] > 1)
                {
                    storedtypes["stone"] -= 2;
                    storedtypes["gravel"] += 1;
                    return true;
                }
                else
                { return false; }
            }
            return true;
        }

        public bool Drench(IWorldAccessor world, BlockSelection blockSel)
        {
            Block dropblock = world.GetBlock(new AssetLocation("game", "muddygravel"));
            if (storedtypes["gravel"] > 0)
            {
                ItemStack dropStack = new ItemStack(dropblock, Math.Min(dropblock.MaxStackSize / 4, storedtypes["gravel"]));//Deviding max stack drop by four because not doing so created a mess.
                world.SpawnItemEntity(dropStack.Clone(), blockSel.Position.ToVec3d() + blockSel.HitPosition, (blockSel.Face.Normalf.ToVec3d() * .05) + new Vec3d(0, .08, 0));
                storedtypes["gravel"] -= Math.Min(dropblock.MaxStackSize / 4, storedtypes["gravel"]);
                return true;
            }
            return false;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            storedType = tree.GetString("storedType", "");
            storedtypes["sand"] = tree.GetInt("sand", 0);
            storedtypes["gravel"] = tree.GetInt("gravel", 0);
            storedtypes["stone"] = tree.GetInt("stone", 0);
            storageLock = (StorageLocksEnum)tree.GetInt("storageLock", 0);
            maxStorable = tree.GetInt("maxStorable");

            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetString("storedType", storedType);
            tree.SetInt("sand", storedtypes["sand"]);
            tree.SetInt("gravel", storedtypes["gravel"]);
            tree.SetInt("stone", storedtypes["stone"]);
            tree.SetInt("storageLock", (int)storageLock);
            tree.SetInt("maxStorable", maxStorable);
            base.ToTreeAttributes(tree);
        }
    }
}