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
            $"Will be ignored if {nameof(EnablePlugDurability)} is true")]
        [Range(0f, 1f)]
        public float BreakPlugChance { get; set; } = 0;

        [Description($"Enables durability for plugs. " +
            $"Plugs with different durability do not stack. " +
            $"Ignores {nameof(BreakPlugChance)} if true")]
        public bool EnablePlugDurability { get; set; } = false;

        [Description("0 - unlimited")]
        [Range(0, int.MaxValue)]
        public int CopperPlugDurability { get; set; } = 5;

        [Description("0 - unlimited. Any bronze")]
        [Range(0, int.MaxValue)]
        public int BronzePlugDurability { get; set; } = 10;

        [Description("0 - unlimited. Iron, meteoric iron")]
        [Range(0, int.MaxValue)]
        public int IronPlugDurability { get; set; } = 15;

        [Description("0 - unlimited")]
        [Range(0, int.MaxValue)]
        public int SteelPlugDurability { get; set; } = 20;

        [Description($"Allowed slab containers: " +
            $"1 - general (chest, hotbar, common backpack content slots), 2 - backpack slots, 4 - mining bag content slots, 256 - offhand. " +
            $"You can combine it, for example 5 (1+4) is general or mining bag")]
        public int SlabStorageFlags { get; set; } = 5;

        [Description($"Allowed rubble storage containers: " +
            $"1 - general (chest, hotbar, common backpack content slots), 2 - backpack slots, 4 - mining bag content slots, 256 - offhand. " +
            $"You can combine it, for example 5 (1+4) is general or mining bag")]
        public int RubbleStorageStorageFlags { get; set; } = 5;
    }
}
