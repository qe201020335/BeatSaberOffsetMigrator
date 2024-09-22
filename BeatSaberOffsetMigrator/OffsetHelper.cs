using SiraUtil.Logging;
using SiraUtil.Services;
using UnityEngine;
using Zenject;

namespace BeatSaberOffsetMigrator;

public class OffsetHelper: MonoBehaviour
{

    private SiraLog _logger;

    private VRController _leftController;

    private VRController _rightController;

    private OpenVRInputHelper _openVRInputHelper;
    
    
    [Inject]
    private void Init(SiraLog logger, IMenuControllerAccessor controllerAccessor, OpenVRInputHelper openVRInputHelper)
    {
        _logger = logger;
        _leftController = controllerAccessor.LeftController;
        _rightController = controllerAccessor.RightController;
        _openVRInputHelper = openVRInputHelper;
        
        _logger.Debug("OffsetHelper initialized");
    }

    private void LateUpdate()
    {
        // var leftVRCon = _openVRInputHelper.LeftController;
        // var rightVRCon = _openVRInputHelper.RightController;
        // if (leftVRCon == null || rightVRCon == null)
        // {
        //     return;
        // }
        
        var leftPose = new SteamVR_Utils.RigidTransform(_openVRInputHelper.GetLeftControllerLastPose().mDeviceToAbsoluteTracking);
        var rightPose = new SteamVR_Utils.RigidTransform(_openVRInputHelper.GetRightControllerLastPose().mDeviceToAbsoluteTracking);

        var leftLoc = _leftController.position;
        var leftRot = _leftController.rotation;

        var rightLoc = _rightController.position;
        var rightRot = _rightController.rotation;
        
        _logger.Debug($"L:{leftPose.pos}{leftPose.rot} {leftLoc}{leftRot} R:{rightPose.pos}{rightPose.rot} {rightLoc}{rightRot}");
    }
}