using System;
using SiraUtil.Logging;
using UnityEngine;
using Valve.VR;
using Zenject;

namespace BeatSaberOffsetMigrator.InputHelper;

public class OpenVRInputHelper: IVRInputHelper, IInitializable, IDisposable
{
    private readonly SiraLog _logger;

    private readonly CVRSystem _vrSystem;

    private readonly CVRCompositor _vrCompositor;

    private readonly OpenVRHelper _openVRHelper;

    private uint LeftControllerIndex { get; set; }
    private uint RightControllerIndex { get; set; }

    public string RuntimeName => "OpenVR (Steam VR)";
    public bool Supported => true;
    
    private OpenVRInputHelper(SiraLog logger, IVRPlatformHelper platformHelper)
    {
        _logger = logger;
        _vrSystem = OpenVR.System;
        _vrCompositor = OpenVR.Compositor;
        _openVRHelper = (platformHelper as OpenVRHelper)!;
    }

    void IInitializable.Initialize()
    {
        LoadControllers();
        _openVRHelper.inputFocusWasCapturedEvent += OnInputFocusCaptured;
    }

    void IDisposable.Dispose()
    {
        _openVRHelper.inputFocusWasCapturedEvent -= OnInputFocusCaptured;
    }

    private void OnInputFocusCaptured()
    {
        _logger.Debug("Input focused, loading controllers");
        LoadControllers();
    }

    private void LoadControllers()
    {
        var leftIndex = _vrSystem.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
        var rightIndex = _vrSystem.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);

        if (leftIndex == OpenVR.k_unTrackedDeviceIndexInvalid || rightIndex == OpenVR.k_unTrackedDeviceIndexInvalid)
        {
            _logger.Warn("Not enough controllers!");
            return;
        }

        LeftControllerIndex = leftIndex;
        RightControllerIndex = rightIndex;
        
        _logger.Notice($"Found index for controllers! {LeftControllerIndex} {RightControllerIndex}");
    }
    
    public Pose GetLeftVRControllerPose()
    {
        var transform = new SteamVR_Utils.RigidTransform(_openVRHelper._poses[LeftControllerIndex].mDeviceToAbsoluteTracking);
        return new Pose(transform.pos, transform.rot);
    }

    public Pose GetRightVRControllerPose()
    {
        var transform = new SteamVR_Utils.RigidTransform(_openVRHelper._poses[RightControllerIndex].mDeviceToAbsoluteTracking);
        return new Pose(transform.pos, transform.rot);
    }
}