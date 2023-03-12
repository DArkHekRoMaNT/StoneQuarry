using ProtoBuf;

namespace StoneQuarry
{
    [ProtoContract]
    public class Config
    {
        [ProtoMember(1)] public PlugSizes PlugSizes = new PlugSizes();
        [ProtoMember(2)] public PlugSizesMoreMetals PlugSizesMoreMetals = new PlugSizesMoreMetals();
        [ProtoMember(3)] public int RubbleStorageMaxSize = 512;
        [ProtoMember(4)] public float SlabInteractionTime = 0.2f;
        [ProtoMember(5)] public float PlugWorkModifier = 1;
        [ProtoMember(6)] public float BreakPlugChance = 0;
    }

    [ProtoContract]
    public class PlugSizes
    {
        [ProtoMember(1)] public int Copper = 3;
        [ProtoMember(2)] public int TinBronze = 4;
        [ProtoMember(3)] public int BismuthBronze = 4;
        [ProtoMember(4)] public int BlackBronze = 4;
        [ProtoMember(5)] public int Iron = 5;
        [ProtoMember(6)] public int MeteoricIron = 5;
        [ProtoMember(7)] public int Steel = 6;
    }

    [ProtoContract]
    public class PlugSizesMoreMetals
    {
        //[ProtoMember(1)] public int Copper = 3;
        [ProtoMember(2)] public int Nickel = 3;
        [ProtoMember(3)] public int Monel = 4;
        [ProtoMember(4)] public int Constantan = 4;
        //[ProtoMember(5)] public int TinBronze = 4;
        //[ProtoMember(6)] public int BismuthBronze = 4;
        //[ProtoMember(7)] public int BlackBronze = 4;
        [ProtoMember(8)] public int Cupronickel = 5;
        [ProtoMember(9)] public int PhosphorBronze = 5;
        [ProtoMember(10)] public int Nichrome = 5;
        //[ProtoMember(11)] public int Iron = 5;
        [ProtoMember(12)] public int Chromium = 5;
        //[ProtoMember(13)] public int MeteoricIron = 5;
        //[ProtoMember(14)] public int Steel = 6;
        [ProtoMember(15)] public int Zamak = 6;
        [ProtoMember(16)] public int Ferromagnesium = 6;
        [ProtoMember(17)] public int Alnico = 6;
        [ProtoMember(18)] public int Titanium = 6;
        [ProtoMember(19)] public int TitaniumGold = 6;
        [ProtoMember(20)] public int BlueSteel = 6;
        [ProtoMember(21)] public int Kovar = 5;
        [ProtoMember(22)] public int Invar = 6;
        [ProtoMember(23)] public int Talonite = 6;
        [ProtoMember(24)] public int StainlessSteel = 7;
        [ProtoMember(25)] public int Chromoly = 7;
        [ProtoMember(26)] public int Damascus = 7;
        [ProtoMember(27)] public int ToolSteel = 7;
    }
}
