using CommonLib.Config;

namespace StoneQuarry
{
    [Config("stonequarry.json")]
    public class Config
    {
        public int RubbleStorageMaxSize { get; set; } = 512;
        public float SlabInteractionTime { get; set; } = 0.2f;
        public float PlugWorkModifier { get; set; } = 1;
        public float PlugSizeModifier { get; set; } = 1;
        public float BreakPlugChance { get; set; } = 0;
    }
}
