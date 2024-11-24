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

    private SiraLog _logger = null!;
    
    private PluginConfig _config = null!;

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
    
    internal void SaveUnityOffset()
    {
        if (!_vrPlatformHelper.GetNodePose(XRNode.LeftHand, _leftController.nodeIdx, out var leftPos, out var leftRot) ||
            !_vrPlatformHelper.GetNodePose(XRNode.RightHand, _rightController.nodeIdx, out var rightPos, out var rightRot))
        {
            _logger.Error("Failed to get node pose");
            ResetUnityOffset();
            return;
        }

        var unityL = new Pose(leftPos, leftRot);
        var unityR = new Pose(rightPos, rightRot);
        
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
    
}