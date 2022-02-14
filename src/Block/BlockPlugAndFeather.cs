using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    public class BlockPlugAndFeather : Block
    {
        WorldInteraction[] interactions;
        readonly SimpleParticleProperties breakParticles = new SimpleParticleProperties(32, 32, ColorUtil.ColorFromRgba(122, 76, 23, 50), new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f());
        public AssetLocation crackSound = new AssetLocation("game", "sounds/block/heavyice");
        public AssetLocation hammerSound = new AssetLocation("game", "sounds/block/meteoriciron-hit-pickaxe");

        public int MaxSearchRange => Attributes["searchrange"].AsInt(6);
        public const int WORK_PER_BLOCK = 2; // the amount of work needed per block
        public const int BASE_WORK = 5; // the base amount of work needed

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            var quarryImpact = new List<ItemStack>();
            var quarryStarter = new List<ItemStack>();
            foreach (var colObj in api.World.Collectibles)
            {
                if (colObj.Attributes != null)
                {
                    if (colObj.Attributes["quarryimpact"].Exists)
                    {
                        quarryImpact.Add(new ItemStack(colObj));
                    }
                    if (colObj.Attributes["quarrystarter"].Exists)
                    {
                        quarryStarter.Add(new ItemStack(colObj));
                    }
                }
            }

            interactions = new WorldInteraction[] {
                new WorldInteraction(){
                    ActionLangCode = Code.Domain + ":wi-plugandfeather-quarrystarter",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = quarryStarter.ToArray()
                },
                new WorldInteraction(){
                    ActionLangCode = Code.Domain + ":wi-plugandfeather-quarryimpact",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = quarryImpact.ToArray()
                }
            };
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            string orientation;
            string oDirections;
            if (blockSel.Face.Index == 5)
            {
                orientation = "up";
                oDirections = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
            }
            else if (blockSel.Face.Index == 4)
            {
                orientation = "down";
                oDirections = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
            }
            else
            {
                orientation = "horizontal";
                oDirections = blockSel.Face.Opposite.ToString();
            }

            AssetLocation blockSwapLocation = new AssetLocation(Code.Domain, CodeWithoutParts(2) + "-" + orientation + "-" + oDirections);
            Block blockSwap = world.GetBlock(blockSwapLocation);
            world.BlockAccessor.SetBlock(blockSwap.Id, blockSel.Position);

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEPlugAndFeather;
            be.orientation = orientation;
            be.facing = oDirections;

            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var be = (BEPlugAndFeather)world.BlockAccessor.GetBlockEntity(pos);
            if (be == null || be.master == null)
            {
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            }
            else
            {
                if (world.BlockAccessor.GetBlockEntity(be.master.AsBlockPos) is BEPlugAndFeather masterBE)
                {
                    var dropStack = GetDrops(world, pos, byPlayer, dropQuantityMultiplier)[0];

                    foreach (Vec3i slavePos in masterBE.slaves)
                    {
                        dropStack.StackSize = api.World.Rand.NextDouble() <= Core.Config.BreakPlugChance ? 0 : 1;
                        world.BlockAccessor.SetBlock(0, slavePos.AsBlockPos);
                        world.BlockAccessor.MarkBlockDirty(slavePos.AsBlockPos);
                        world.SpawnItemEntity(dropStack.Clone(), new Vec3d()
                        {
                            X = slavePos.X + .5f,
                            Y = slavePos.Y + .5f,
                            Z = slavePos.Z + .5f
                        });
                    }

                    dropStack.StackSize = api.World.Rand.NextDouble() <= Core.Config.BreakPlugChance ? 0 : 1;
                    world.BlockAccessor.SetBlock(0, be.master.AsBlockPos);
                    world.BlockAccessor.MarkBlockDirty(be.master.AsBlockPos);
                    world.SpawnItemEntity(dropStack.Clone(), new Vec3d()
                    {
                        X = masterBE.Pos.X + .5f,
                        Y = masterBE.Pos.Y + .5f,
                        Z = masterBE.Pos.Z + .5f
                    });
                }
                else
                {
                    base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                }
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var be = (BEPlugAndFeather)world.BlockAccessor.GetBlockEntity(blockSel.Position);
            var activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

            if (be == null || activeStack == null || activeStack.ItemAttributes == null)
            {
                return false;
            }

            if (be.master == null && (activeStack.ItemAttributes["quarrystarter"]?.AsBool() ?? false))
            {
                if (world.Side == EnumAppSide.Client)
                {
                    (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                }

                MakeNetwork(world, byPlayer, blockSel);
                int maxwork = WorkNeeded(be.slaveCount + 1);
                be.SetMaxWork(maxwork);

                if (be.master == null)
                {
                    return false;
                }

                breakParticles.MinPos = be.master.AsBlockPos.ToVec3d();
                breakParticles.AddPos = new Vec3d(1, 1, 1);
                breakParticles.ColorByBlock = world.BlockAccessor.GetBlock(be.master.AsBlockPos);
                breakParticles.MinVelocity = new Vec3f(.1f, .3f, .1f);
                breakParticles.AddVelocity = new Vec3f(0f, .3f, 0f);

                world.SpawnParticles(breakParticles, byPlayer);

                foreach (Vec3i slave in be.slaves)
                {
                    breakParticles.MinPos = slave.AsBlockPos.ToVec3d();
                    world.SpawnParticles(breakParticles, byPlayer);

                    if (world.BlockAccessor.GetBlockEntity(slave.AsBlockPos) is BEPlugAndFeather slaveBE)
                    {
                        slaveBE.SetMaxWork(maxwork);
                    }
                }
            }

            if (be.master != null && activeStack.ItemAttributes.KeyExists("quarryimpact"))
            {
                if (be.IncreaseWork(10) == true)
                {
                    if (be.state != be.maxState)
                    {
                        world.PlaySoundAt(crackSound, byPlayer, byPlayer, true, 32, .5f);

                        be.IncreaseState(1);
                        SwitchState(be.state, world, byPlayer, blockSel);
                        activeStack.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot);
                    }
                    if (CheckDone(be, world) == true)
                    {
                        BreakAll(be, world, byPlayer);
                    }
                }

                world.PlaySoundAt(hammerSound, byPlayer, byPlayer, true, 32, .5f);

                if (world.Side == EnumAppSide.Client)
                {
                    (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                }
            }

            return true;
        }

        /// <summary> Makes the network if a network is available to make </summary>
        public void MakeNetwork(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var be = (BEPlugAndFeather)world.BlockAccessor.GetBlockEntity(blockSel.Position);

            if (be != null && be.master == null)
            {
                List<Vec3i> slaves = FindSlaves(world, byPlayer, blockSel);
                if (slaves != null)
                {
                    be.master = blockSel.Position.ToVec3i();
                    Vec3i[] cube = FindCube(slaves);
                    int work = WorkNeeded(slaves.Count + 1);

                    foreach (Vec3i slave in slaves)
                    {
                        be.AddSlave(slave);
                        BEPlugAndFeather tempBE = world.BlockAccessor.GetBlockEntity(slave.AsBlockPos) as BEPlugAndFeather;
                        tempBE.master = blockSel.Position.ToVec3i();
                        tempBE.maxWork = work;
                    }
                }
            }
        }

        /// <summary> Finds the amount of work needed for each point on the network </summary>
        public int WorkNeeded(int blocks)
        {
            return BASE_WORK + (blocks * WORK_PER_BLOCK);
        }

        public void SwitchState(int pickState, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            string[] switchTerm = { "one", "two", "three" };

            if (pickState < switchTerm.Length)
            {
                Block block = world.BlockAccessor.GetBlock(blockSel.Position);
                AssetLocation newState = block.CodeWithPart(switchTerm[pickState], 2);

                (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEPlugAndFeather).state = pickState;
                world.BlockAccessor.ExchangeBlock(world.GetBlock(newState).Id, blockSel.Position);
            }
        }

        /// <returns> false if not done, true if done</returns>
        public bool CheckDone(BEPlugAndFeather be, IWorldAccessor world)
        {
            if (be == null || be.master == null)
            {
                return false;
            }

            var master = world.BlockAccessor.GetBlockEntity(be.master.AsBlockPos) as BEPlugAndFeather;
            if (master == null || master.state != master.maxState)
            {
                return false;
            }


            List<Vec3i> slaves = master.slaves;
            foreach (Vec3i slave in slaves)
            {
                var slaveBE = (BEPlugAndFeather)world.BlockAccessor.GetBlockEntity(slave.AsBlockPos);
                if (slaveBE == null || slaveBE.state != slaveBE.maxState)
                {
                    return false;
                }
            }

            return true;
        }
        public void BreakAll(BEPlugAndFeather be, IWorldAccessor world, IPlayer byPlayer)
        {
            BEPlugAndFeather master = world.BlockAccessor.GetBlockEntity(be.master.AsBlockPos) as BEPlugAndFeather;
            List<Vec3i> points = new List<Vec3i> { be.master };

            foreach (Vec3i slave in master.slaves)
            {
                points.Add(slave);
            }

            Vec3i[] cube = FindCube(points);
            List<BlockPos> blocks = FindBlocksPos(cube[0], cube[1]);
            //world.HighlightBlocks(byPlayer, 0, blocks);
            IDictionary<string, int> stones = FindStoneTypes(world, blocks);

            string rockpath = "";
            int rockamount = 0;

            foreach (KeyValuePair<string, int> k in stones)
            {
                if (k.Value > rockamount)
                {
                    rockpath = k.Key;
                    rockamount = k.Value;
                }
            }

            foreach (BlockPos point in blocks)
            {
                if (world.BlockAccessor.GetBlock(point).Code.Path == "rock-" + rockpath)
                {
                    breakParticles.ColorByBlock = world.BlockAccessor.GetBlock(point);
                    breakParticles.MinQuantity = 5;
                    breakParticles.AddQuantity = 2;
                    breakParticles.MinPos = point.ToVec3d();
                    breakParticles.AddPos = new Vec3d(1, 1, 1);
                    breakParticles.MinVelocity = new Vec3f(0, 3, 0);
                    breakParticles.AddVelocity = new Vec3f(.1f, .5f, .1f);
                    breakParticles.MinSize = 1f;
                    breakParticles.MaxSize = 3f;

                    world.BlockAccessor.SetBlock(0, point);

                    world.SpawnParticles(breakParticles, byPlayer);
                }
            }

            // This is where our system decide what to drop.
            string dropItemFillerString = GetDropType(rockamount);
            string sizeModFillerString = GetSizeFillerString(dropItemFillerString);
            string dropItemString = "stonestorage" + dropItemFillerString + "-" + rockpath + sizeModFillerString + "north";

            AssetLocation dropitemLoc = new AssetLocation(Code.Domain, dropItemString.ToLower());
            Block dropItem = world.GetBlock(dropitemLoc);
            if (dropItem != null)
            {
                ItemStack dropItemStack = new ItemStack(dropItem, 1);
                dropItemStack.Attributes.SetInt("stonestored", rockamount);
                world.SpawnItemEntity(dropItemStack, new Vec3d()
                {
                    X = ((cube[1].X - cube[0].X) / 2f) + cube[0].X,
                    Y = ((cube[1].Y - cube[0].Y) / 2f) + cube[0].Y,
                    Z = ((cube[1].Z - cube[0].Z) / 2f) + cube[0].Z
                });
            }
            else
            {
                api.Logger.Warning("[" + Code.Domain + "] Unknown drop item " + dropitemLoc);
            }
            world.BlockAccessor.BreakBlock(be.master.AsBlockPos, byPlayer);
        }

        /// <summary>
        /// looks at the blocks above and below this one it the direction it's facing and matches the first that has the orientation of up/down and the same facing direction.
        /// if you're looking at this to figure out what I did.... good luck? If you can optimize this let me know. I'm moving on to something else now.
        /// </summary>
        public Vec3i GetCounterpart(IWorldAccessor world, IPlayer byPlayer, BlockPos blockSel)
        {
            var be = (BEPlugAndFeather)world.BlockAccessor.GetBlockEntity(blockSel);
            if (be == null || be.facing == null || be.orientation == null)
            {
                return null;
            }

            Vec3i orientation = new Vec3i(); // orientation is +\-.
            Vec3i dir = new Vec3i(); // dir is a set vector positive or negative.
            string[] checkDir = { "", "" }; // the direction string to check against.

            Vec3i checkPos = new Vec3i();
            Vec3i startPos = blockSel.ToVec3i();

            if (be.orientation == "up")
            {
                dir = new Vec3i(0, 1, 0);
                if (be.facing == "north" || be.facing == "south")
                {
                    orientation = new Vec3i(0, 0, 1);
                    checkDir = new string[] { "north", "south" };
                }
                else if (be.facing == "east" || be.facing == "west")
                {
                    orientation = new Vec3i(1, 0, 0);
                    checkDir = new string[] { "east", "west" };
                }
            }
            else if (be.orientation == "down")
            {
                dir = new Vec3i(0, -1, 0);
                if (be.facing == "north" || be.facing == "south")
                {
                    orientation = new Vec3i(0, 0, 1);
                    checkDir = new string[] { "north", "south" };
                }
                else if (be.facing == "east" || be.facing == "west")
                {
                    orientation = new Vec3i(1, 0, 0);
                    checkDir = new string[] { "east", "west" };
                }
            }
            else if (be.orientation == "horizontal")
            {
                orientation = new Vec3i(0, 1, 0);
                if (be.facing == "north")
                {
                    dir = new Vec3i(0, 0, -1);
                    checkDir = new string[] { "north", "south" };
                }
                else if (be.facing == "east")
                {
                    dir = new Vec3i(1, 0, 0);
                    checkDir = new string[] { "east", "west" };
                }
                else if (be.facing == "south")
                {
                    dir = new Vec3i(0, 0, 1);
                    checkDir = new string[] { "north", "south" };
                }
                else if (be.facing == "west")
                {
                    dir = new Vec3i(-1, 0, 0);
                    checkDir = new string[] { "east", "west" };
                }
            }


            for (int x = 1; x <= MaxSearchRange; x++)
            {
                for (int y = 1; y <= MaxSearchRange; y++)
                {
                    checkPos = new Vec3i(startPos.X + (dir.X * x), startPos.Y + (dir.Y * x), startPos.Z + (dir.Z * x));

                    Vec3i rpos1 = new Vec3i(checkPos.X + (orientation.X * y), checkPos.Y + (orientation.Y * y), checkPos.Z + (orientation.Z * y));
                    Vec3i rpos2 = new Vec3i(checkPos.X - (orientation.X * y), checkPos.Y - (orientation.Y * y), checkPos.Z - (orientation.Z * y));

                    Block block1 = world.BlockAccessor.GetBlock(rpos1.AsBlockPos);
                    Block block2 = world.BlockAccessor.GetBlock(rpos2.AsBlockPos);
                    if (be.orientation == "horizontal")
                    {
                        if (block1.Code.Path.Contains("plugandfeather") && block1.FirstCodePart(1) == FirstCodePart(1) && block1.Code.Path.Contains("-down-") && (block1.Code.Path.Contains(checkDir[0]) || block1.Code.Path.Contains(checkDir[1])))
                        {
                            return rpos1;
                        }
                        if (block2.Code.Path.Contains("plugandfeather") && block2.FirstCodePart(1) == FirstCodePart(1) && block2.Code.Path.Contains("-up-") && (block2.Code.Path.Contains(checkDir[0]) || block2.Code.Path.Contains(checkDir[1])))
                        {
                            return rpos2;
                        }
                    }
                    else if (be.orientation == "up" || be.orientation == "down")
                    {
                        if (block1.Code.Path.Contains("plugandfeather-") && block1.Code.Path.Contains("-horizontal-") && block1.FirstCodePart(1) == this.FirstCodePart(1) && (block1.Code.Path.Contains(checkDir[0]) || block1.Code.Path.Contains(checkDir[1])))
                        {
                            return rpos1;
                        }
                        if (block2.Code.Path.Contains("plugandfeather-") && block2.Code.Path.Contains("-horizontal-") && block2.FirstCodePart(1) == this.FirstCodePart(1) && (block2.Code.Path.Contains(checkDir[0]) || block2.Code.Path.Contains(checkDir[1])))
                        {
                            return rpos2;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary> Finds blocks in a line around this block. Stops if theres a break in the line </summary>
        public List<Vec3i> GetNeighbours(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

            BEPlugAndFeather BE = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEPlugAndFeather;
            Vec3i checkDir = new Vec3i();
            Vec3i startPos = blockSel.Position.ToVec3i();

            List<Vec3i> returnBlocks = new List<Vec3i>();

            if (BE.facing == null)
            {
                return null;
            }

            if (BE.facing == "north" || BE.facing == "south")
            {
                checkDir = new Vec3i(1, 0, 0);
            }
            else if (BE.facing == "east" || BE.facing == "west")
            {
                checkDir = new Vec3i(0, 0, 1);
            }

            bool p1 = true;
            bool p2 = true;

            for (int i = 1; i <= MaxSearchRange; i++)
            {
                Vec3i checkPos1 = new Vec3i(startPos.X + (checkDir.X * i), startPos.Y + (checkDir.Y * i), startPos.Z + (checkDir.Z * i));
                Vec3i checkPos2 = new Vec3i(startPos.X - (checkDir.X * i), startPos.Y - (checkDir.Y * i), startPos.Z - (checkDir.Z * i));

                Block block1 = world.BlockAccessor.GetBlock(checkPos1.ToBlockPos());
                Block block2 = world.BlockAccessor.GetBlock(checkPos2.ToBlockPos());

                BEPlugAndFeather blocke1 = world.BlockAccessor.GetBlockEntity(checkPos1.ToBlockPos()) as BEPlugAndFeather;
                BEPlugAndFeather blocke2 = world.BlockAccessor.GetBlockEntity(checkPos2.ToBlockPos()) as BEPlugAndFeather;

                if (p1 == true)
                {
                    if (block1.Code.Path.Contains(FirstCodePart()) && block1.FirstCodePart(1) == this.FirstCodePart(1) && block1.Code.Path.Contains(FirstCodePart(3)) && block1.Code.Path.Contains(FirstCodePart(4)) && blocke1.master == null)
                    {
                        returnBlocks.Add(checkPos1);
                    }
                    else
                    {
                        p1 = false;
                    }
                }

                if (p2 == true)
                {
                    if (block2.Code.Path.Contains(FirstCodePart()) && block2.FirstCodePart(1) == this.FirstCodePart(1) && block2.Code.Path.Contains(FirstCodePart(3)) && block2.Code.Path.Contains(FirstCodePart(4)) && blocke2.master == null)
                    {
                        returnBlocks.Add(checkPos2);
                    }
                    else
                    {
                        p2 = false;
                    }
                }

                if (returnBlocks.Count == MaxSearchRange - 1)
                {
                    break;
                }

            }

            return returnBlocks;
        }

        /// <summary> Builds a series of points based on the surrounding blocks </summary>
        public List<Vec3i> FindSlaves(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            Vec3i counterpart = GetCounterpart(world, byPlayer, blockSel.Position);
            List<Vec3i> neighbours = GetNeighbours(world, byPlayer, blockSel);

            List<Vec3i> slaves = new List<Vec3i>();

            if (counterpart == null)
            {
                return null;
            }

            slaves.Add(counterpart);

            for (int i = 0; i < neighbours.Count; i++)
            {
                var nBlock = world.BlockAccessor.GetBlock(neighbours[i].AsBlockPos) as BlockPlugAndFeather;
                var nBE = world.BlockAccessor.GetBlockEntity(neighbours[i].AsBlockPos) as BEPlugAndFeather;

                Vec3i nCounterPos = nBlock.GetCounterpart(world, byPlayer, neighbours[i].AsBlockPos);
                BEPlugAndFeather nCounterBE = null;
                if (nCounterPos != null)
                {
                    nCounterBE = world.BlockAccessor.GetBlockEntity(nCounterPos.AsBlockPos) as BEPlugAndFeather;
                }

                if (nCounterPos != null && nCounterBE.master == null)
                {
                    slaves.Add(nCounterPos);
                    slaves.Add(neighbours[i]);
                }
            }

            return slaves;
        }

        public Vec3i[] FindCube(List<Vec3i> dataPoints)
        {
            if (dataPoints == null)
            {
                return null;
            }

            Vec3i minPos = dataPoints[0].Clone();
            Vec3i maxPos = dataPoints[0].Clone();

            foreach (Vec3i pos in dataPoints)
            {
                if (pos.X < minPos.X) minPos.X = pos.X;
                if (pos.Y < minPos.Y) minPos.Y = pos.Y;
                if (pos.Z < minPos.Z) minPos.Z = pos.Z;

                if (pos.X > maxPos.X) maxPos.X = pos.X;
                if (pos.Y > maxPos.Y) maxPos.Y = pos.Y;
                if (pos.Z > maxPos.Z) maxPos.Z = pos.Z;
            }

            return new Vec3i[] { minPos, maxPos };
        }

        /// <summary> Get a list of all blocks found in a cube defined by two points </summary>
        public List<BlockPos> FindBlocksPos(Vec3i minPos, Vec3i maxPos)
        {
            List<BlockPos> blocksPos = new List<BlockPos>();

            for (int x = minPos.X; x <= maxPos.X; x++)
            {
                for (int y = minPos.Y; y <= maxPos.Y; y++)
                {
                    for (int z = minPos.Z; z <= maxPos.Z; z++)
                    {
                        blocksPos.Add(new BlockPos(x, y, z));
                    }
                }
            }

            return blocksPos;
        }

        /// <summary> Checks all points givin and returns the types and quantaties of stone found. </summary>
        public IDictionary<string, int> FindStoneTypes(IWorldAccessor world, List<BlockPos> points)
        {
            IDictionary<string, int> types = new Dictionary<string, int>();
            foreach (var pos in points)
            {
                Block block = world.BlockAccessor.GetBlock(pos);
                if (block.FirstCodePart() == "rock")
                {
                    if (types.ContainsKey(block.FirstCodePart(1)))
                    {
                        types[block.FirstCodePart(1)] += 1;
                    }
                    else
                    {
                        types.Add(block.FirstCodePart(1), 1);
                    }
                }
            }
            return types;
        }


        public string GetSizeFillerString(string rocksize)
        {
            string filler = "-";

            if (rocksize == "Large")
            {
                filler = "-zero-";
            }
            else if (rocksize == "Huge")
            {
                filler = "-zero-zero-";
            }
            else if (rocksize == "Giant")
            {
                filler = "-zero-zero-";
            }

            return filler;
        }

        public string GetDropType(int quantity)
        {
            string dropType = null;
            if (quantity > 0 && quantity < 42)
            {
                dropType = "Small";
            }
            else if (quantity >= 42 && quantity < 84)
            {
                dropType = "Med";
            }
            else if (quantity >= 84 && quantity < 126)
            {
                dropType = "Large";
            }
            else if (quantity >= 126 && quantity < 168)
            {
                dropType = "Huge";
            }
            else if (quantity >= 168)
            {
                dropType = "Giant";
            }
            return dropType;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine(Lang.Get(Code.Domain + ":info-plugandfeather-heldinfo(range={0})", Attributes["searchrange"]));
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
