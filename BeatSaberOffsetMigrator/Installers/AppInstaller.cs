using System;
using BeatSaber.Init;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.EO;
using BeatSaberOffsetMigrator.InputHelper;
using BeatSaberOffsetMigrator.Utils;
using BGLib.DotnetExtension.CommandLine;
using BGLib.Polyglot;
using UnityEngine.XR.OpenXR;
using Zenject;
using Object = UnityEngine.Object;

namespace BeatSaberOffsetMigrator.Installers
{
    internal class AppInstaller : Installer
    {
        private const string OpenVRLibId = "OpenVR";
        
        private readonly PluginConfig _config;
        
        public const string IsOVRBindingKey = "IsOVR";
        public const string IsFPFCBindingKey = "IsFpfc";

        public AppInstaller(PluginConfig config)
        {
            _config = config;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(_config);
            
            Plugin.Log.Notice("Current OpenXR runtime: " + OpenXRRuntime.name);
            
            // var arguments = Container.Resolve<CommandLineParserResult>(); // can't resolve this here
            var isFpfc = Object.FindObjectOfType<BSAppInit>()!.commandLineArguments.Contains(BSAppInit.kFPFCOption);
            
            Plugin.Log.Debug("Is fpfc: " + isFpfc);
            
            var isOvr = false;

            if (isFpfc)
            {
                Container.BindInterfacesTo<UnsupportedVRInputHelper>().AsSingle().OnInstantiated<UnsupportedVRInputHelper>((_, instance )=>
                {
                    instance.ReasonIfNotWorking = Localization.Get("BSOM_ERR_FPFC");
                });
            }
            else if (OpenXRRuntime.name.IndexOf("steamvr", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (ModUtils.IsModInstalled(OpenVRLibId))
                {
                    Container.BindInterfacesTo<OpenVRInputHelper>().AsSingle();
                }
                else
                {
                    Container.BindInterfacesTo<UnsupportedVRInputHelper>().AsSingle().OnInstantiated<UnsupportedVRInputHelper>((_, instance )=>
                    {
                        instance.ReasonIfNotWorking = Localization.Get("BSOM_ERR_OPENVR_NOT_INSTALLED");
                    });
                }
            }
            else if (OpenXRRuntime.name.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                isOvr = true;
                Container.BindInterfacesTo<OculusVRInputHelper>().AsSingle();
            }
            else
            {
                Container.BindInterfacesTo<UnsupportedVRInputHelper>().AsSingle().OnInstantiated<UnsupportedVRInputHelper>((ctx, instance) =>
                {
                    instance.ReasonIfNotWorking = Localization.Get("BSOM_ERR_UNSUPPORTED_RUNTIME");
                });
            }
            
            Container.BindInstance(isOvr).WithId(IsOVRBindingKey);
            Container.BindInterfacesAndSelfTo<EasyOffsetManager>().AsSingle();
            
            Container.BindInstance(isFpfc).WithId(IsFPFCBindingKey);
        }
    }
}