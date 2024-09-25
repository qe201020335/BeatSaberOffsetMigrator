using System;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.InputHelper;
using UnityEngine.XR;
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
            
            if (XRSettings.loadedDeviceName.IndexOf("openvr", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Container.BindInterfacesTo<OpenVRInputHelper>().AsSingle();
            }
            // TODO: Implement OculusVR support
            // else if (XRSettings.loadedDeviceName.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0)
            // {
            //     
            // }
            else
            {
                Container.BindInterfacesTo<UnsupportedVRInputHelper>().AsSingle();
            }
        }
    }
}