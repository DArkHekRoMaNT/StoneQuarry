using System;
using System.IO;
using Vintagestory.API.Common;

namespace StoneQuarry
{
    public class Core : ModSystem
    {
        public static Config Config { get; private set; }

        public override void StartPre(ICoreAPI api)
        {
            LoadConfig(api);
            SetConfigForPatches(api);
        }
        public override void Start(ICoreAPI api)
        {
            ClassRegister(api);
        }

        private void SetConfigForPatches(ICoreAPI api)
        {
            foreach (var field in typeof(PlugSizes).GetFields())
            {
                int value = (int)field.GetValue(Config.PlugSizes);
                api.World.Config.SetInt($"SQ_PlugSizes_{field.Name}", value);
            }

            api.World.Config.SetInt($"SQ_RubbleStorageMaxSize", Config.RubbleStorageMaxSize);
        }

        private void LoadConfig(ICoreAPI api)
        {
            string configFilename = Mod.Info.ModID + ".json";
            if (!File.Exists(api.GetOrCreateDataPath("ModConfig") + "/" + configFilename))
            {
                Config = new Config();
            }
            else
            {
                try
                {
                    Config = api.LoadModConfig<Config>(configFilename);
                }
                catch (Exception e)
                {
                    api.Logger.Error($"[{Mod.Info.ModID}] Config file cannot be loaded, a new one will be created. Error message: {e.Message}");
                    Config = new Config();
                }
            }
            api.StoreModConfig(Config, configFilename);
        }

        private void ClassRegister(ICoreAPI api)
        {
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
