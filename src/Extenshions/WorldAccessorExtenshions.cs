using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace StoneQuarry
{
    public static class WorldAccessorExtenshions
    {
        public static CollectibleObject GetCollectibleObject(this IWorldAccessor world, AssetLocation code)
        {
            return (CollectibleObject)world.GetItem(code) ?? world.GetBlock(code);
        }
        public static bool IsPlayerCanBreakBlock(this IWorldAccessor world, BlockPos pos, IServerPlayer byPlayer)
        {
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                return true;
            }

            var sapi = byPlayer.Entity.Api as ICoreServerAPI;

            IList<LandClaim> claims = sapi.WorldManager.SaveGame.LandClaims;
            foreach (LandClaim claim in claims)
            {
                if (claim.PositionInside(pos))
                {
                    EnumPlayerAccessResult result = claim.TestPlayerAccess(byPlayer, EnumBlockAccessFlags.BuildOrBreak);
                    return result != EnumPlayerAccessResult.Denied;
                }
            }

            return true;
        }
    }
}
