using CommonLib.Config;

namespace StoneQuarry
{
    [Config("stonequarry.json")]
    public class Config
    {
        [Description("Max total amount of sand, gravel and stones that can be stored in one rubble storage")]
        [Range(1, int.MaxValue)]
        public int RubbleStorageMaxSize { get; set; } = 512;

        [Description("Time of interaction with slabs to obtain stone in seconds")]
        [Range(0f, 10f)]
        public float SlabInteractionTime { get; set; } = 0.2f;

        [Description("Modifier of the difficulty (hits number) of hammering plugs")]
        [Range(0f, 100f)]
        public float PlugWorkModifier { get; set; } = 1;

        [Description("Modifier of plugs size (range)")]
        [Range(0f, 100f)]
        public float PlugSizeModifier { get; set; } = 1;

        [Description($"Chance for break plug after use. " +
            $"Will be ignored if {nameof(PlugDurability)} is not 0")]
        [Range(0f, 1f)]
        public float BreakPlugChance { get; set; } = 0;

        [Description($"How many times can the plug be used before it breaks. " +
            $"Plugs with different durability do not stack. " +
            $"Ignores {nameof(BreakPlugChance)} if not 0")]
        [Range(0, int.MaxValue)]
        public int PlugDurability { get; set; } = 0;
    }
}
