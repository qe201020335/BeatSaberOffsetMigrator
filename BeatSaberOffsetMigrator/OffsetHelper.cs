using System.Collections;
using System.Collections.Generic;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.InputHelper;
using BeatSaberOffsetMigrator.Utils;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using SiraUtil.Services;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace BeatSaberOffsetMigrator;

public class OffsetHelper
{
    private SiraLog _logger;
    
    private PluginConfig _config;

    private VRController _leftController = null!;

    private VRController _rightController = null!;

    private IVRInputHelper _vrInputHelper = null!;
    
    private IVRPlatformHelper _vrPlatformHelper = null!;
    
    internal Pose LeftGamePose { get; set; }
    
    internal Pose RightGamePose { get; set; }
    
    internal bool IsSupported => _vrInputHelper.Supported;
    
    internal bool IsWorking => _vrInputHelper.Working;

    internal Pose LeftRuntimePose => _vrInputHelper.GetLeftVRControllerPose();
    
    internal Pose RightRuntimePose => _vrInputHelper.GetRightVRControllerPose();
    
    internal Pose LeftOffset => CalculateOffset(LeftRuntimePose, LeftGamePose);
    internal Pose RightOffset => CalculateOffset(RightRuntimePose, RightGamePose);
    
    internal bool UnityOffsetSaved { get; private set; }
    internal Pose UnityOffsetL { get; private set; } = Pose.identity;
    internal Pose UnityOffsetR { get; private set; } = Pose.identity;
    private Pose UnityOffsetLReversed { get; set; } = Pose.identity;
    private Pose UnityOffsetRReversed { get; set; } = Pose.identity;


    [Inject]
    private void Init(SiraLog logger, PluginConfig config, IMenuControllerAccessor controllerAccessor, IVRInputHelper vrInputHelper, IVRPlatformHelper vrPlatformHelper)
    {
        _logger = logger;
        _config = config;
        _leftController = controllerAccessor.LeftController;
        _rightController = controllerAccessor.RightController;
        _vrInputHelper = vrInputHelper;
        _vrPlatformHelper = vrPlatformHelper;

        // TODO load device specific offset
        var leftUnityOffset = _config.LeftUnityOffset;
        var rightUnityOffset = _config.RightUnityOffset;
        UnityOffsetSaved = leftUnityOffset != Pose.identity || rightUnityOffset != Pose.identity;
        SetUnityOffset(leftUnityOffset, rightUnityOffset);
        
        _logger.Debug("OffsetHelper initialized");
    }

    private Pose CalculateOffset(Pose from, Pose to)
    {
        var invRot = Quaternion.Inverse(from.rotation);
        
        var offset = new Pose
        {
            position = invRot * (to.position - from.position),
            rotation = invRot * to.rotation
        };
        
        return offset;
    }

    private void SetUnityOffset(Pose left, Pose right)
    {
        _logger.Debug($"Using Offsets: \nL: {left.Format()}\nR: {right.Format()}");
        UnityOffsetL = left;
        UnityOffsetR = right;
        UnityOffsetLReversed = CalculateOffset(left, Pose.identity);
        UnityOffsetRReversed = CalculateOffset(right, Pose.identity);
    }
    
    internal IEnumerator SaveUnityOffset()
    {
        var count = _config.OffsetSampleCount;
        var leftPoses = new List<Pose>(count);
        var rightPoses = new List<Pose>(count);
        for (int i = 0; i < count; i++)
        {
            if (!_vrPlatformHelper.GetNodePose(XRNode.LeftHand, _leftController.nodeIdx, out var leftPos, out var leftRot) ||
                !_vrPlatformHelper.GetNodePose(XRNode.RightHand, _rightController.nodeIdx, out var rightPos, out var rightRot))
            {
                _logger.Error("Failed to get node pose");
                ResetUnityOffset();
                yield break;
            }
            
            leftPoses.Add(new Pose(leftPos, leftRot));
            rightPoses.Add(new Pose(rightPos, rightRot));
            yield return null;
        }
        

        var unityL = AveragePose(leftPoses);
        var unityR = AveragePose(rightPoses);
        
        var offsetL = CalculateOffset(LeftRuntimePose, unityL);
        var offsetR = CalculateOffset(RightRuntimePose, unityR);
        
        _config.LeftUnityOffset = offsetL;
        _config.RightUnityOffset = offsetR;
        SetUnityOffset(offsetL, offsetR);
        UnityOffsetSaved = true;
    }
    
    internal void RevertUnityOffset(Transform t, XRNode node)
    {
        if (!UnityOffsetSaved) return;
        Pose offset;
        switch (node)
        {
            case XRNode.LeftHand:
                offset = UnityOffsetLReversed;
                break;
            case XRNode.RightHand:
                offset = UnityOffsetRReversed;
                break;
            default:
                return;
        }

        t.Offset(offset);
    }
    
    internal void ResetUnityOffset()
    {
        UnityOffsetSaved = false;
        _config.LeftUnityOffset = Pose.identity;
        _config.RightUnityOffset = Pose.identity;
        UnityOffsetLReversed = Pose.identity;
        UnityOffsetRReversed = Pose.identity;
    }
    
    private Pose AveragePose(List<Pose> poses)
    {
        var count = poses.Count;

        if (count == 0) return Pose.identity;
        
        double posx = 0, posy = 0, posz = 0, rotx = 0, roty = 0, rotz = 0, rotw = 0;
        foreach (var pose in poses)
        {
            posx += pose.position.x;
            posy += pose.position.y;
            posz += pose.position.z;
            rotx += pose.rotation.x;
            roty += pose.rotation.y;
            rotz += pose.rotation.z;
            rotw += pose.rotation.w;
        }
        
        var avgPos = new Vector3((float)(posx / count), (float)(posy / count), (float)(posz / count));
        var avgRot = new Quaternion((float)(rotx / count), (float)(roty / count), (float)(rotz / count), (float)(rotw / count));
        
        return new Pose(avgPos, avgRot);
    }
}