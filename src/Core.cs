using Vintagestory.API.Common;

namespace QuarryWorks
{
    public class Core : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("chunk", typeof(ChunksItem));
            api.RegisterItemClass("slabsetter", typeof(BlockStackSetterItem));


            api.RegisterBlockClass("RubbleStorage", typeof(RubbleStorageBlock));
            api.RegisterBlockEntityClass("RubbleStorageBE", typeof(RubbleStorageBE));

            api.RegisterBlockClass("RoughStoneStorage", typeof(RoughCutStorageBlock));
            api.RegisterBlockEntityClass("StoneStorageCoreBE", typeof(RoughCutStorageBE));
            api.RegisterBlockEntityClass("StoneStorageCapBE", typeof(GenericStorageCapBE));


            api.RegisterBlockClass("PlugFeatherBlock", typeof(PlugnFeatherBlock));
            api.RegisterBlockEntityClass("PlugFeatherBE", typeof(PlugnFeatherBE));
        }
    }
}
