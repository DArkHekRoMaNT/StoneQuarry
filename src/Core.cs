using Vintagestory.API.Common;

namespace StoneQuarry
{
    public class Core : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("ItemSlabTool", typeof(ItemSlabTool));
            api.RegisterItemClass("ItemSlabContentSetter", typeof(ItemSlabContentSetter));

            api.RegisterBlockClass("BlockGenericMultiblockPart", typeof(BlockGenericMultiblockPart));
            api.RegisterBlockClass("BlockRubbleStorage", typeof(BlockRubbleStorage));
            api.RegisterBlockClass("BlockRoughCutStorage", typeof(BlockRoughCutStorage));
            api.RegisterBlockClass("BlockPlugAndFeather", typeof(BlockPlugAndFeather));

            api.RegisterBlockEntityClass("GenericMultiblockPart", typeof(BEGenericMultiblockPart));
            api.RegisterBlockEntityClass("RubbleStorage", typeof(BERubbleStorage));
            api.RegisterBlockEntityClass("RoughCutStorage", typeof(BERoughCutStorage));
            api.RegisterBlockEntityClass("PlugAndFeather", typeof(BEPlugAndFeather));


            //legacy
            api.RegisterBlockEntityClass("RubbleStorageBE", typeof(BERubbleStorage));
            api.RegisterBlockEntityClass("StoneStorageCoreBE", typeof(BERoughCutStorage));
            api.RegisterBlockEntityClass("StoneStorageCapBE", typeof(BEGenericMultiblockPart));
            api.RegisterBlockEntityClass("PlugFeatherBE", typeof(BEPlugAndFeather));
        }
    }
}
