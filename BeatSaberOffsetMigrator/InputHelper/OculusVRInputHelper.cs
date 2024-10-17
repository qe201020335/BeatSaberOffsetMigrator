using System;
using System.Diagnostics;
using System.IO;
using BeatSaberOffsetMigrator.Shared;
using IPA.Utilities;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace BeatSaberOffsetMigrator.InputHelper;

public class OculusVRInputHelper: IVRInputHelper, ITickable, IInitializable, IDisposable
{
    public string RuntimeName => "OculusVR";
    public bool Supported => true;
    
    private readonly SiraLog _logger;
    
    private readonly OVRHelperSharedMemoryManager _sharedMemoryManager;
    
    private readonly string _helperPath = Path.Combine(UnityGame.UserDataPath, @"BeatSaberOffsetMigrator\Helper\VRInputHelper.Oculus.exe");
    
    private Pose _leftPose = Pose.identity;
    
    private Pose _rightPose = Pose.identity;
    
    private ControllerPose _poses;
    
    private Process? _helperProcess;
    
    public OculusVRInputHelper(SiraLog logger)
    {
        _logger = logger;
        _sharedMemoryManager = OVRHelperSharedMemoryManager.CreateReadOnly();
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => CleanUpHelper();  // in case we crash
    }

    private void StartHelper()
    {
        var psi = new ProcessStartInfo
        {
            FileName = _helperPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        var process = new Process
        {
            StartInfo = psi
        };
        
        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data == null) return;
            _logger.Info($"[VRInputHelper.Oculus] {args.Data}");
        };
        
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data == null) return;
            _logger.Error($"[VRInputHelper.Oculus] {args.Data}");
        };
        
        process.Exited += (sender, args) =>
        {
            _logger.Info("[VRInputHelper.Oculus] Process exited");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _helperProcess = process;
    }

    private void CleanUpHelper()
    {
        _helperProcess?.Kill();
        _helperProcess?.Dispose();
        _helperProcess = null;
    }
    
    void IInitializable.Initialize()
    {
        StartHelper();
        Application.onBeforeRender += (this as ITickable).Tick;
    }
    
    void IDisposable.Dispose()
    {
        CleanUpHelper();
        Application.onBeforeRender -= (this as ITickable).Tick;
    }
    
    void ITickable.Tick()
    {
        _sharedMemoryManager.Read(ref _poses);
        if (_poses.valid != 1) return;
        
        _leftPose = new Pose
        {
            position = new Vector3(_poses.lposx, _poses.lposy, _poses.lposz),
            rotation = new Quaternion(_poses.lrotx, _poses.lroty, _poses.lrotz, _poses.lrotw)
        };
        
        _rightPose = new Pose
        {
            position = new Vector3(_poses.rposx, _poses.rposy, _poses.rposz),
            rotation = new Quaternion(_poses.rrotx, _poses.rroty, _poses.rrotz, _poses.rrotw)
        };
    }
    
    public Pose GetLeftVRControllerPose()
    {
        return _leftPose;
    }

    public Pose GetRightVRControllerPose()
    {
        return _rightPose;
    }
}