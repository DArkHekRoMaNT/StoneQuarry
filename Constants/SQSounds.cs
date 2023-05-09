using Vintagestory.API.Common;

namespace StoneQuarry
{
    public static class SQSounds
    {
        public static AssetLocation Crack => new("game:sounds/block/heavyice");
        public static AssetLocation QuarryCrack => new("game:sounds/effect/rockslide");
        public static AssetLocation MetalHit => new("game:sounds/block/meteoriciron-hit-pickaxe");
        public static AssetLocation RockHit => new("game:sounds/block/rock-hit-pickaxe");
        public static AssetLocation StoneCrush => new("game:sounds/effect/stonecrush");
        public static AssetLocation WaterSplash => new("game:sounds/effect/water-pour");
    }
}
