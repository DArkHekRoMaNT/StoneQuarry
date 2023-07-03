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
        private readonly Dictionary<int, string> _cacheKeysByItemStackId = new();
        private ICoreClientAPI _capi = null!;

        private StoneSlabRenderPreset? _currPreset;
        private Block? _currBlock;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            _capi = api;
        }

        public Size2i AtlasSize => _capi.BlockTextureAtlas.Size;

        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (textureCode.StartsWith("stone") && _currPreset != null)
                {
                    string stoneNum = textureCode.Substring(5);
                    if (int.TryParse(stoneNum, out int num))
                    {
                        if (0 <= num && num < _currPreset.Blocks.Length)
                        {
                            Block? block = _currPreset.Blocks[num];
                            if (block == null)
                            {
                                return _capi.Tesselator.GetTextureSource(_currBlock)["filler"];
                            }

                            ITexPositionSource tex = _capi.Tesselator.GetTextureSource(block);
                            string otherCode = block.Textures.FirstOrDefault().Key;
                            return tex[otherCode];
                        }
                    }
                    Mod.Logger.Warning("Missing texture path for stone slab mesh texture code {0}, seems like a missing texture definition or invalid block.", textureCode);
                    return _capi.BlockTextureAtlas.UnknownTexturePosition;
                }

                return _capi.Tesselator.GetTextureSource(_currBlock)[textureCode];
            }
        }

        public MeshRef GetInventoryMeshRef(ItemStack itemstack, BlockStoneSlab block)
        {
            string key = itemstack.TempAttributes.GetString("cachedMeshKey", null);

            if (key == null)
            {
                _currPreset = StoneSlabRenderPreset.FromAttributes(itemstack.Attributes, _capi.World, block);
                _currBlock = block;
                key = $"{Mod.Info.ModID}-{_currBlock.Code}-invmesh-{_currPreset}";
                itemstack.TempAttributes.SetString("cachedMeshKey", key);
            }

            return ObjectCacheUtil.GetOrCreate(_capi, key, () =>
            {
                var rotation = new Vec3f(0, _currBlock!.Shape.rotateY, 0);
                Shape shape = GetBaseShape(_currBlock.Shape);
                _capi.Tesselator.TesselateShape(nameof(StoneSlabMeshCache), shape,
                    out MeshData mesh, this, rotation);
                return _capi.Render.UploadMesh(mesh);
            });
        }

        public MeshData GetMesh(BEStoneSlab be, ITesselatorAPI tessThreadTesselator)
        {
            _currPreset = be.RenderPreset;
            _currBlock = be.Block;

            string key = $"{Mod.Info.ModID}-{_currBlock.Code}-blockmesh-{_currPreset}";
            return ObjectCacheUtil.GetOrCreate(_capi, key, () =>
            {
                var rotation = new Vec3f(0, _currBlock.Shape.rotateY, 0);
                Shape shape = GetBaseShape(_currBlock.Shape);
                tessThreadTesselator.TesselateShape(nameof(StoneSlabMeshCache), shape,
                    out MeshData mesh, this, rotation);
                return mesh;
            });
        }

        private Shape GetBaseShape(CompositeShape shape)
        {
            string path = $"{shape.Base.Domain}:shapes/{shape.Base.Path}.json";
            return _capi.Assets.Get<Shape>(new AssetLocation(path));
        }
    }
}
