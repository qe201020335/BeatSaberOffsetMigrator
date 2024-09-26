using BeatSaberOffsetMigrator.Configuration;
using HarmonyLib;
using SiraUtil.Affinity;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace BeatSaberOffsetMigrator.Patches;

//Overwrite other offset mods using this target method
public class VRControllerPatch: IAffinity
{
    [Inject]
    private readonly OffsetHelper _offsetHelper = null!;
    
    [AffinityPostfix]
    [AffinityPatch(typeof(VRController), nameof(VRController.Update))]
    private void Postfix(VRController __instance)
    {
        if (!PluginConfig.Instance.ApplyOffset || !_offsetHelper.IsSupported) return;
        
        var node = __instance.node;
        
        Pose offset;
        Pose controllerPose;
        switch (node)
        {
            case XRNode.LeftHand:
                offset = new Pose(PluginConfig.Instance.LeftOffsetPosition, PluginConfig.Instance.LeftOffsetRotation);
                controllerPose = _offsetHelper.LeftRuntimePose;
                break;
            case XRNode.RightHand:
                offset = new Pose(PluginConfig.Instance.RightOffsetPosition, PluginConfig.Instance.RightOffsetRotation);
                controllerPose = _offsetHelper.RightRuntimePose;
                break;
            default:
                return;
        }
        
        var transform = __instance.transform;
        transform.rotation = controllerPose.rotation;
        transform.position = controllerPose.position;
        transform.Translate(offset.position);
        transform.Rotate(offset.rotation.eulerAngles);
    }
}