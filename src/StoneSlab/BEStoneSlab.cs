using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace StoneQuarry
{

    public class BEStoneSlab : BlockEntity, ITexPositionSource
    {
        public StoneSlabInventory Inventory { get; private set; }

        public BaseAllowedCodes AllowedCodes => (Block as BlockStoneSlab).AllowedCodes;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Inventory = new StoneSlabInventory(api, Pos, AllowedCodes);
        }

        public Block CurrentRock => Inventory.MostQuantityRock;

        public ICoreClientAPI capi => Api as ICoreClientAPI;

        public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (textureCode == "stone")
                {
                    ITexPositionSource tex = capi.Tesselator.GetTexSource(CurrentRock);
                    string otherCode = CurrentRock.Textures.FirstOrDefault().Key;
                    return tex[otherCode];
                }

                return capi.Tesselator.GetTexSource(Block)[textureCode];
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            Block rockBlock = Inventory.MostQuantityRock;
            if (rockBlock == null)
            {
                return base.OnTesselation(mesher, tessThreadTesselator);
            }

            string path = Block.Shape.Base.Domain + ":shapes/" + Block.Shape.Base.Path + ".json";
            Shape blockShape = Api.Assets.Get<Shape>(new AssetLocation(path));
            Vec3f rotation = new Vec3f(0, Block.Shape.rotateY, 0);
            tessThreadTesselator.TesselateShape(nameof(BEStoneSlab), blockShape, out MeshData mesh, this, rotation);

            mesher.AddMeshData(mesh);

            return true;
        }

        public void ContentToAttributes(ITreeAttribute tree)
        {
            Inventory.ToTreeAttributes(tree);
        }

        public void ContentFromAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            Inventory = new StoneSlabInventory(world.Api, Pos, AllowedCodes);
            Inventory.FromTreeAttributes(tree);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ContentToAttributes(tree);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            ContentFromAttributes(tree, worldAccessForResolve);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            string langKey = Core.ModId + ":info-stoneslab-heldinfo(count={0},stone={1})";

            for (int i = 0; i < Inventory.Count; i++)
            {
                var slot = Inventory[i];

                if (!slot.Empty)
                {
                    string rockName = Lang.Get(slot.Itemstack.Collectible.Code.ToString());
                    int quantity = slot.Itemstack.StackSize;
                    string text = Lang.Get(langKey, quantity, rockName);

                    if (i == Inventory.CurrentSlotId)
                    {
                        text = "(+) " + text;
                    }

                    dsc.AppendLine(text);
                }
            }
        }

        public ItemStack GetSelfDrop()
        {
            if (Inventory.Empty)
            {
                return null;
            }

            ItemStack stack = new ItemStack(Block);
            ContentToAttributes(stack.Attributes);
            return stack;
        }
    }
}
