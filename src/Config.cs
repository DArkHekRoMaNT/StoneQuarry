namespace StoneQuarry
{
    public class Config
    {
        public PlugSizes PlugSizes = new PlugSizes();
        public int RubbleStorageMaxSize = 512;
        public float SlabInteractionTime = 0.2f;
        public float PlugWorkModifier = 1;
        public float BreakPlugChance = 0;
    }

    public class PlugSizes
    {
        public int Copper = 3;
        public int TinBronze = 4;
        public int BismuthBronze = 4;
        public int BlackBronze = 4;
        public int Iron = 5;
        public int MeteoricIron = 5;
        public int Steel = 6;
    }
}
