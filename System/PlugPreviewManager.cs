using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace StoneQuarry
{
    public class PlugPreviewManager
    {
        private readonly ICoreClientAPI _api;
        private readonly List<BlockPos> _plugNetworkBlocks;
        private readonly List<BlockPos> _highlightBlocks;

        public PlugPreviewManager(ICoreClientAPI api)
        {
            _api = api;
            _plugNetworkBlocks = new List<BlockPos>();
            _highlightBlocks = new List<BlockPos>();
        }

        public void TogglePreview(BlockPos plugPos)
        {
            if (_plugNetworkBlocks.Contains(plugPos))
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
            if (_plugNetworkBlocks.Contains(plugPos))
            {
                _plugNetworkBlocks.Clear();
                _highlightBlocks.Clear();
            }

            UpdatePreview();
        }

        public void EnablePreview(BlockPos plugPos)
        {
            DisablePreview();

            if (_api.World.BlockAccessor.GetBlockEntity(plugPos) is BEPlugAndFeather be)
            {
                if (be.IsNetworkPart)
                {
                    var blocks = be.GetAllBlocksInside();
                    if (blocks != null)
                    {
                        _plugNetworkBlocks.AddRange(be.Points);
                        _highlightBlocks.AddRange(blocks);
                        UpdatePreview();
                    }
                }
            }
        }

        private void DisablePreview()
        {
            if (_plugNetworkBlocks.Count > 0)
            {
                _plugNetworkBlocks.Clear();
                _highlightBlocks.Clear();
            }

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            _api.World.HighlightBlocks(_api.World.Player, Constants.PreviewHighlightSlotId, _highlightBlocks);
        }
    }
}
