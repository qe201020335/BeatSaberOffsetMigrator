using System;
using SiraUtil.Logging;
using UnityEngine;
using Valve.VR;
using Zenject;

namespace BeatSaberOffsetMigrator;

public class OpenVRInputHelper: IInitializable, IDisposable, ITickable
{
    private readonly SiraLog _logger;

    private readonly CVRSystem _vrSystem;

    private readonly CVRCompositor _vrCompositor;

    private readonly IVRPlatformHelper _vrPlatformHelper;
    
    private readonly TrackedDevicePose_t[] _poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private readonly TrackedDevicePose_t[] _gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

    public uint LeftControllerIndex { get; private set; }
    public uint RightControllerIndex { get; private set; }
    
    
    private OpenVRInputHelper(SiraLog logger, IVRPlatformHelper platformHelper)
    {
        _logger = logger;
        _vrSystem = OpenVR.System;
        _vrCompositor = OpenVR.Compositor;
        _vrPlatformHelper = platformHelper;
    }

    void IInitializable.Initialize()
    {
        LoadControllers();
        _vrPlatformHelper.inputFocusWasCapturedEvent += OnInputFocusCaptured;
    }

    void IDisposable.Dispose()
    {
        _vrPlatformHelper.inputFocusWasCapturedEvent -= OnInputFocusCaptured;
    }

    void ITickable.Tick()
    {
        _vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, _poses);
    }

    private void OnInputFocusCaptured()
    {
        _logger.Debug("Input focused, loading controllers");
        LoadControllers();
    }

    private void LoadControllers()
    {
        _logger.Info("Loading controllers");
        _logger.Debug($"Current tracking space: {_vrCompositor.GetTrackingSpace()}");
        for (uint i = 0; i < 64; i++)
        {
            var c = _vrSystem.GetTrackedDeviceClass(i);
            if (c != ETrackedDeviceClass.Invalid && c != ETrackedDeviceClass.Max)
            {
                _logger.Debug($"Found device class {c} at index {i}");
            }
        }
        
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
    
    public Pose GetLeftControllerLastPose()
    {
        var m = _poses[LeftControllerIndex].mDeviceToAbsoluteTracking;
        return new Pose(m.GetPosition(), m.GetRotation());
    }
    
    public Pose GetRightControllerLastPose()
    {
        var m = _poses[RightControllerIndex].mDeviceToAbsoluteTracking;
        return new Pose(m.GetPosition(), m.GetRotation());
    }
}