using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.Installers;
using HarmonyLib;
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
        
        private readonly Harmony _harmony = new Harmony("com.github.qe201020335.BeatSaberOffsetMigrator");

        [Init]
        public void Init(Zenjector zenjector, IPALogger logger, Config config)
        {
            Instance = this;
            Log = logger;

            PluginConfig.Instance = config.Generated<PluginConfig>();
            
            // always make it false at game start so a broken config won't make saber inaccessible
            PluginConfig.Instance.ApplyOffset = false;

            zenjector.UseLogger(logger);
            zenjector.UseMetadataBinder<Plugin>();
            zenjector.Install<AppInstaller>(Location.App, PluginConfig.Instance);
            zenjector.Install<MenuInstaller>(Location.Menu);
            
            _harmony.PatchAll();
            
            Log.Debug("BeatSaberOffsetMigrator initialized.");
        }
    }
}