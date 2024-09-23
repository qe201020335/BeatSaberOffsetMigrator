using BeatSaberOffsetMigrator.Configuration;
using SiraUtil.Logging;
using SiraUtil.Services;
using UnityEngine;
using Zenject;

namespace BeatSaberOffsetMigrator;

public class OffsetHelper: MonoBehaviour
{

    private SiraLog _logger = null!;
    
    private PluginConfig _config = null!;

    private VRController _leftController = null!;

    private VRController _rightController = null!;

    private OpenVRInputHelper _openVRInputHelper = null!;
    
    internal Pose LeftGamePose { get; private set; }
    
    internal Pose RightGamePose { get; private set; }
    
    internal Pose LeftSteamVRPose { get; private set; }
    
    internal Pose RightSteamVRPose { get; private set; }
    
    internal Pose LeftOffset => CalculateOffset(LeftSteamVRPose, LeftGamePose);
    internal Pose RightOffset => CalculateOffset(RightSteamVRPose, RightGamePose);
    
    
    [Inject]
    private void Init(SiraLog logger, PluginConfig config, IMenuControllerAccessor controllerAccessor, OpenVRInputHelper openVRInputHelper)
    {
        _logger = logger;
        _config = config;
        _leftController = controllerAccessor.LeftController;
        _rightController = controllerAccessor.RightController;
        _openVRInputHelper = openVRInputHelper;
        
        _logger.Debug("OffsetHelper initialized");
    }

    private Pose CalculateOffset(Pose from, Pose to)
    {
        var offset = new Pose
        {
            position = to.position - from.position,
            rotation = Quaternion.Inverse(from.rotation) * to.rotation
        };
        
        // _logger.Debug($"Offset: {offset.position}{offset.rotation}");
        return offset;
    }

    private void LateUpdate()
    {
        var leftPose = new SteamVR_Utils.RigidTransform(_openVRInputHelper.GetLeftControllerLastPose().mDeviceToAbsoluteTracking);
        LeftSteamVRPose = new Pose(leftPose.pos, leftPose.rot);
        
        var rightPose = new SteamVR_Utils.RigidTransform(_openVRInputHelper.GetRightControllerLastPose().mDeviceToAbsoluteTracking);
        RightSteamVRPose = new Pose(rightPose.pos, rightPose.rot);

        var leftLoc = _leftController.position;
        var leftRot = _leftController.rotation;
        LeftGamePose = new Pose(leftLoc, leftRot);

        var rightLoc = _rightController.position;
        var rightRot = _rightController.rotation;
        RightGamePose = new Pose(rightLoc, rightRot);
        
        // _logger.Debug($"L:{leftPose.pos}{leftPose.rot} {leftLoc}{leftRot} R:{rightPose.pos}{rightPose.rot} {rightLoc}{rightRot}");
    }
}