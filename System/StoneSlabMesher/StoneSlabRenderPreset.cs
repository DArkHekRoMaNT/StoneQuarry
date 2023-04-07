using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace StoneQuarry
{
    public class StoneSlabRenderPreset
    {
        public Block?[] Blocks { get; private set; }

        private StoneSlabRenderPreset(Block[] blocks)
        {
            Blocks = blocks;
        }

        private StoneSlabRenderPreset(Block block)
        {
            string size = block.Variant["size"];
            Blocks = new Block[SizeToBlockCount(size)];
        }

        public StoneSlabRenderPreset(StoneSlabInventory inventory, Block block) : this(block)
        {
            Update(inventory, block);
        }

        public void Update(StoneSlabInventory inv, Block block)
        {
            int all = 0;
            var quantities = new List<double>();
            var storedBlocks = new List<Block>();

            int maxBlockCount = SizeToBlockCount(block.Variant["size"]);
            Blocks = new Block[maxBlockCount];

            foreach (var slot in inv)
            {
                if (slot.Empty) continue;

                all += slot.StackSize;
                quantities.Add(slot.StackSize);
                storedBlocks.Add(slot.Itemstack.Block);
            }

            if (quantities.Count == 0)
            {
                return;
            }

            double partSize = (double)all / maxBlockCount;
            for (int i = 0; i < maxBlockCount; i++)
            {
                Blocks[i] = null;

                for (int k = 0; k < storedBlocks.Count; k++)
                {
                    if (quantities[k] >= 0)
                    {
                        Blocks[i] = storedBlocks[k];
                        quantities[k] -= partSize;
                        break;
                    }
                }

                if (Blocks[i] == null)
                {
                    double max = quantities.Max();
                    int id = quantities.IndexOf(max);
                    Blocks[i] = storedBlocks[id];
                    quantities[id] -= partSize;
                }
            }
        }

        private static int SizeToBlockCount(string size)
        {
            return size switch
            {
                "giant" => 12,
                "huge" => 8,
                "large" => 4,
                "medium" => 2,
                "small" => 1,
                _ => throw new NotImplementedException($"Unknown size {size}"),
            };
        }

        public void ToAttributes(ITreeAttribute tree)
        {
            ITreeAttribute subtree = tree.GetOrAddTreeAttribute("preset");

            subtree.SetInt("size", Blocks.Length);
            for (int i = 0; i < Blocks.Length; i++)
            {
                subtree.SetString(i + "", Blocks[i]?.Code + "");
            }
        }

        public override string ToString()
        {
            return string.Join("-", Blocks.Select((b) => b?.Code));
        }

        public static StoneSlabRenderPreset? FromAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve, Block? block = null)
        {
            ITreeAttribute subtree = tree.GetTreeAttribute("preset");

            if (subtree != null)
            {
                int size = subtree.GetInt("size", -1);
                if (size == -1) // GetInt cause problem with creativeinventoryStacks
                {
                    size = (int)subtree.GetLong("size", 0);
                }

                Block[] blocks = new Block[size];
                for (int i = 0; i < size; i++)
                {
                    string code = subtree.GetString(i + "", null);
                    if (worldForResolve != null)
                    {
                        blocks[i] = worldForResolve.GetBlock(new AssetLocation(code));
                    }
                }
                return new StoneSlabRenderPreset(blocks);
            }
            else if (block != null)
            {
                return new StoneSlabRenderPreset(block);
            }

            return null;
        }
    }
}
