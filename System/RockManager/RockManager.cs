using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    public class RockManager : ModSystem, IRockManager
    {
        private readonly Dictionary<AssetLocation, RockData> _data = new();

        public IReadOnlyList<RockData> Data => _data.Values.ToList();

        public override double ExecuteOrder() => 0.21; // Requires Block and Item Loader: 0.2

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Network
                .RegisterChannel(Constants.RockManagerChannelId)
                .RegisterMessageType<List<RockData>>()
                .SetMessageHandler<List<RockData>>((list) =>
                {
                    _data.Clear();
                    foreach (RockData rockData in list)
                    {
                        _data.Add(rockData.Rock, rockData);
                    }
                });
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            LoadRockData();

            var serverChannel = api.Network
                .RegisterChannel(Constants.RockManagerChannelId)
                .RegisterMessageType<List<RockData>>();

            api.Event.PlayerJoin += (player) =>
            {
                serverChannel.SendPacket(_data.Values.ToList(), player);
            };

            void LoadRockData()
            {
                try
                {
                    var dataLoc = new AssetLocation($"{Mod.Info.ModID}:config/rockdata.json");
                    foreach (RockData rockData in api.Assets.Get<List<RockData>>(dataLoc))
                    {
                        _data.Add(rockData.Rock, rockData);
                    }
                }
                catch (Exception e)
                {
                    Mod.Logger.Error("Rock data not loaded, error: {0}", e.Message);
                }

                ResolveWildcards();
                CheckAllExist();
            }

            void ResolveWildcards()
            {
                List<RockData> resolvedWildcards = new();
                List<AssetLocation> toRemove = new();
                foreach (RockData rockData in _data.Values)
                {
                    AssetLocation rockCode = rockData.Rock;
                    if (rockCode.IsWildCard)
                    {
                        foreach (Block block in api.World.Blocks)
                        {
                            if (block.Code != null)
                            {
                                if (WildcardUtil.Match(rockCode, block.Code))
                                {
                                    string wildcardValue = WildcardUtil
                                        .GetWildcardValue(rockCode, block.Code);

                                    RockData resolved = (RockData)rockData.Clone();
                                    resolved.SetWildcardValue(wildcardValue, api);
                                    resolvedWildcards.Add(resolved);
                                }
                            }
                        }
                        toRemove.Add(rockData.Rock);
                    }
                }

                foreach (RockData rockData in resolvedWildcards)
                {
                    _data.Add(rockData.Rock, rockData);
                }

                foreach (AssetLocation code in toRemove)
                {
                    _data.Remove(code);
                }
            }

            void CheckAllExist()
            {
                List<RockData> unknownRocks = new();
                foreach (RockData rockData in _data.Values)
                {
                    foreach (AssetLocation? code in rockData)
                    {
                        if (code == null)
                        {
                            continue;
                        }

                        bool found = false;
                        foreach (Block block in api.World.Blocks)
                        {
                            if (code.Equals(block.Code))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            foreach (Item item in api.World.Items)
                            {
                                if (code.Equals(item.Code))
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (!found)
                        {
                            string? type = rockData[code];
                            if (type != null)
                            {
                                if (type == "rock")
                                {
                                    Mod.Logger.Warning("Unknown rock {0}", rockData.Rock);
                                    unknownRocks.Add(rockData);
                                    continue;
                                }
                                else
                                {
                                    Mod.Logger.Warning("Unknown {0} code {1} in rock {2}", type, code, rockData.Rock);
                                    rockData[type] = null;
                                }
                            }
                        }
                    }
                }
            }
        }

        public AssetLocation? GetValue(AssetLocation rock, string type)
        {
            RockData? data = GetValue(rock);
            if (data != null)
            {
                return data[type];
            }
            return null;
        }

        public RockData? GetValue(AssetLocation rock)
        {
            if (_data.TryGetValue(rock, out RockData? value))
            {
                return value;
            }
            return null;
        }

        public bool IsSuitableRock(AssetLocation rock)
        {
            return IsSuitable(rock, "rock");
        }

        public bool IsSuitable(AssetLocation code, string? type = null)
        {
            foreach (RockData rockData in _data.Values)
            {
                if (type != null)
                {
                    if (code.Equals(rockData[type]))
                    {
                        return true;
                    }

                    // Collect cracked rock as rock
                    if (type == "rock" && code.Equals(rockData["crackedrock"]))
                    {
                        return true;
                    }
                }
                else
                {
                    foreach (AssetLocation? value in rockData)
                    {
                        if (code.Equals(value))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public string? GetRockType(AssetLocation code)
        {
            foreach (RockData rockData in _data.Values)
            {
                string? type = rockData[code];
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public bool TryResolveCode(AssetLocation code,
            [NotNullWhen(true)] out string? type,
            [NotNullWhen(true)] out AssetLocation? rock)
        {
            foreach (RockData rockData in _data.Values)
            {
                type = rockData[code];
                if (type != null)
                {
                    rock = rockData.Rock;
                    return true;
                }
            }

            type = null;
            rock = null;
            return false;
        }
    }
}
