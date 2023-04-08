using CommonLib.Config;
using CommonLib.Extensions;
using CommonLib.Utils;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace StoneQuarry
{
    public class BEPlugAndFeather : BlockEntity
    {
        private SimpleParticleProperties? _breakParticles;
        private int _currentStageWork = 0;
        private ILogger? _modLogger;

        private SimpleParticleProperties BreakParticles
        {
            get => _breakParticles ??= new()
            {
                MinQuantity = 5,
                AddQuantity = 2,
                MinSize = 1f,
                MaxSize = 3f,
                MinPos = new Vec3d(),
                AddPos = new Vec3d(1, 1, 1),
                MinVelocity = new Vec3f(0, -0.1f, 0),
                AddVelocity = new Vec3f(.2f, -0.2f, .2f),
                ColorByBlock = Block,
                LifeLength = .5f,
                ParticleModel = EnumParticleModel.Quad
            };
        }
        private int MaxWorkPerStage
        {
            get
            {
                int workNeeded = 5 + (Points.Count * 2);
                return (int)(workNeeded * Config.PlugWorkModifier);
            }
        }
        private ILogger ModLogger => _modLogger ?? Api.Logger;
        private Config Config { get; set; } = null!;

        /// <summary> All points for own plug network (including the current plug). Empty if the network does not exist. </summary>
        public List<BlockPos> Points { get; } = new();
        public bool IsNetworkPart => Points.Count > 0;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            var configs = api.ModLoader.GetModSystem<ConfigManager>();
            Config = configs.GetConfig<Config>();
            _modLogger = api.ModLoader.GetModSystem<Core>().Mod.Logger;
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);

            if (packetid == Constants.PreviewPacketId)
            {
                Api.ModLoader.GetModSystem<Core>()
                    .PlugPreviewManager?
                    .DisablePreview(Pos);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("work", _currentStageWork);

            if (Points.Count != 0)
            {
                tree.SetInt("pointcount", Points.Count);
                for (int i = 0; i < Points.Count; i++)
                {
                    tree.SetBlockPos("point" + i, Points[i]);
                }
            }

            base.ToTreeAttributes(tree);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            _currentStageWork = tree.GetInt("work", _currentStageWork);

            int slaveCount = tree.GetInt("pointcount", 0);
            if (slaveCount != 0)
            {
                for (int i = 0; i < slaveCount; i++)
                {
                    Points.Add(tree.GetBlockPos("point" + i));
                }
            }

            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public bool IsDone(IWorldAccessor world)
        {
            foreach (var point in Points)
            {
                if (world.BlockAccessor.GetBlock(point) is BlockPlugAndFeather pointBlock)
                {
                    if (pointBlock.Stage != BlockPlugAndFeather.MaxStage)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TryHitPlug(ItemStack byStack)
        {
            if (byStack.ItemAttributes != null)
            {
                bool isPlugImpactTool = byStack.ItemAttributes.KeyExists("plugimpact");
                if (IsNetworkPart && isPlugImpactTool)
                {
                    _currentStageWork += byStack.ItemAttributes["plugimpact"].AsInt();
                    if (_currentStageWork > MaxWorkPerStage)
                    {
                        _currentStageWork -= MaxWorkPerStage;
                        return true;
                    }
                }
            }

            return false;
        }

        public void BreakAll(IWorldAccessor world, IServerPlayer byPlayer)
        {
            IDictionary<AssetLocation, int> quantitiesByRock = GetRocksInside(world, byPlayer);
            List<ItemStack> contentStacks = new();

            int rockQuantity = 0;
            foreach (var rock in quantitiesByRock)
            {
                rockQuantity += rock.Value;
                contentStacks.Add(new ItemStack(world.GetBlock(rock.Key), rock.Value));
            }

            string? slabSize = rockQuantity switch
            {
                >= 196 => "giant",
                >= 128 => "huge",
                >= 64 => "large",
                >= 32 => "medium",
                >= 1 => "small",
                _ => null
            };

            if (slabSize != null)
            {
                string dropItemString = "stoneslab-" + slabSize + "-north";

                AssetLocation dropItemLoc = new(Core.ModId, dropItemString);
                if (world.GetBlock(dropItemLoc) is BlockStoneSlab dropItem)
                {
                    ItemStack dropItemStack = new(dropItem, 1);

                    var inv = StoneSlabInventory.StacksToTreeAttributes(contentStacks, dropItemStack.Attributes, Api);
                    var preset = new StoneSlabRenderPreset(inv, dropItem);
                    preset.ToAttributes(dropItemStack.Attributes);

                    Cuboidi? insideCube = GetInsideCube();
                    if (insideCube != null)
                    {
                        world.SpawnItemEntity(dropItemStack, insideCube.Center.ToVec3d().Add(.5, .5, .5));
                    }
                }
                else
                {
                    ModLogger.Warning("Unknown drop item " + dropItemLoc);
                }
            }

            world.BlockAccessor.BreakBlock(Pos, byPlayer);

            Dictionary<AssetLocation, int> GetRocksInside(IWorldAccessor world, IServerPlayer byPlayer)
            {
                var quantitiesByRock = new Dictionary<AssetLocation, int>();

                IRockManager manager = Api.ModLoader.GetModSystem<RockManager>();

                foreach (var pos in GetAllBlocksInside())
                {
                    Block block = world.BlockAccessor.GetBlock(pos);
                    if (manager.IsSuitableRock(block.Code))
                    {
                        if (world.IsPlayerCanBreakBlock(pos, byPlayer))
                        {
                            BreakParticles.ColorByBlock = world.BlockAccessor.GetBlock(pos);
                            BreakParticles.MinPos = pos.ToVec3d();

                            world.BlockAccessor.SetBlock(0, pos);

                            world.SpawnParticles(BreakParticles, byPlayer);

                            if (quantitiesByRock.ContainsKey(block.Code))
                            {
                                quantitiesByRock[block.Code] += 1;
                            }
                            else
                            {
                                quantitiesByRock.Add(block.Code, 1);
                            }
                        }
                    }
                }
                return quantitiesByRock;
            }
        }

        public List<BlockPos> GetAllBlocksInside()
        {
            var blocks = new List<BlockPos>();

            Cuboidi? cube = GetInsideCube();

            if (cube != null)
            {
                for (int x = cube.MinX; x <= cube.MaxX; x++)
                {
                    for (int y = cube.MinY; y <= cube.MaxY; y++)
                    {
                        for (int z = cube.MinZ; z <= cube.MaxZ; z++)
                        {
                            blocks.Add(new BlockPos(x, y, z));
                        }
                    }
                }
            }

            return blocks;
        }

        private Cuboidi? GetInsideCube()
        {
            if (IsNetworkPart)
            {
                var cube = new Cuboidi(Points[0], Points[1]);
                cube.GrowBy(-1, -1, -1);

                foreach (var pos in Points)
                {
                    if (Api.World.BlockAccessor.GetBlock(pos) is BlockPlugAndFeather pb)
                    {
                        BlockPos innerPos = pos.Copy();

                        if (pb.Orientation == "down")
                        {
                            innerPos.Add(BlockFacing.DOWN.Normali);
                        }

                        if (pb.Orientation == "up")
                        {
                            innerPos.Add(BlockFacing.UP.Normali);
                        }

                        if (pb.Orientation == "horizontal")
                        {
                            innerPos.Add(BlockFacing.FromCode(pb.Direction).Normali);
                        }

                        if (innerPos.X < cube.X1) cube.X1 = innerPos.X;
                        if (innerPos.Y < cube.Y1) cube.Y1 = innerPos.Y;
                        if (innerPos.Z < cube.Z1) cube.Z1 = innerPos.Z;

                        if (innerPos.X > cube.X2) cube.X2 = innerPos.X;
                        if (innerPos.Y > cube.Y2) cube.Y2 = innerPos.Y;
                        if (innerPos.Z > cube.Z2) cube.Z2 = innerPos.Z;
                    }
                }

                return cube;
            }

            return null;
        }
    }
}
