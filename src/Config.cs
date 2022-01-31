namespace StoneQuarry
{
    public class Config
    {
        public PlugSizes PlugSizes = new PlugSizes();
        public PlugSizesMoreMetals PlugSizesMoreMetals = new PlugSizesMoreMetals();
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

    public class PlugSizesMoreMetals
    {
        //public int Copper = 3;
        public int Nickel = 3;
        public int Monel = 4;
        public int Constantan = 4;
        //public int TinBronze = 4;
        //public int BismuthBronze = 4;
        //public int BlackBronze = 4;
        public int Cupronickel = 5;
        public int PhosphorBronze = 5;
        public int Nichrome = 5;
        //public int Iron = 5;
        public int Chromium = 5;
        //public int MeteoricIron = 5;
        //public int Steel = 6;
        public int Zamak = 6;
        public int Ferromagnesium = 6;
        public int Alnico = 6;
        public int Titanium = 6;
        public int TitaniumGold = 6;
        public int BlueSteel = 6;
        public int Kovar = 5;
        public int Invar = 6;
        public int Talonite = 6;
        public int StainlessSteel = 7;
        public int Chromoly = 7;
        public int Damascus = 7;
        public int ToolSteel = 7;
    }
}
