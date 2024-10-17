using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.InputHelper;
using SiraUtil.Affinity;
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
    
    internal Pose LeftGamePose { get; set; }
    
    internal Pose RightGamePose { get; set; }
    
    internal bool IsSupported => _vrInputHelper.Supported;

    internal Pose LeftRuntimePose => _vrInputHelper.GetLeftVRControllerPose();
    
    internal Pose RightRuntimePose => _vrInputHelper.GetRightVRControllerPose();
    
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
        
        return offset;
    }
}