using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace StoneQuarry
{
    public class BEStoneSlab : BlockEntity
    {
        private SimpleParticleProperties? _interactParticles;
        private StoneSlabMeshCache? _meshCache;

        public StoneSlabRenderPreset? RenderPreset { get; private set; }
        public StoneSlabInventory? Inventory { get; private set; }
        public SimpleParticleProperties InteractParticles
        {
            get
            {
                _interactParticles ??= new()
                {
                    MinPos = new Vec3d(),
                    AddPos = new Vec3d(.5, .5, .5),
                    MinQuantity = 5,
                    AddQuantity = 20,
                    GravityEffect = .9f,
                    WithTerrainCollision = true,
                    ParticleModel = EnumParticleModel.Quad,
                    LifeLength = 0.5f,
                    MinVelocity = new Vec3f(-0.4f, -0.4f, -0.4f),
                    AddVelocity = new Vec3f(0.8f, 1.2f, 0.8f),
                    MinSize = 0.1f,
                    MaxSize = 0.4f,
                    DieOnRainHeightmap = false
                };

                if (Inventory?.CurrentRock != null)
                {
                    Block rock = Api.World.GetBlock(Inventory.CurrentRock);
                    if (rock != null)
                    {
                        _interactParticles.ColorByBlock = rock;
                    }
                    else
                    {
                        _interactParticles.ColorByBlock = Block;
                    }
                }

                return _interactParticles;
            }
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Inventory ??= new StoneSlabInventory(api, Pos, 0);
            if (api is ICoreClientAPI)
            {
                _meshCache = api.ModLoader.GetModSystem<StoneSlabMeshCache>();
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            MeshData? mesh = _meshCache?.GetMesh(this, tessThreadTesselator);

            if (mesh != null)
            {
                mesher.AddMeshData(mesh);
                return true;
            }

            return base.OnTesselation(mesher, tessThreadTesselator);
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

            for (int i = 0; i < Inventory?.Count; i++)
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

        public void ContentToAttributes(ITreeAttribute tree)
        {
            Inventory?.ToTreeAttributes(tree);
            RenderPreset?.ToAttributes(tree);
        }

        public void ContentFromAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            Inventory = new StoneSlabInventory(world.Api, Pos, tree.GetInt("qslots"));
            Inventory.FromTreeAttributes(tree);

            RenderPreset = StoneSlabRenderPreset.FromAttributes(tree, world);
            RenderPreset ??= new StoneSlabRenderPreset(Inventory, Block);
        }

        public ItemStack? GetSelfDrop()
        {
            if (Inventory == null || Inventory.Empty)
            {
                return null;
            }

            Block block = Api.World.GetBlock(Block.CodeWithVariant("side", "north"));
            ItemStack stack = new(block);
            ContentToAttributes(stack.Attributes);

            return stack;
        }
    }
}
