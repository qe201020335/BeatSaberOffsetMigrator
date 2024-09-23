using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.InputHelper;
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

    private IVRInputHelper _vrInputHelper = null!;
    
    internal Pose LeftGamePose { get; private set; }
    
    internal Pose RightGamePose { get; private set; }
    
    internal bool IsSupported => _vrInputHelper.Supported;
    
    internal Pose LeftRuntimePose { get; private set; }
    
    internal Pose RightRuntimePose { get; private set; }
    
    internal Pose LeftOffset => CalculateOffset(LeftRuntimePose, LeftGamePose);
    internal Pose RightOffset => CalculateOffset(RightRuntimePose, RightGamePose);


    [Inject]
    private void Init(SiraLog logger, PluginConfig config, IMenuControllerAccessor controllerAccessor, IVRInputHelper vrInputHelper)
    {
        _logger = logger;
        _config = config;
        _leftController = controllerAccessor.LeftController;
        _rightController = controllerAccessor.RightController;
        _vrInputHelper = vrInputHelper;
        
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
        LeftRuntimePose = _vrInputHelper.GetLeftVRControllerPose();
        RightRuntimePose = _vrInputHelper.GetRightVRControllerPose();

        var leftLoc = _leftController.viewAnchorTransform.position;
        var leftRot = _leftController.viewAnchorTransform.rotation;
        LeftGamePose = new Pose(leftLoc, leftRot);

        var rightLoc = _rightController.viewAnchorTransform.position;
        var rightRot = _rightController.viewAnchorTransform.rotation;
        RightGamePose = new Pose(rightLoc, rightRot);
        
        // _logger.Debug($"L:{leftPose.pos}{leftPose.rot} {leftLoc}{leftRot} R:{rightPose.pos}{rightPose.rot} {rightLoc}{rightRot}");
    }
}