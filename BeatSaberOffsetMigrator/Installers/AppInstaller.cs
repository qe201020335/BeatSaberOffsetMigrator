using BeatSaberOffsetMigrator.Configuration;
using Valve.VR;
using Zenject;

namespace BeatSaberOffsetMigrator.Installers
{
    internal class AppInstaller : Installer
    {
        private readonly PluginConfig _config;

        public AppInstaller(PluginConfig config)
        {
            _config = config;
        }

        public override void InstallBindings()
        {
            OpenVRHelper.Initialize();
            
            Container.BindInstance(_config);
            Container.BindInterfacesAndSelfTo<OpenVRInputHelper>().AsSingle();
        }
    }
}