using CommonLib.Config;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace StoneQuarry
{
    public class Core : ModSystem
    {
        public static string ModId => "stonequarry";
        public PlugPreviewManager? PlugPreviewManager { get; private set; }

        public override void StartPre(ICoreAPI api)
        {
            var configs = api.ModLoader.GetModSystem<ConfigManager>();
            var config = configs.GetConfig<Config>();
            var plugSizes = typeof(PlugSizes).GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (FieldInfo plugSize in plugSizes)
            {
                int value = (int)plugSize.GetValue(null);
                value = (int)(value * config.PlugSizeModifier);
                api.World.Config.SetInt($"SQ_PlugSizes_{plugSize.Name}", value);
            }
            api.World.Config.SetInt($"SQ_RubbleStorageMaxSize", config.RubbleStorageMaxSize);

            if (api is ICoreClientAPI capi)
            {
                PlugPreviewManager = new PlugPreviewManager(capi);
            }
        }

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockStoneSlab", typeof(BlockStoneSlab));
            api.RegisterBlockEntityClass("StoneSlab", typeof(BEStoneSlab));

            api.RegisterBlockClass("BlockRubbleStorage", typeof(BlockRubbleStorage));
            api.RegisterBlockEntityClass("RubbleStorage", typeof(BERubbleStorage));

            api.RegisterBlockClass("BlockPlugAndFeather", typeof(BlockPlugAndFeather));
            api.RegisterBlockEntityClass("PlugAndFeather", typeof(BEPlugAndFeather));

            api.RegisterItemClass("ItemRubbleHammer", typeof(ItemRubbleHammer));
        }
    }
}
