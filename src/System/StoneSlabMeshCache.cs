using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    public class StoneSlabMeshCache : ModSystem, ITexPositionSource
    {
        private ICoreClientAPI api;
        private Dictionary<int, string> _cacheKeysByItemStackId = new Dictionary<int, string>();

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.api = api;
        }

        public Size2i AtlasSize => api.BlockTextureAtlas.Size;

        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (textureCode.StartsWith("stone") && currPreset != null)
                {
                    string stoneNum = textureCode.Substring(5);
                    if (int.TryParse(stoneNum, out int num))
                    {
                        if (0 <= num && num < currPreset.Blocks.Length)
                        {
                            Block block = currPreset.Blocks[num];
                            if (block == null)
                            {
                                return api.Tesselator.GetTexSource(currBlock)["filler"];
                            }

                            ITexPositionSource tex = api.Tesselator.GetTexSource(block);
                            string otherCode = block.Textures.FirstOrDefault().Key;
                            return tex[otherCode];
                        }
                    }
                    Core.ModLogger.Warning("Missing texture path for stone slab mesh texture code {0}, seems like a missing texture definition or invalid block.", textureCode);
                    return api.BlockTextureAtlas.UnknownTexturePosition;
                }

                return api.Tesselator.GetTexSource(currBlock)[textureCode];
            }
        }

        private StoneSlabPreset currPreset;
        private Block currBlock;

        public MeshRef GetInventoryMeshRef(ItemStack itemstack, BlockStoneSlab block)
        {
            string key = itemstack.TempAttributes.GetString("cachedMeshKey", null);
            if (key == null)
            {
                currPreset = StoneSlabPreset.FromAttributes(itemstack.Attributes, api.World, block);
                currBlock = block;
                key = currBlock.Code + "-invmesh-" + currPreset;
                itemstack.TempAttributes.SetString("cachedMeshKey", key);
            }

            return ObjectCacheUtil.GetOrCreate(api, key, () =>
            {
                Vec3f rotation = new Vec3f(0, currBlock.Shape.rotateY, 0);
                api.Tesselator.TesselateShape(
                    nameof(StoneSlabMeshCache), GetShape(), out MeshData mesh, this, rotation);
                return api.Render.UploadMesh(mesh);
            });
        }

        public MeshData GetMesh(BEStoneSlab beStoneSlab, ITesselatorAPI tessThreadTesselator)
        {
            currPreset = beStoneSlab.RenderPreset;
            currBlock = beStoneSlab.Block;

            string key = currBlock.Code + "-blockmesh-" + currPreset;
            return ObjectCacheUtil.GetOrCreate(api, key, () =>
            {
                Vec3f rotation = new Vec3f(0, currBlock.Shape.rotateY, 0);
                tessThreadTesselator.TesselateShape(
                    nameof(StoneSlabMeshCache), GetShape(), out MeshData mesh, this, rotation);
                return mesh;
            });
        }

        private Shape GetShape()
        {
            string path = currBlock.Shape.Base.Domain + ":shapes/" + currBlock.Shape.Base.Path + ".json";
            return api.Assets.Get<Shape>(new AssetLocation(path));
        }
    }
}
