using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Common;

namespace StoneQuarry
{
    public interface IRockManager
    {
        IReadOnlyList<RockData> Data { get; }
        string? GetRockType(AssetLocation code);

        RockData? GetValue(AssetLocation rock);

        AssetLocation? GetValue(AssetLocation rock, string type);

        bool IsSuitable(AssetLocation code, string? type = null);

        bool IsSuitableRock(AssetLocation rock);

        bool TryResolveCode(AssetLocation code, [NotNullWhen(true)] out string? type, [NotNullWhen(true)] out AssetLocation? rock);
    }
}
