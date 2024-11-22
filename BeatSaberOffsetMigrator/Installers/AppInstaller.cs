using System;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.InputHelper;
using BeatSaberOffsetMigrator.Utils;
using UnityEngine.XR.OpenXR;
using Zenject;

namespace BeatSaberOffsetMigrator.Installers
{
    internal class AppInstaller : Installer
    {
        private const string OpenVRLibId = "OpenVR";
        
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
                if (ModUtils.IsModInstalled(OpenVRLibId))
                {
                    Container.BindInterfacesTo<OpenVRInputHelper>().AsSingle();
                }
                else
                {
                    Container.BindInterfacesTo<UnsupportedVRInputHelper>().AsSingle().OnInstantiated<UnsupportedVRInputHelper>((_, instance )=>
                    {
                        instance.ReasonIfNotWorking = "OpenVR API Library not installed";
                    });
                }
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