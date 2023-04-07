using CommonLib.Utils;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections;
using Vintagestory.API.Common;

namespace StoneQuarry
{
    [JsonObject]
    [ProtoContract]
    public class RockData : IEnumerable, ICloneable, IComparable<RockData>
    {
        public static AssetLocation UndefinedRock => new("");
        public static readonly string[] types =
        {
            "rock",
            "stone",
            "rockpolished",
            "stonebrick",
            "gravel",
            "sand"
        };

        [ProtoMember(2)] private AssetLocation? _stone;
        [ProtoMember(3)] private AssetLocation? _rockPolished;
        [ProtoMember(4)] private AssetLocation? _stoneBrick;
        [ProtoMember(5)] private AssetLocation? _gravel;
        [ProtoMember(6)] private AssetLocation? _sand;

        [ProtoMember(1)] public AssetLocation Rock { get; set; }

        public RockData()
        {
            Rock = UndefinedRock;
        }

        public AssetLocation? this[string type]
        {
            get => type switch
            {
                "rock" => Rock,
                "stone" => _stone,
                "rockpolished" => _rockPolished,
                "stonebrick" => _stoneBrick,
                "gravel" => _gravel,
                "sand" => _sand,
                _ => null
            };

            set
            {
                switch (type)
                {
                    case "rock": Rock = value ?? UndefinedRock; break;
                    case "stone": _stone = value; break;
                    case "rockpolished": _rockPolished = value; break;
                    case "stonebrick": _stoneBrick = value; break;
                    case "gravel": _gravel = value; break;
                    case "sand": _sand = value; break;
                }
            }
        }

        public string? this[AssetLocation code]
        {
            get
            {
                if (code.Equals(Rock)) return "rock";
                if (code.Equals(_stone)) return "stone";
                if (code.Equals(_rockPolished)) return "rockpolished";
                if (code.Equals(_stoneBrick)) return "stonebrick";
                if (code.Equals(_gravel)) return "gravel";
                if (code.Equals(_sand)) return "sand";

                return null;
            }
        }

        public void SetWildcardValue(string wildcardValue, ICoreAPI api)
        {
            for (int i = 0; i < types.Length; i++)
            {
                string type = types[i];
                AssetLocation? code = this[type];
                if (code != null)
                {
                    code.Path = code.Path.Replace("*", wildcardValue);
                    if (api.World.GetCollectibleObject(code) == null)
                    {
                        this[type] = null;
                    }
                }
            }
        }

        public object Clone()
        {
            return new RockData()
            {
                Rock = Rock.Clone(),
                _stone = _stone?.Clone(),
                _rockPolished = _rockPolished?.Clone(),
                _stoneBrick = _stoneBrick?.Clone(),
                _gravel = _gravel?.Clone(),
                _sand = _sand?.Clone(),
            };
        }

        public IEnumerator GetEnumerator()
        {
            foreach (string type in types)
            {
                yield return this[type];
            }
        }

        public int CompareTo(RockData other)
        {
            return Rock.CompareTo(other.Rock);
        }
    }
}
