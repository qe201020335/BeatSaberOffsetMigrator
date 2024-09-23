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
        var invRot = Quaternion.Inverse(from.rotation);
        
        var offset = new Pose
        {
            position = invRot * (to.position - from.position),
            rotation = invRot * to.rotation
        };
        
        // _logger.Debug($"Offset: {offset.position}{offset.rotation}");
        return offset;
    }

    private void LateUpdate()
    {
        LeftSteamVRPose = _openVRInputHelper.GetLeftControllerLastPose();
        RightSteamVRPose = _openVRInputHelper.GetRightControllerLastPose();

        var leftLoc = _leftController.viewAnchorTransform.position;
        var leftRot = _leftController.viewAnchorTransform.rotation;
        LeftGamePose = new Pose(leftLoc, leftRot);

        var rightLoc = _rightController.viewAnchorTransform.position;
        var rightRot = _rightController.viewAnchorTransform.rotation;
        RightGamePose = new Pose(rightLoc, rightRot);
        
        // _logger.Debug($"L:{leftPose.pos}{leftPose.rot} {leftLoc}{leftRot} R:{rightPose.pos}{rightPose.rot} {rightLoc}{rightRot}");
    }
}