using BeatSaberOffsetMigrator.Configuration;
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
            Container.BindInstance(_config);
            Container.BindInterfacesAndSelfTo<OpenVRInputHelper>().AsSingle();
        }
    }
}