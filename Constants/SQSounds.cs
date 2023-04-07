using System;
using Vintagestory.API.Common;

namespace StoneQuarry
{
    public static class SQSounds
    {
        public static AssetLocation Crack => new("game:sounds/block/heavyice");
        public static AssetLocation Hit => new("game:sounds/block/meteoriciron-hit-pickaxe");
        public static AssetLocation RockHit => new("game:sounds/block/rock-hit-pickaxe");
        public static AssetLocation StoneCrush => new("game:sounds/effect/stonecrush");
        public static AssetLocation WaterSplash => new("game:sounds/environment/largesplash1");
    }
}
