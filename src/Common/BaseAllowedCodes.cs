using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace StoneQuarry
{
    public class BaseAllowedCodes
    {
        private readonly Dictionary<string, Dictionary<string, string>> _codesByRockAndType;

        public IReadOnlyDictionary<string, string> this[string rock]
        {
            get
            {
                if (_codesByRockAndType.ContainsKey(rock))
                {
                    return _codesByRockAndType[rock];
                }

                return new Dictionary<string, string>();
            }
        }

        public string this[string rock, string type]
        {
            get
            {
                var codesByType = this[rock];
                if (codesByType != null && codesByType.ContainsKey(type))
                {
                    return codesByType[type];
                }

                return "";
            }
        }

        public IReadOnlyCollection<string> Rocks => _codesByRockAndType.Keys;

        public IReadOnlyCollection<string> Types
        {
            get
            {
                var types = new List<string>();

                foreach (var rock in Rocks)
                {
                    foreach (var type in this[rock].Keys)
                    {
                        if (!types.Contains(type))
                        {
                            types.Add(type);
                        }
                    }
                }

                return types;
            }
        }

        public BaseAllowedCodes()
        {
            _codesByRockAndType = new Dictionary<string, Dictionary<string, string>>();
        }

        public void FromJson(string json)
        {
            var list = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);

            _codesByRockAndType.Clear();
            foreach (var codesByType in list)
            {
                if (codesByType.TryGetValue("rock", out string rock))
                {
                    _codesByRockAndType.Add(rock, codesByType);
                }
                else
                {
                    var errorObject = JsonConvert.SerializeObject(codesByType, Formatting.None);
                    Core.ModLogger.Error("Allowed codes require the \"rock\" property, {0} will be ignored", errorObject);
                }
            }
        }

        public bool HasCode(string rock, string code)
        {
            var codesByType = this[rock];
            return codesByType.Values.Contains(code);
        }

        public bool TryResolveCode(string code, out string type, out string rock)
        {
            foreach (var rockName in Rocks)
            {
                var codesByType = this[rockName];
                foreach (var typeName in codesByType.Keys)
                {
                    if (codesByType[typeName] == code)
                    {
                        type = typeName;
                        rock = rockName;
                        return true;
                    }
                }
            }

            rock = null;
            type = null;
            return false;
        }
    }
}