using System;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.InputHelper;
using UnityEngine.XR.OpenXR;
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
            
            Plugin.Log.Notice("Current OpenXR runtime: " + OpenXRRuntime.name);
            
            if (OpenXRRuntime.name.IndexOf("steamvr", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Container.BindInterfacesTo<OpenVRInputHelper>().AsSingle();
            }
            else if (OpenXRRuntime.name.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Container.BindInterfacesTo<OculusVRInputHelper>().AsSingle();
            }
            else
            {
                Container.BindInterfacesTo<UnsupportedVRInputHelper>().AsSingle();
            }
        }
    }
}