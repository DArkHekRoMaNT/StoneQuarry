using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    public class BlockPlugAndFeather : Block
    {
        public static AssetLocation CrackSoundLocation => new("game", "sounds/block/heavyice");
        public static AssetLocation HammerSoundLocation => new("game", "sounds/block/meteoriciron-hit-pickaxe");

        public SimpleParticleProperties QuarryStartParticles { get; private set; }

        public WorldInteraction[] CommonWorldInteractions { get; private set; }
        public WorldInteraction[] NetworkPartWorldInteractions { get; private set; }

        public int MaxSearchRange => Attributes["searchrange"].AsInt(0);
        public string Material => Variant["metal"];
        public string Orientation => Variant["orientation"];
        public string Direction => Variant["direction"];


        public readonly int MaxStage = 2;
        public int Stage => int.Parse(Variant["stage"]);


        public BlockPlugAndFeather()
        {
            QuarryStartParticles = new SimpleParticleProperties()
            {
                MinQuantity = 16,
                AddQuantity = 16,
                MinSize = .5f,
                MaxSize = 2f,
                MinPos = new Vec3d(),
                AddPos = new Vec3d(1, 1, 1),
                ColorByBlock = this,
                LifeLength = .5f,
                MinVelocity = new Vec3f(0, -1, 0),
                ParticleModel = EnumParticleModel.Cube
            };

            CommonWorldInteractions = Array.Empty<WorldInteraction>();
            NetworkPartWorldInteractions = Array.Empty<WorldInteraction>();
        }


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            InitWorldInteractions();
        }

        private void InitWorldInteractions()
        {
            var quarryImpact = new List<ItemStack>();
            foreach (var colObj in api.World.Collectibles)
            {
                if (colObj.Attributes != null)
                {
                    if (colObj.Attributes["plugimpact"].Exists)
                    {
                        quarryImpact.Add(new ItemStack(colObj));
                    }
                }
            }

            CommonWorldInteractions = new WorldInteraction[] {
                new WorldInteraction(){
                    ActionLangCode = Code.Domain + ":wi-plugandfeather-quarryimpact",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = quarryImpact.ToArray()
                }
            };

            NetworkPartWorldInteractions = new WorldInteraction[] {
                new WorldInteraction(){
                    ActionLangCode = Code.Domain + ":wi-plugandfeather-togglepreview",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sneak"
                }
            };
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            string orientation, direction;

            if (blockSel.Face == BlockFacing.DOWN)
            {
                orientation = "up";
                direction = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
            }
            else if (blockSel.Face == BlockFacing.UP)
            {
                orientation = "down";
                direction = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
            }
            else
            {
                orientation = "horizontal";
                direction = blockSel.Face.Opposite.ToString();
            }

            Block orientedBlock = world.GetBlock(CodeWithVariants(new Dictionary<string, string>() {
                { "orientation", orientation },
                { "direction", direction }
            }));

            world.BlockAccessor.SetBlock(orientedBlock.Id, blockSel.Position, byItemStack);

            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BEPlugAndFeather be && be.IsNetworkPart)
            {
                if (be.IsDone(world))
                {
                    foreach (var point in be.Points.ToArray())
                    {
                        if (world.Rand.NextDouble() >= Core.Config.BreakPlugChance)
                        {
                            foreach (var dropStack in GetDrops(world, pos, byPlayer, dropQuantityMultiplier))
                            {
                                world.SpawnItemEntity(dropStack.Clone(), point.ToVec3d().Add(.5, .5, .5));
                            }
                        }

                        world.BlockAccessor.SetBlock(0, point);
                        world.BlockAccessor.MarkBlockDirty(point);
                    }
                }
                else
                {
                    foreach (var point in be.Points.ToArray())
                    {
                        if (world.BlockAccessor.GetBlockEntity(point) is BEPlugAndFeather pbe)
                        {
                            pbe.Points.Clear();
                        }
                        if (world.BlockAccessor.GetBlock(point) is BlockPlugAndFeather pb)
                        {
                            pb.SwitchState(0, world, pos);
                        }
                    }

                    base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                }
            }
            else
            {
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEPlugAndFeather be)
            {
                if (byPlayer.Entity.Controls.Sneak)
                {
                    be.TogglePreview();
                    return true;
                }

                var activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
                bool isPlugImpactTool = activeStack?.ItemAttributes?.KeyExists("plugimpact") ?? false;

                if (isPlugImpactTool)
                {
                    if (!be.IsNetworkPart)
                    {
                        TryCreateNetwork(world, blockSel.Position, byPlayer);
                    }

                    if (activeStack != null && be.TryHitPlug(activeStack))
                    {
                        if (Stage != MaxStage)
                        {
                            world.PlaySoundAt(CrackSoundLocation, byPlayer, byPlayer, true, 32, .5f);
                            SwitchState(Stage + 1, world, blockSel.Position);
                            activeStack.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot);
                        }

                        if (byPlayer is IServerPlayer serverPlayer)
                        {
                            if (be.IsDone(world))
                            {
                                be.BreakAll(world, serverPlayer);
                            }
                        }
                    }

                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                    world.PlaySoundAt(HammerSoundLocation, byPlayer, byPlayer, true, 32, .5f);

                    return true;
                }
            }
            return false;
        }

        public void SwitchState(int newStage, IWorldAccessor world, BlockPos pos)
        {
            if (0 <= newStage && newStage <= MaxStage)
            {
                AssetLocation newCode = CodeWithVariant("stage", newStage + "");
                int? blockId = world.GetBlock(newCode)?.Id;
                world.BlockAccessor.ExchangeBlock(blockId ?? 0, pos);
            }

            world.SpawnCubeParticles(pos, pos.ToVec3d().Add(.5, .5, .5), 1, 5);
        }

        public bool TryCreateNetwork(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            ItemStack activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            bool isImpactTool = activeStack?.ItemAttributes?.KeyExists("plugimpact") ?? false;

            if (isImpactTool)
            {
                List<BlockPos>? points = FindNetworkPoints(world, pos);

                if (points != null)
                {
                    foreach (var p in points)
                    {
                        if (world.BlockAccessor.GetBlockEntity(p) is BEPlugAndFeather pbe)
                        {
                            pbe.Points.AddRange(points);

                            QuarryStartParticles.MinPos = p.ToVec3d();

                            world.SpawnParticles(QuarryStartParticles, byPlayer);
                        }
                    }

                    return true;
                }

            }

            return false;
        }

        private List<BlockPos>? FindNetworkPoints(IWorldAccessor world, BlockPos pos)
        {
            List<BlockPos> points = new();

            BlockPos? oppositePos = FindOppositePlug(world, pos);
            if (oppositePos == null)
            {
                return null;
            }

            var oppositeBlock = world.BlockAccessor.GetBlock(oppositePos) as BlockPlugAndFeather;

            points.Add(pos);
            points.Add(oppositePos);


            BlockPos? checkDir = null;
            switch (Direction)
            {
                case "north": checkDir = BlockFacing.EAST.Normali.ToBlockPos(); break;
                case "east": checkDir = BlockFacing.SOUTH.Normali.ToBlockPos(); break;
                case "south": checkDir = BlockFacing.WEST.Normali.ToBlockPos(); break;
                case "west": checkDir = BlockFacing.NORTH.Normali.ToBlockPos(); break;
            }

            bool ended1 = false, ended2 = false;
            for (int r = 1; r <= MaxSearchRange + 1; r++)
            {
                if (!ended1)
                {
                    BlockPos checkPos = pos + (checkDir * r);
                    BlockPos checkOppositePos = oppositePos + (checkDir * r);

                    bool isSuited = IsSuitedNetworkPart(this, checkPos, world);
                    bool isOppositeSuited = IsSuitedNetworkPart(oppositeBlock, checkOppositePos, world);

                    if (isSuited && isOppositeSuited)
                    {
                        points.Add(checkPos);
                        points.Add(checkOppositePos);

                        if (points.Count >= MaxSearchRange * 2)
                        {
                            break;
                        }
                    }
                    else
                    {
                        ended1 = true;
                    }
                }

                if (!ended2)
                {
                    BlockPos checkPos = pos - (checkDir * r);
                    BlockPos checkOppositePos = oppositePos - (checkDir * r);

                    bool isSuited = IsSuitedNetworkPart(this, checkPos, world);
                    bool isOppositeSuited = IsSuitedNetworkPart(oppositeBlock, checkOppositePos, world);

                    if (isSuited && isOppositeSuited)
                    {
                        points.Add(checkPos);
                        points.Add(checkOppositePos);

                        if (points.Count >= MaxSearchRange * 2)
                        {
                            break;
                        }
                    }
                    else
                    {
                        ended2 = true;
                    }
                }
            }

            return points;
        }

        private BlockPos? FindOppositePlug(IWorldAccessor world, BlockPos pos)
        {
            BlockPos? subDir = null;
            BlockPos? mainDir = null;
            string[]? oppositeDirection = null;

            if (Orientation == "up")
            {
                mainDir = BlockFacing.UP.Normali.ToBlockPos();
                if (Direction == "north" || Direction == "south")
                {
                    subDir = BlockFacing.SOUTH.Normali.ToBlockPos();
                    oppositeDirection = new string[] { "north", "south" };
                }
                else if (Direction == "east" || Direction == "west")
                {
                    subDir = BlockFacing.EAST.Normali.ToBlockPos();
                    oppositeDirection = new string[] { "east", "west" };
                }
            }
            else if (Orientation == "down")
            {
                mainDir = BlockFacing.DOWN.Normali.ToBlockPos();
                if (Direction == "north" || Direction == "south")
                {
                    subDir = BlockFacing.SOUTH.Normali.ToBlockPos();
                    oppositeDirection = new string[] { "north", "south" };
                }
                else if (Direction == "east" || Direction == "west")
                {
                    subDir = BlockFacing.EAST.Normali.ToBlockPos();
                    oppositeDirection = new string[] { "east", "west" };
                }
            }
            else if (Orientation == "horizontal")
            {
                subDir = BlockFacing.UP.Normali.ToBlockPos();
                if (Direction == "north")
                {
                    mainDir = BlockFacing.NORTH.Normali.ToBlockPos();
                    oppositeDirection = new string[] { "north", "south" };
                }
                else if (Direction == "east")
                {
                    mainDir = BlockFacing.EAST.Normali.ToBlockPos();
                    oppositeDirection = new string[] { "east", "west" };
                }
                else if (Direction == "south")
                {
                    mainDir = BlockFacing.SOUTH.Normali.ToBlockPos();
                    oppositeDirection = new string[] { "north", "south" };
                }
                else if (Direction == "west")
                {
                    mainDir = BlockFacing.WEST.Normali.ToBlockPos();
                    oppositeDirection = new string[] { "east", "west" };
                }
            }

            if (mainDir == null || subDir == null || oppositeDirection == null)
            {
                return null;
            }

            for (int i = 1; i <= MaxSearchRange; i++)
            {
                for (int j = 1; j <= MaxSearchRange; j++)
                {
                    BlockPos pos1 = pos + mainDir * i + subDir * j;
                    BlockPos pos2 = pos + mainDir * i - subDir * j;

                    var block1 = world.BlockAccessor.GetBlock(pos1) as BlockPlugAndFeather;
                    var block2 = world.BlockAccessor.GetBlock(pos2) as BlockPlugAndFeather;

#pragma warning disable IDE0019 // Use pattern matching
                    var be1 = world.BlockAccessor.GetBlockEntity(pos1) as BEPlugAndFeather;
                    var be2 = world.BlockAccessor.GetBlockEntity(pos2) as BEPlugAndFeather;
#pragma warning restore IDE0019 // Use pattern matching

                    bool isSuited1 = be1 != null && !be1.IsNetworkPart &&
                        block1?.Material == Material &&
                        oppositeDirection.Contains(block1.Direction);

                    bool isSuited2 = be2 != null && !be2.IsNetworkPart &&
                        block2?.Material == Material &&
                        oppositeDirection.Contains(block2.Direction);

                    if (Orientation == "horizontal")
                    {
                        if (isSuited1 && block1?.Orientation == "down")
                        {
                            return pos1;
                        }
                        if (isSuited2 && block2?.Orientation == "up")
                        {
                            return pos2;
                        }
                    }
                    else if (Orientation == "up" || Orientation == "down")
                    {
                        if (isSuited1 && block1?.Orientation == "horizontal")
                        {
                            return pos1;
                        }
                        if (isSuited2 && block2?.Orientation == "horizontal")
                        {
                            return pos2;
                        }
                    }
                }
            }

            return null;
        }

        private static bool IsSuitedNetworkPart(BlockPlugAndFeather? template, BlockPos pos, IWorldAccessor world)
        {
            return template != null &&
                world.BlockAccessor.GetBlock(pos) is BlockPlugAndFeather block &&
                world.BlockAccessor.GetBlockEntity(pos) is BEPlugAndFeather be &&

                !be.IsNetworkPart &&
                block.Material == template.Material &&
                block.Direction == template.Direction &&
                block.Orientation == template.Orientation;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine(Lang.Get(Core.ModId + ":info-plugandfeather-heldinfo(range={0})", Attributes["searchrange"]));
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var wi = new List<WorldInteraction>(CommonWorldInteractions);

            if (world.BlockAccessor.GetBlockEntity(selection.Position) is BEPlugAndFeather be && be.IsNetworkPart)
            {
                wi.AddRange(NetworkPartWorldInteractions);
            }
            wi.AddRange(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));

            return wi.ToArray();
        }
    }
}
