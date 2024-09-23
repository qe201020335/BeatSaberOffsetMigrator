using BeatSaberOffsetMigrator.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberOffsetMigrator.Patches;

//Overwrite other offset mods using this target method
[HarmonyPatch(typeof(VRController), nameof(VRController.Update))]
public class VRControllerPatch
{
    [HarmonyPostfix]
    private static void Postfix(VRController __instance)
    {
        if (!PluginConfig.Instance.ApplyOffset) return;
        Pose offset;
        switch (__instance._node)
        {
            case XRNode.LeftHand:
                offset = new Pose(PluginConfig.Instance.LeftOffsetPosition, PluginConfig.Instance.LeftOffsetRotation);
                break;
            case XRNode.RightHand:
                offset = new Pose(PluginConfig.Instance.RightOffsetPosition, PluginConfig.Instance.RightOffsetRotation);
                break;
            default:
                return;
        }
        
        var transform = __instance.transform;
        ApplyOffset(transform, offset);
    }
    
    private static void ApplyOffset(Transform transform, Pose offset)
    {
        transform.position += offset.position;
        transform.rotation *= offset.rotation;
    }
}