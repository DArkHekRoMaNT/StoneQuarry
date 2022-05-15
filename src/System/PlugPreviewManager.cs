using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace StoneQuarry
{
    public class PlugPreviewManager
    {
        public const int BEPacketId = 51235;
        public const int HighlightSlotId = 1312;

        private readonly ICoreClientAPI api;
        private readonly List<BlockPos> plugNetworkBlocks;
        private readonly List<BlockPos> highlightBlocks;

        public PlugPreviewManager(ICoreClientAPI api)
        {
            this.api = api;
            plugNetworkBlocks = new List<BlockPos>();
            highlightBlocks = new List<BlockPos>();
        }

        public void TogglePreview(BlockPos plugPos)
        {
            if (plugNetworkBlocks.Contains(plugPos))
            {
                DisablePreview();
            }
            else
            {
                EnablePreview(plugPos);
            }
        }

        public void DisablePreview(BlockPos plugPos)
        {
            if (plugNetworkBlocks.Contains(plugPos))
            {
                plugNetworkBlocks.Clear();
                highlightBlocks.Clear();
            }

            UpdatePreview();
        }

        private void EnablePreview(BlockPos plugPos)
        {
            DisablePreview();

            if (api.World.BlockAccessor.GetBlockEntity(plugPos) is BEPlugAndFeather be)
            {
                if (be.IsNetworkPart)
                {
                    var blocks = be.GetAllBlocksInside();
                    if (blocks != null)
                    {
                        plugNetworkBlocks.AddRange(be.Points);
                        highlightBlocks.AddRange(blocks);
                        UpdatePreview();
                    }
                }
            }
        }

        private void DisablePreview()
        {
            if (plugNetworkBlocks.Count > 0)
            {
                plugNetworkBlocks.Clear();
                highlightBlocks.Clear();
            }

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            api.World.HighlightBlocks(api.World.Player, HighlightSlotId, highlightBlocks);
        }
    }
}
