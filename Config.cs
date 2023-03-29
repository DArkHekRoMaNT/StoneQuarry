using ProtoBuf;

namespace StoneQuarry
{
    [ProtoContract]
    public class Config
    {
        [ProtoMember(1)] public PlugSizes PlugSizes = new();
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
}
