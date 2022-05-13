using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    public class BERubbleStorage : BlockEntity, ITexPositionSource
    {
        public int MaxStorable => Block.Attributes["maxStorable"].AsInt(0);

        private int _lastContentLevel = 0;
        public int CurrentContentLevel
        {
            get
            {
                float topLevel = (1 - ((float)CurrentQuantity / MaxStorable));
                topLevel = (float)Math.Round(topLevel, 1);
                return (int)(topLevel * 10);
            }
        }

        private string? _currentTop = null;
        public AssetLocation? StoredRock { get; set; } = null;

        public int CurrentQuantity => Content["stone"] + Content["gravel"] + Content["sand"];
        public Dictionary<string, int> Content { get; private set; } = new Dictionary<string, int>()
        {
            { "stone", 0 },
            { "gravel", 0 },
            { "sand", 0 },
        };

        public EnumStorageLock StorageLock { get; set; } = EnumStorageLock.None;


        public Size2i? AtlasSize => (Api as ICoreClientAPI)?.BlockTextureAtlas.Size;
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (textureCode == "stone" || textureCode == "gravel" || textureCode == "sand")
                {
                    if (StoredRock != null)
                    {
                        return blockTexSource[textureCode + "-" + StoredRock];
                    }
                }
                return blockTexSource[textureCode];
            }
        }


#nullable disable
        private ITexPositionSource blockTexSource;
        public RockManager rockManager;
#nullable restore

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            rockManager = api.ModLoader.GetModSystem<RockManager>();

            if (api is ICoreClientAPI capi)
            {
                blockTexSource = capi.Tesselator.GetTexSource(Block);
            }

            CheckCurrentTop();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (StoredRock != null)
            {
                if (_currentTop != null)
                {
                    string topName = "top-" + _currentTop + "-" + StoredRock;
                    string topPath = Core.ModId + ":shapes/block/rubblestorage/top-" + _currentTop + ".json";

                    Vec3f? offset = null;
                    if (_currentTop != "plate")
                    {
                        topName += "-" + CurrentContentLevel;
                        offset = new Vec3f(0, -0.06f * CurrentContentLevel, 0);
                    }

                    MeshData topMesh = GetOrCreateMesh(topName, topPath, tessThreadTesselator, offset);

                    mesher.AddMeshData(topMesh);
                }

                string buttonsName = "buttons-" + StoredRock;
                string buttonsPath = Core.ModId + ":shapes/block/rubblestorage/buttons.json";
                MeshData buttonsMesh = GetOrCreateMesh(buttonsName, buttonsPath, tessThreadTesselator);
                mesher.AddMeshData(buttonsMesh);
            }

            return base.OnTesselation(mesher, tessThreadTesselator);
        }

        private MeshData GetOrCreateMesh(string name, string path, ITesselatorAPI tesselator, Vec3f? offset = null)
        {
            string key = $"{Core.ModId}-rubblestorage-{name}-{Block.LastCodePart()}";
            return ObjectCacheUtil.GetOrCreate(Api, key, () =>
            {
                Shape shape = Api.Assets.Get<Shape>(new AssetLocation(path));
                tesselator.TesselateShape(nameof(BERubbleStorage), shape, out MeshData mesh, this);

                if (offset != null)
                {
                    mesh.Translate(offset);
                }

                float radY = 0;
                switch (Block.LastCodePart())
                {
                    case "north":
                        radY = 0;
                        break;
                    case "east":
                        radY = (float)(1.5 * Math.PI); //270
                        break;
                    case "south":
                        radY = (float)Math.PI; //180
                        break;
                    case "west":
                        radY = (float)(0.5 * Math.PI); //90
                        break;
                }
                return mesh.Rotate(new Vec3f(.5f, .5f, .5f), 0, radY, 0);
            });
        }

        /// <summary>
        /// Set's the displayed block to the type that has the largest amount of stored material.
        /// </summary>
        public void CheckCurrentTop()
        {
            string? newTop = null;
            int maxQuantity = GameMath.Max(Content.Values.ToArray());

            if (maxQuantity > 0)
            {
                newTop = Content.First((val) => val.Value == maxQuantity).Key;
            }

            if (newTop != _currentTop || CurrentContentLevel != _lastContentLevel)
            {
                _currentTop = newTop;
                _lastContentLevel = CurrentContentLevel;
                MarkDirty(true);
            }
        }

        public bool TryRemoveResource(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, string contentType, int quantity)
        {
            if (Content[contentType] <= 0 || StoredRock == null)
            {
                return false;
            }

            quantity = GameMath.Clamp(quantity, 0, Content[contentType]);

            ItemStack giveStack;
            var code = rockManager.GetValue(StoredRock, contentType);
            if (code != null)
            {
                var colObj = Api.World.GetCollectibleObject(code);
                if (colObj != null)
                {
                    giveStack = new ItemStack(colObj, Math.Min(colObj.MaxStackSize, quantity));

                    if (!byPlayer.InventoryManager.TryGiveItemstack(giveStack.Clone()))
                    {
                        world.SpawnItemEntity(giveStack.Clone(), blockSel.HitPosition + (blockSel.HitPosition.Normalize() * .5) + blockSel.Position.ToVec3d(), blockSel.HitPosition.Normalize() * .05);
                    }
                    Content[contentType] -= giveStack.StackSize;

                    return true;
                }
            }

            return false;
        }

        public bool TryAddResource(ItemSlot slot, int quantity)
        {
            if (slot?.Itemstack == null)
            {
                return false;
            }

            int availableSpace = MaxStorable - CurrentQuantity;
            quantity = quantity > availableSpace ? availableSpace : quantity;
            quantity = quantity > slot.StackSize ? slot.StackSize : quantity;

            if (quantity <= 0)
            {
                return false;
            }


            AssetLocation code = slot.Itemstack.Collectible.Code;
            if (rockManager.TryResolveCode(code, out string? contentType, out AssetLocation? rockName))
            {
                if (StoredRock == null)
                {
                    StoredRock = rockName.Clone();
                }

                if (StoredRock.Equals(rockName))
                {
                    if (Content.ContainsKey(contentType))
                    {
                        Content[contentType] += quantity;
                        slot.TakeOut(quantity);
                        slot.MarkDirty();
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Will attempt to add all eligible items from the player's inventory.
        /// </summary>
        public bool TryAddAll(IPlayer byPlayer)
        {
            bool flag = false;

            foreach (var inv in byPlayer.InventoryManager.Inventories)
            {
                if (inv.Key.Contains("creative")) continue;

                foreach (var slot in inv.Value)
                {
                    if (TryAddResource(slot, slot.StackSize))
                    {
                        flag = true;
                    }
                }
            }

            return flag;
        }

        /// <summary>
        /// Turns stone into gravel and gravel into sand.
        /// </summary>
        public bool TryDegradeNext() => StorageLock switch
        {
            EnumStorageLock.Stone => TryDegrade("gravel", "sand"),
            EnumStorageLock.Gravel => TryDegrade("stone", "gravel"),
            EnumStorageLock.Sand => TryDegrade("gravel", "sand") || TryDegrade("stone", "sand", false),
            _ => TryDegrade("stone", "sand", true) || TryDegrade("gravel", "sand"),
        };

        private bool TryDegrade(string from, string to, bool split = true)
        {
            if (from == "stone" && Content["stone"] > 1)
            {
                if (to == "gravel")
                {
                    Content["gravel"] += 1;
                }
                else if (to == "sand")
                {
                    float mpl = (float)Content["stone"] / Content["gravel"];
                    bool toSand = Content["sand"] * mpl < Content["gravel"] || !split;
                    Content[toSand ? "sand" : "gravel"] += 1;
                }
                else return false;

                Content["stone"] -= 2;
                return true;
            }

            if (from == "gravel" && to == "sand" && Content["gravel"] > 0)
            {
                Content["gravel"] -= 1;
                Content["sand"] += 1;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try to create a muddy gravel.
        /// </summary>
        public bool TryDrench(IWorldAccessor world, BlockSelection blockSel, IPlayer byPlayer)
        {
            var portion = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetTreeAttribute("contents");
            ItemStack? portionStack = portion.GetItemstack("0");

            if (portionStack.Collectible.Code.Equals(new AssetLocation("game", "waterportion")))
            {
                Block dropblock = world.GetBlock(new AssetLocation("game", "muddygravel"));
                if (Content["gravel"] > 0)
                {
                    // Deviding max stack drop by four because not doing so created a mess.
                    ItemStack dropStack = new(dropblock, Math.Min(dropblock.MaxStackSize / 4, Content["gravel"]));

                    world.SpawnItemEntity(dropStack.Clone(),
                        blockSel.Position.ToVec3d() + blockSel.HitPosition,
                        blockSel.Face.Normalf.ToVec3d() * .05 + new Vec3d(0, .08, 0));

                    Content["gravel"] -= Math.Min(dropblock.MaxStackSize / 4, Content["gravel"]);

                    portionStack.StackSize -= 100;
                    if (portionStack.StackSize <= 0)
                    {
                        portionStack = null;
                    }

                    portion.SetItemstack("0", portionStack);
                    return true;
                }
            }

            return false;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            string? storedType = tree.GetString("storedType", null);
            if (storedType != null)
            {
                StoredRock = new AssetLocation(storedType);
            }

            Content["sand"] = tree.GetInt("sand", 0);
            Content["gravel"] = tree.GetInt("gravel", 0);
            Content["stone"] = tree.GetInt("stone", 0);
            StorageLock = (EnumStorageLock)tree.GetInt("storageLock", 0);

            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if (StoredRock != null)
            {
                tree.SetString("storedType", StoredRock.ToShortString());
            }

            tree.SetInt("sand", Content["sand"]);
            tree.SetInt("gravel", Content["gravel"]);
            tree.SetInt("stone", Content["stone"]);
            tree.SetInt("storageLock", (int)StorageLock);

            base.ToTreeAttributes(tree);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            string stone = Lang.Get(Core.ModId + ":info-rubblestorage-stone(count={0})", Content["stone"]);
            string gravel = Lang.Get(Core.ModId + ":info-rubblestorage-gravel(count={0})", Content["gravel"]);
            string sand = Lang.Get(Core.ModId + ":info-rubblestorage-sand(count={0})", Content["sand"]);

            string locked = Lang.Get(Core.ModId + ":info-rubblestorage-locked");
            switch (StorageLock)
            {
                case EnumStorageLock.Stone:
                    stone += locked;
                    break;
                case EnumStorageLock.Gravel:
                    gravel += locked;
                    break;
                case EnumStorageLock.Sand:
                    sand += locked;
                    break;
            }

            string rockType = Lang.Get(Core.ModId + ":info-rubblestorage-none");
            if (StoredRock != null)
            {
                rockType = Lang.Get(StoredRock + "");
            }

            dsc.AppendLine(Lang.Get(Core.ModId + ":info-rubblestorage-type(type={0})", rockType));
            dsc.AppendLine(stone);
            dsc.AppendLine(gravel);
            dsc.AppendLine(sand);
        }
    }

    public enum EnumStorageLock : int
    {
        None = 0,
        Sand = 1,
        Gravel = 2,
        Stone = 3
    }
}