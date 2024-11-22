using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.InputHelper;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using SiraUtil.Services;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace BeatSaberOffsetMigrator;

public class OffsetHelper: MonoBehaviour
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


    [Inject]
    private void Init(SiraLog logger, PluginConfig config, IMenuControllerAccessor controllerAccessor, IVRInputHelper vrInputHelper, IVRPlatformHelper vrPlatformHelper)
    {
        _logger = logger;
        _config = config;
        _leftController = controllerAccessor.LeftController;
        _rightController = controllerAccessor.RightController;
        _vrInputHelper = vrInputHelper;
        _vrPlatformHelper = vrPlatformHelper;
        
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

    internal bool UnityOffsetSaved { get; private set; }
    internal Pose UnityOffsetL { get; private set; }
    internal Pose UnityOffsetR { get; private set; }
    
    internal void SaveUnityOffset()
    {
        if (!_vrPlatformHelper.GetNodePose(XRNode.LeftHand, _leftController.nodeIdx, out var leftPos, out var leftRot) ||
            !_vrPlatformHelper.GetNodePose(XRNode.RightHand, _rightController.nodeIdx, out var rightPos, out var rightRot))
        {
            _logger.Error("Failed to get node pose");
            UnityOffsetSaved = false;
            UnityOffsetL = default;
            UnityOffsetR = default;
            return;
        }

        var unityL = new Pose(leftPos, leftRot);
        var unityR = new Pose(rightPos, rightRot);
        
        UnityOffsetL = CalculateOffset(LeftRuntimePose, unityL);
        UnityOffsetR = CalculateOffset(RightRuntimePose, unityR);
        
        UnityOffsetSaved = true;
    }
    
    
}