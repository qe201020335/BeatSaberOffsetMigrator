using BeatSaberOffsetMigrator.EO;
using BeatSaberOffsetMigrator.Patches;
using BeatSaberOffsetMigrator.UI;
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
            
            Container.BindInterfacesAndSelfTo<MenuViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<MenuFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
            Container.BindInterfacesTo<MenuButtonManager>().AsSingle();
            Container.BindInterfacesTo<VRControllerPatch>().AsSingle();
            Container.BindInterfacesAndSelfTo<EasyOffsetExporter>().AsSingle();
        }
    }
}