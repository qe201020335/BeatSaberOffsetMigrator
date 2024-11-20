﻿using System;
using SiraUtil.Logging;
using UnityEngine;
using Valve.VR;
using Zenject;

namespace BeatSaberOffsetMigrator.InputHelper;

public class OpenVRInputHelper: IVRInputHelper, IInitializable, IDisposable, ITickable
{
    private readonly SiraLog _logger;

    private readonly CVRSystem? _vrSystem;

    private readonly CVRCompositor? _vrCompositor;

    [Inject]
    private readonly IVRPlatformHelper _vrPlatformHelper = null!;
    
    private readonly TrackedDevicePose_t[] _poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private readonly TrackedDevicePose_t[] _gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

    private uint _leftControllerIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
    private uint _rightControllerIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
    private bool _controllersFound = false;

    public string RuntimeName => "OpenVR (Steam VR)";
    public bool Supported => true;

    private bool _working = true;
    
    public bool Working
    {
        get => _working;
        private set
        {
            _working = value;
            if (value)
            {
                ReasonIfNotWorking = "";
            }
        }
    }

    public string ReasonIfNotWorking { get; private set; } = "";
    
    
    private OpenVRInputHelper(SiraLog logger)
    {
        _logger = logger;
        if (!OpenVRHelper.Initialize())
        {
            _logger.Error("Failed to initialize OpenVR");
            Working = false;
            ReasonIfNotWorking = "Failed to initialize OpenVR\nCheck logs for details";
        }
        else
        {
            _vrSystem = OpenVR.System;
            _vrCompositor = OpenVR.Compositor;
        }
    }

    void IInitializable.Initialize()
    {
        LoadControllers();
        _vrPlatformHelper.inputFocusWasCapturedEvent += OnInputFocusCaptured;
        Application.onBeforeRender += (this as ITickable).Tick;
    }

    void IDisposable.Dispose()
    {
        _vrPlatformHelper.inputFocusWasCapturedEvent -= OnInputFocusCaptured;
        Application.onBeforeRender -= (this as ITickable).Tick;
    }

    void ITickable.Tick()
    {
        if (_vrSystem == null || !_controllersFound) return;
        _vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, _poses);
        if ( _poses[_leftControllerIndex].eTrackingResult != ETrackingResult.Running_OK 
             || _poses[_rightControllerIndex].eTrackingResult != ETrackingResult.Running_OK)
        {
            Working = false;
            ReasonIfNotWorking = "Not all controllers are tracking normally!";
        }
        else
        {
            Working = true;
        }
    }
    
    private void OnInputFocusCaptured()
    {
        _logger.Debug("Input focused, loading controllers");
        LoadControllers();
    }

    private void LoadControllers()
    {
        _logger.Info("Loading controllers");
        if (_vrSystem == null || _vrCompositor == null)
        {
            _logger.Warn("OpenVR not initialized");
            return;
        }
        
        _logger.Debug($"Current tracking space: {_vrCompositor.GetTrackingSpace()}");
        for (uint i = 0; i < 64; i++)
        {
            var c = _vrSystem.GetTrackedDeviceClass(i);
            if (c != ETrackedDeviceClass.Invalid && c != ETrackedDeviceClass.Max)
            {
                _logger.Trace($"Found device class {c} at index {i}");
            }
        }
        
        _leftControllerIndex = _vrSystem.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
        _rightControllerIndex = _vrSystem.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);

        if (_leftControllerIndex == OpenVR.k_unTrackedDeviceIndexInvalid || _rightControllerIndex == OpenVR.k_unTrackedDeviceIndexInvalid)
        {
            _leftControllerIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
            _rightControllerIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
            _logger.Warn("Not enough controllers!");
            _controllersFound = false;
            Working = false;
            ReasonIfNotWorking = "Not all controllers are detected! Open and close SteamVR menu to refresh.";
        }
        else
        {
            _controllersFound = true;
            _logger.Notice($"Found index for controllers! {_leftControllerIndex} {_rightControllerIndex}");
        }
    }
    
    public Pose GetLeftVRControllerPose()
    {
        var m = _poses[_leftControllerIndex].mDeviceToAbsoluteTracking;
        return new Pose(m.GetPosition(), m.GetRotation());
    }

    public Pose GetRightVRControllerPose()
    {
        var m = _poses[_rightControllerIndex].mDeviceToAbsoluteTracking;
        return new Pose(m.GetPosition(), m.GetRotation());
    }
}