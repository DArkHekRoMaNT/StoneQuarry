using CommonLib.Config;
using CommonLib.Extensions;
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
        private PlugPreviewManager? _previewManager;

        private Config Config { get; set; } = null!;

        public int MaxSearchRange => (int)(Math.Min(1024, Math.Round(Attributes["searchrange"].AsInt(0) * Config.PlugSizeModifier)));

        public string Material => Variant["metal"];
        public string Orientation => Variant["orientation"];
        public string Direction => Variant["direction"];

        public static int MaxStage => 2;
        public int Stage => int.Parse(Variant["stage"]);

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side == EnumAppSide.Client)
            {
                _previewManager = api.ModLoader.GetModSystem<Core>().PlugPreviewManager;
            }

            var configs = api.ModLoader.GetModSystem<ConfigManager>();
            Config = configs.GetConfig<Config>();
        }

        public override int GetMaxDurability(ItemStack itemstack)
        {
            return GetPlugMaxDurability();
        }

        private int GetPlugMaxDurability()
        {
            int durability = Material switch
            {
                "copper" => Config.CopperPlugDurability,
                "tinbronze" => Config.BronzePlugDurability,
                "bismuthbronze" => Config.BronzePlugDurability,
                "blackbronze" => Config.BronzePlugDurability,
                "iron" => Config.IronPlugDurability,
                "meteoriciron" => Config.IronPlugDurability,
                "steel" => Config.SteelPlugDurability,
                "admin" => 0,
                _ => 0
            };

            return Config.EnablePlugDurability ? durability : 0;
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            GetDirectionAndOrientation(world, byPlayer, blockSel, out string orientation, out string direction);

            Block orientedBlock = world.GetBlock(CodeWithVariants(new Dictionary<string, string> {
                { "orientation", orientation },
                { "direction", direction }
            }));

            world.BlockAccessor.SetBlock(orientedBlock.Id, blockSel.Position, byItemStack);

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEPlugAndFeather be)
            {
                be.Durability = byItemStack.Attributes.GetAsInt("durability", GetPlugMaxDurability());
            }

            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (api is ICoreServerAPI sapi)
            {
                sapi.Network.SendBlockEntityPacket(byPlayer as IServerPlayer, pos, Constants.PreviewPacketId);
            }

            if (world.BlockAccessor.GetBlockEntity(pos) is BEPlugAndFeather be && be.IsNetworkPart)
            {
                if (be.IsDone(world))
                {
                    Vec3d dropPos = be.GetDropPos();
                    foreach (BlockPos point in be.Points.ToArray())
                    {
                        ItemStack dropStack = GetDrops(world, pos, byPlayer, dropQuantityMultiplier)[0].Clone();

                        if (Config.EnablePlugDurability)
                        {
                            if (world.BlockAccessor.GetBlockEntity(point) is BEPlugAndFeather pointBE)
                            {
                                int durability;

                                if (pointBE.Durability == -1)
                                {
                                    durability = GetPlugMaxDurability() - 1;
                                }
                                else
                                {
                                    durability = pointBE.Durability - 1;
                                }

                                if (durability > 0)
                                {
                                    dropStack.Attributes.SetInt("durability", durability);
                                    world.SpawnItemEntity(dropStack, dropPos);
                                }
                            }
                        }

                        else if (world.Rand.NextDouble() >= Config.BreakPlugChance)
                        {
                            world.SpawnItemEntity(dropStack, dropPos);
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
                            pb.SwitchStage(0, world, pos);
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

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            ItemStack drop = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier)[0];

            if (world.BlockAccessor.GetBlockEntity(pos) is BEPlugAndFeather be)
            {
                if (be.Durability > 0)
                {
                    drop.Attributes.SetInt("durability", be.Durability);
                }
            }

            return new ItemStack[] { drop };
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            GetDirectionAndOrientation(world, byPlayer, blockSel,
                out string orientation, out string direction);

            BlockFacing facing = orientation switch
            {
                "up" => BlockFacing.UP,
                "down" => BlockFacing.DOWN,
                "horizontal" => BlockFacing.FromCode(direction),
                _ => throw new NotImplementedException()
            };

            Block block = world.BlockAccessor.GetBlock(blockSel.Position + facing.Normali.ToBlockPos());

            if (block.SideSolid.OnSide(facing.Opposite))
            {
                return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            }
            else
            {
                failureCode = "requireattachable";
                return false;
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                world.BlockAccessor.MarkBlockDirty(blockSel.Position);
                return false;
            }

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEPlugAndFeather be)
            {
                if (byPlayer.Entity.Controls.Sneak)
                {
                    _previewManager?.TogglePreview(blockSel.Position);
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
                            world.PlaySoundAt(SQSounds.Crack, byPlayer, byPlayer, true, 32, .5f);
                            SwitchStage(Stage + 1, world, blockSel.Position);
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
                    world.PlaySoundAt(SQSounds.MetalHit, byPlayer, byPlayer, true, 32, .5f);

                    return true;
                }
            }
            return false;
        }

        public void SwitchStage(int newStage, IWorldAccessor world, BlockPos pos)
        {
            if (0 <= newStage && newStage <= MaxStage)
            {
                AssetLocation newCode = CodeWithVariant("stage", $"{newStage}");
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
                        }
                    }

                    return true;
                }
            }

            return false;

            List<BlockPos>? FindNetworkPoints(IWorldAccessor world, BlockPos pos)
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

                BlockPos direction = Direction switch
                {
                    "north" => BlockFacing.EAST.Normali.ToBlockPos(),
                    "east" => BlockFacing.SOUTH.Normali.ToBlockPos(),
                    "south" => BlockFacing.WEST.Normali.ToBlockPos(),
                    "west" => BlockFacing.NORTH.Normali.ToBlockPos(),
                    _ => throw new NotImplementedException()
                };
                BlockPos oppositeDirection = direction * -1;

                bool inRow = true;
                bool inOppositeRow = true;
                for (int step = 1; step <= MaxSearchRange + 1; step++)
                {
                    if (inRow)
                    {
                        inRow = HasNextPlug(direction, step);
                        if (points.Count >= MaxSearchRange * 2)
                        {
                            break;
                        }
                    }

                    if (inOppositeRow)
                    {
                        inOppositeRow = HasNextPlug(oppositeDirection, step);
                        if (points.Count >= MaxSearchRange * 2)
                        {
                            break;
                        }
                    }
                }

                return points;

                bool HasNextPlug(BlockPos movingDirection, int step)
                {
                    BlockPos moving = movingDirection * step;
                    BlockPos checkPos = pos + moving;
                    BlockPos checkOppositePos = oppositePos + moving;

                    bool isSuited = IsSuitedNetworkPart(this, checkPos);
                    bool isOppositeSuited = IsSuitedNetworkPart(oppositeBlock, checkOppositePos);

                    if (isSuited && isOppositeSuited)
                    {
                        points.Add(checkPos);
                        points.Add(checkOppositePos);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                bool IsSuitedNetworkPart(BlockPlugAndFeather? template, BlockPos pos)
                {
                    return template is not null &&
                        world.BlockAccessor.GetBlock(pos) is BlockPlugAndFeather block &&
                        world.BlockAccessor.GetBlockEntity(pos) is BEPlugAndFeather be &&

                        !be.IsNetworkPart &&
                        block.Material == template.Material &&
                        block.Direction == template.Direction &&
                        block.Orientation == template.Orientation;
                }
            }

            BlockPos? FindOppositePlug(IWorldAccessor world, BlockPos pos)
            {
                BlockPos? subDir = null;
                BlockPos? mainDir = null;
                string[]? oppositeDirection = null;

                ResolveDirections();

                if (mainDir is null || subDir is null || oppositeDirection is null)
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


                        bool isSuited1 = world.BlockAccessor.GetBlockEntity(pos1) is BEPlugAndFeather be1 &&
                            !be1.IsNetworkPart &&
                            block1?.Material == Material &&
                            oppositeDirection.Contains(block1.Direction);

                        bool isSuited2 = world.BlockAccessor.GetBlockEntity(pos2) is BEPlugAndFeather be2 &&
                            !be2.IsNetworkPart &&
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

                void ResolveDirections()
                {
                    if (Orientation == "up")
                    {
                        mainDir = BlockFacing.UP.Normali.ToBlockPos();
                        if (Direction == "north" || Direction == "south")
                        {
                            subDir = BlockFacing.SOUTH.Normali.ToBlockPos();
                            oppositeDirection = ["north", "south"];
                        }
                        else if (Direction == "east" || Direction == "west")
                        {
                            subDir = BlockFacing.EAST.Normali.ToBlockPos();
                            oppositeDirection = ["east", "west"];
                        }
                    }
                    else if (Orientation == "down")
                    {
                        mainDir = BlockFacing.DOWN.Normali.ToBlockPos();
                        if (Direction == "north" || Direction == "south")
                        {
                            subDir = BlockFacing.SOUTH.Normali.ToBlockPos();
                            oppositeDirection = ["north", "south"];
                        }
                        else if (Direction == "east" || Direction == "west")
                        {
                            subDir = BlockFacing.EAST.Normali.ToBlockPos();
                            oppositeDirection = ["east", "west"];
                        }
                    }
                    else if (Orientation == "horizontal")
                    {
                        subDir = BlockFacing.UP.Normali.ToBlockPos();
                        if (Direction == "north")
                        {
                            mainDir = BlockFacing.NORTH.Normali.ToBlockPos();
                            oppositeDirection = ["north", "south"];
                        }
                        else if (Direction == "east")
                        {
                            mainDir = BlockFacing.EAST.Normali.ToBlockPos();
                            oppositeDirection = ["east", "west"];
                        }
                        else if (Direction == "south")
                        {
                            mainDir = BlockFacing.SOUTH.Normali.ToBlockPos();
                            oppositeDirection = ["north", "south"];
                        }
                        else if (Direction == "west")
                        {
                            mainDir = BlockFacing.WEST.Normali.ToBlockPos();
                            oppositeDirection = ["east", "west"];
                        }
                    }
                }
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLineOnce();
            dsc.Append(Lang.Get($"{Core.ModId}:info-plugandfeather-heldinfo(range={{0}})", MaxSearchRange));
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var be = world.BlockAccessor.GetBlockEntity(selection.Position) as BEPlugAndFeather;
            bool isNetworkPart = be?.IsNetworkPart == true;

            return new[] {
                new WorldInteraction
                {
                    ActionLangCode = $"{Core.ModId}:wi-plugandfeather-quarryimpact",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = ObjectCacheUtil.GetOrCreate(world.Api, $"{Core.ModId}-plugimpact", () =>
                    {
                        var list = new List<ItemStack>();
                        foreach (var colObj in api.World.Collectibles)
                        {
                            if (colObj.Attributes != null)
                            {
                                if (colObj.Attributes["plugimpact"].Exists)
                                {
                                    list.Add(new ItemStack(colObj));
                                }
                            }
                        }
                        return list.ToArray();
                    })
                } }
                .AppendIf(isNetworkPart, new WorldInteraction
                {
                    ActionLangCode = $"{Core.ModId}:wi-plugandfeather-togglepreview",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sneak"
                })
                .Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        private static void GetDirectionAndOrientation(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, out string orientation, out string direction)
        {
            BlockPos offset = blockSel.Face.Opposite.Normali.AsBlockPos;
            Block block = world.BlockAccessor.GetBlock(blockSel.Position + offset);
            if (block is BlockPlugAndFeather plug)
            {
                orientation = plug.Orientation;
                direction = plug.Direction;
                return;
            }

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
        }
    }
}
