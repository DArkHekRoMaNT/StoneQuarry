using Vintagestory.API.MathTools;

namespace StoneQuarry
{
    public static class Vec3iExtensions
    {
        public static Vec3f ToVec3f(this Vec3i pos)
        {
            return new Vec3f(pos.X, pos.Y, pos.Z);
        }
        public static Vec3d ToVec3d(this Vec3i pos)
        {
            return new Vec3d(pos.X, pos.Y, pos.Z);
        }
    }
}
