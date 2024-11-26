using System.Collections.Generic;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.EO;
using BeatSaberOffsetMigrator.Utils;
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
    private readonly PluginConfig _config = null!;
    
    [Inject]
    private readonly OffsetHelper _offsetHelper = null!;

    [Inject]
    private readonly EasyOffsetManager _easyOffsetManager = null!;

    internal bool UseGeneratedOffset { get; set; } = false;

    private Dictionary<XRNode, bool> _wasApplying = new Dictionary<XRNode, bool>(2);

    [AffinityPostfix]
    [AffinityPatch(typeof(VRController), nameof(VRController.Update))]
    private void Postfix(VRController __instance)
    {
        if (!_offsetHelper.IsRuntimeSupported)
        {
            // Don't do anything if the VR system is not supported
            return;
        }
        
        var viewTransform = __instance.viewAnchorTransform;
        var xrnode = __instance.node;
        if (PluginConfig.Instance.ApplyOffset)
        {
            _wasApplying[xrnode] = true;
            viewTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (UseGeneratedOffset)
            {
                ApplyGeneratedOffset(__instance, xrnode);
            }
            else if (_offsetHelper.IsRuntimePoseValid)
            {
                ApplyOffset(__instance.transform, xrnode);
            }
        }
        else 
        {
            if (_wasApplying.TryGetValue(xrnode, out var previous) && previous)
            {
                _wasApplying[xrnode] = false;
                //Reset the offset
                __instance.UpdateAnchorOffsetPose();
            }
        }
        
        var pose = new Pose(viewTransform.position, viewTransform.rotation);
        switch (xrnode)
        {
            case XRNode.LeftHand:
                _offsetHelper.LeftGamePose = pose;
                break;
            case XRNode.RightHand:
                _offsetHelper.RightGamePose = pose;
                break;
        }
    }
    
    private void ApplyOffset(Transform transform, XRNode node)
    {
        Pose offset;
        Pose controllerPose;
        switch (node)
        {
            case XRNode.LeftHand:
                offset = _config.LeftOffset;
                controllerPose = _offsetHelper.LeftRuntimePose;
                break;
            case XRNode.RightHand:
                offset = _config.RightOffset;
                controllerPose = _offsetHelper.RightRuntimePose;
                break;
            default:
                return;
        }
        
        transform.SetLocalPositionAndRotation(controllerPose.position, controllerPose.rotation);
        transform.Offset(offset);
    }
    
    private void ApplyGeneratedOffset(VRController vrController, XRNode node)
    {
        if (vrController._vrPlatformHelper.GetNodePose(node, vrController.nodeIdx, out var pos, out var rot))
        {
            var transform = vrController.transform;
            transform.SetLocalPositionAndRotation(pos, rot);
            _offsetHelper.RevertUnityOffset(transform, node);
            _easyOffsetManager.ApplyOffset(transform, node);
        }
    }
}