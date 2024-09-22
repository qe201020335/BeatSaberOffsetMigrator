using Zenject;

namespace BeatSaberOffsetMigrator.Installers
{
    internal class MenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<OffsetHelper>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("OffsetHelper")
                .AsSingle().NonLazy();
        }
    }
}