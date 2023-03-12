using System;
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

        private string? _currentDisplayedTop = null;
        private int _lastContentLevel = 0;
        public int CurrentContentLevel
        {
            get
            {
                float topLevel = 1 - Inventory?.Filling ?? 0;
                topLevel = (float)Math.Round(topLevel, 1);
                return (int)(topLevel * 10);
            }
        }

        public EnumStorageLock StorageLock { get; set; } = EnumStorageLock.None;
        public RubbleStorageInventory? Inventory { get; private set; }

        public Size2i AtlasSize => ((ICoreClientAPI)Api).BlockTextureAtlas.Size;
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                ICoreClientAPI capi = (ICoreClientAPI)Api;

                if (Inventory != null)
                {
                    AssetLocation? rock = Inventory.StoredRock;
                    if (rock != null)
                    {
                        AssetLocation? blockCode = null;

                        if (textureCode == "gravel" || textureCode == "sand")
                        {
                            blockCode = Inventory.RockManager.GetValue(rock, textureCode);
                        }
                        else if (textureCode == "stone")
                        {
                            blockCode = rock;
                        }

                        if (blockCode != null)
                        {
                            Block? block = Api.World.GetBlock(blockCode);
                            if (block != null)
                            {
                                ITexPositionSource tex = capi.Tesselator.GetTexSource(block);
                                string otherCode = block.Textures.FirstOrDefault().Key;
                                return tex[otherCode];
                            }
                        }
                    }
                }

                return capi.Tesselator.GetTexSource(Block)[textureCode];
            }
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (Inventory == null)
            {
                Inventory = new RubbleStorageInventory(Api, Pos, MaxStorable);
            }

            UpdateDisplayedTop();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (Inventory?.StoredRock != null)
            {
                if (_currentDisplayedTop != null)
                {
                    string topName = "top-" + _currentDisplayedTop + "-" + Inventory.StoredRock;
                    string topPath = Core.ModId + ":shapes/block/rubblestorage/top-" + _currentDisplayedTop + ".json";

                    Vec3f? offset = null;
                    if (_currentDisplayedTop != "plate")
                    {
                        topName += "-" + CurrentContentLevel;
                        offset = new Vec3f(0, -0.06f * CurrentContentLevel, 0);
                    }

                    MeshData topMesh = GetOrCreateMesh(topName, topPath, tessThreadTesselator, offset);

                    mesher.AddMeshData(topMesh);
                }

                string buttonsName = "buttons-" + Inventory.StoredRock;
                string buttonsPath = Core.ModId + ":shapes/block/rubblestorage/buttons.json";
                MeshData buttonsMesh = GetOrCreateMesh(buttonsName, buttonsPath, tessThreadTesselator);
                mesher.AddMeshData(buttonsMesh);
            }

            return base.OnTesselation(mesher, tessThreadTesselator);
        }

        private MeshData GetOrCreateMesh(string meshName, string shapePath, ITesselatorAPI tesselator, Vec3f? offset = null)
        {
            string key = $"{Core.ModId}-rubblestorage-{meshName}-{Block.Variant["side"]}";
            return ObjectCacheUtil.GetOrCreate(Api, key, () =>
            {
                Shape shape = Api.Assets.Get<Shape>(new AssetLocation(shapePath));
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
        public void UpdateDisplayedTop()
        {
            int maxSize = Inventory.Max((s) => s.StackSize);
            RubbleStorageItemSlot slot = (RubbleStorageItemSlot)Inventory.First((s) => s.StackSize == maxSize);
            string? newTop = slot.Type;
            if (newTop != _currentDisplayedTop || CurrentContentLevel != _lastContentLevel)
            {
                _currentDisplayedTop = newTop;
                _lastContentLevel = CurrentContentLevel;
                MarkDirty(true);
            }
        }

        public bool TryRemoveResource(IWorldAccessor world, BlockSelection blockSel, string type, int quantity)
        {
            ItemStack? stack = Inventory?.GetResource(type, quantity);

            if (stack != null)
            {
                Vec3d dropPos = blockSel.HitPosition
                    + blockSel.HitPosition.Normalize() * .5
                    + blockSel.Position.ToVec3d();

                world.SpawnItemEntity(stack, dropPos, blockSel.HitPosition.Normalize() * .05);

                UpdateDisplayedTop();
                MarkDirty(true);

                return true;
            }

            return false;
        }

        public bool TryAddResource(ItemSlot fromSlot, int quantity)
        {
            bool flag = Inventory != null && Inventory.TryAddResource(fromSlot, quantity);
            if(flag)
            {
                UpdateDisplayedTop();
                MarkDirty(true);
            }
            return flag;
        }

        /// <summary>
        /// Will attempt to add all eligible items from the player's inventory.
        /// </summary>
        public bool TryAddAll(IPlayer byPlayer)
        {
            if (Inventory == null)
            {
                return false;
            }

            string[] suitedInventories =
            {
                GlobalConstants.hotBarInvClassName,
                GlobalConstants.backpackInvClassName
            };

            bool itemsAdded = false;

            foreach (var inv in byPlayer.InventoryManager.Inventories)
            {
                if (suitedInventories.Contains(inv.Value.ClassName))
                {
                    foreach (var slot in inv.Value)
                    {
                        if (Inventory.TryAddResource(slot, slot.StackSize))
                        {
                            itemsAdded = true;
                        }
                    }
                }
            }

            return itemsAdded;
        }

        /// <summary>
        /// Turns stone into gravel and gravel into sand.
        /// </summary>
        public bool TryDegrade() => Inventory != null && StorageLock switch
        {
            EnumStorageLock.Stone => Inventory.TryDegrade("gravel", "sand"),
            EnumStorageLock.Gravel => Inventory.TryDegrade("stone", "gravel"),
            EnumStorageLock.Sand => Inventory.TryDegrade("gravel", "sand") || Inventory.TryDegrade("stone", "sand", false),
            _ => Inventory.TryDegrade("stone", "sand", true) || Inventory.TryDegrade("gravel", "sand"),
        };

        /// <summary>
        /// Try to create a muddy gravel.
        /// </summary>
        public bool TryDrench(IWorldAccessor world, BlockSelection blockSel, IPlayer byPlayer)
        {
            if (Inventory == null)
            {
                return false;
            }

            ItemSlot? activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ITreeAttribute? portion = activeSlot?.Itemstack?.Attributes?.GetTreeAttribute("contents");

            if (portion != null)
            {
                int gravelQuantity = Inventory.GravelSlot.StackSize;
                ItemStack? portionStack = portion.GetItemstack("0");
                AssetLocation portionCode = portionStack.Collectible.Code;

                if (gravelQuantity > 0 && portionCode.Equals(new AssetLocation("game", "waterportion")))
                {
                    Block muddyGravelBlock = world.GetBlock(new AssetLocation("game", "muddygravel"));

                    // Deviding max stack drop by four because not doing so created a mess.
                    int quantity = Math.Min(muddyGravelBlock.MaxStackSize / 4, gravelQuantity);
                    ItemStack muddyGravelStack = new(muddyGravelBlock, quantity);
                    Inventory.GravelSlot.TakeOut(quantity);

                    world.SpawnItemEntity(muddyGravelStack.Clone(),
                        blockSel.Position.ToVec3d() + blockSel.HitPosition,
                        blockSel.Face.Normalf.ToVec3d() * .05 + new Vec3d(0, .08, 0));

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
            base.FromTreeAttributes(tree, worldAccessForResolve);
            StorageLock = (EnumStorageLock)tree.GetInt("storageLock", 0);
            Inventory = new RubbleStorageInventory(worldAccessForResolve.Api, Pos, MaxStorable);
            Inventory.FromTreeAttributes(tree);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("storageLock", (int)StorageLock);
            Inventory?.ToTreeAttributes(tree);
        }

        public ItemStack GetSelfStack()
        {
            ItemStack stack = new(Block);
            Inventory?.ToTreeAttributes(stack.Attributes);
            return stack;
        }

        public void SetDataFromStack(ItemStack stack)
        {
            Inventory = new RubbleStorageInventory(Api, Pos, MaxStorable);
            Inventory.FromTreeAttributes(stack.Attributes);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            if (Inventory != null)
            {
                if (Inventory.StoredRock == null)
                {
                    dsc.AppendLine(Lang.Get(Core.ModId + ":info-rubblestorage-empty"));
                }
                else
                {
                    string stoneLangCode = Core.ModId + ":info-rubblestorage-stone(count={0})";
                    string gravelLangCode = Core.ModId + ":info-rubblestorage-gravel(count={0})";
                    string sandLangCode = Core.ModId + ":info-rubblestorage-sand(count={0})";

                    string stoneText = Lang.Get(stoneLangCode, Inventory.StoneSlot.StackSize);
                    string gravelText = Lang.Get(gravelLangCode, Inventory.GravelSlot.StackSize);
                    string sandText = Lang.Get(sandLangCode, Inventory.SandSlot.StackSize);

                    string locked = Lang.Get(Core.ModId + ":info-rubblestorage-locked");
                    switch (StorageLock)
                    {
                        case EnumStorageLock.Stone:
                            stoneText += locked;
                            break;
                        case EnumStorageLock.Gravel:
                            gravelText += locked;
                            break;
                        case EnumStorageLock.Sand:
                            sandText += locked;
                            break;
                    }

                    string rockName = Lang.Get(Inventory.StoredRock.ToString());

                    dsc.AppendLine(Lang.Get(Core.ModId + ":info-rubblestorage-type(type={0})", rockName));
                    dsc.AppendLine(stoneText);
                    dsc.AppendLine(gravelText);
                    dsc.AppendLine(sandText);
                }
            }
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