using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace StoneQuarry
{
    public class Core : ModSystem
    {
        public static Config Config { get; private set; }

        ICoreAPI api;

        public override void StartPre(ICoreAPI api)
        {
            this.api = api;

            if (api is ICoreServerAPI sapi)
            {
                LoadConfig();
                SetConfigForPatches();

                sapi.Network.RegisterChannel(Mod.Info.ModID)
                    .RegisterMessageType<Config>();

                sapi.Event.PlayerJoin += (player) =>
                {
                    var channel = api.Network.GetChannel(Mod.Info.ModID) as IServerNetworkChannel;
                    channel.SendPacket(Config, player);
                };
            }

            if (api is ICoreClientAPI capi)
            {
                capi.Network.RegisterChannel(Mod.Info.ModID)
                    .RegisterMessageType<Config>()
                    .SetMessageHandler<Config>((config) => { Config = config; });

            }
        }

        public override void Start(ICoreAPI api) => ClassRegister();

        private void SetConfigForPatches()
        {
            foreach (var field in typeof(PlugSizes).GetFields())
            {
                int value = (int)field.GetValue(Config.PlugSizes);
                api.World.Config.SetInt($"SQ_PlugSizes_{field.Name}", value);
            }

            foreach (var field in typeof(PlugSizesMoreMetals).GetFields())
            {
                int value = (int)field.GetValue(Config.PlugSizesMoreMetals);
                api.World.Config.SetInt($"SQ_PlugSizesMoreMetals_{field.Name}", value);
            }

            api.World.Config.SetInt($"SQ_RubbleStorageMaxSize", Config.RubbleStorageMaxSize);
        }

        private void LoadConfig()
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

        private void ClassRegister()
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


            // Legacy
            api.RegisterBlockEntityClass("RubbleStorageBE", typeof(BERubbleStorage));
            api.RegisterBlockEntityClass("StoneStorageCoreBE", typeof(BERoughCutStorage));
            api.RegisterBlockEntityClass("StoneStorageCapBE", typeof(BEGenericMultiblockPart));
            api.RegisterBlockEntityClass("PlugFeatherBE", typeof(BEPlugAndFeather));
        }

    }
}
