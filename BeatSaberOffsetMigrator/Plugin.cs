using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.Installers;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace BeatSaberOffsetMigrator
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    [NoEnableDisable]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        [Init]
        public void Init(Zenjector zenjector, IPALogger logger, Config config)
        {
            Instance = this;
            Log = logger;

            PluginConfig.Instance = config.Generated<PluginConfig>();

            zenjector.UseLogger(logger);
            zenjector.UseMetadataBinder<Plugin>();
            zenjector.Install<AppInstaller>(Location.App, PluginConfig.Instance);
            zenjector.Install<MenuInstaller>(Location.Menu);
            
            Log.Debug("BeatSaberOffsetMigrator initialized.");
        }
    }
}