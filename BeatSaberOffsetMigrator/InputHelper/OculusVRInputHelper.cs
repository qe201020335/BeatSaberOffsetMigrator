using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BeatSaberOffsetMigrator.Shared;
using BeatSaberOffsetMigrator.Utils;
using BGLib.Polyglot;
using IPA.Utilities;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace BeatSaberOffsetMigrator.InputHelper;

public class OculusVRInputHelper: IVRInputHelper, ITickable, IInitializable, IDisposable
{
    private static readonly Pose LeftControllerOffset = new Pose(new Vector3(0.0f, -0.03f, -0.04f), Quaternion.Euler(-60.0f, 0.0f, 0.0f));

    public string RuntimeName => "OculusVR";
    public bool Supported => true;
    
    private bool _working;
    
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
    
    private readonly SiraLog _logger;
    
    private readonly OVRHelperSharedMemoryManager _sharedMemoryManager;
    
    private const string HelperName = "VRInputHelper.Oculus.exe";
    
    private readonly string _helperPath = Path.Combine(UnityGame.UserDataPath, @"BeatSaberOffsetMigrator\Helper", HelperName);
    
    private Pose _leftPose = Pose.identity;
    
    private Pose _rightPose = Pose.identity;
    
    private ControllerPose _poses;
    
    private Process? _helperProcess;

    private bool _helperRunning = false;
    
    private bool _disposing = false;
    
    public OculusVRInputHelper(SiraLog logger)
    {
        _logger = logger;
        _sharedMemoryManager = OVRHelperSharedMemoryManager.CreateReadOnly();
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => CleanUpHelper();  // in case we crash
    }
    
    public bool TryGetControllerOffset(out Pose leftOffset, out Pose rightOffset)
    {
        leftOffset = LeftControllerOffset;
        rightOffset = LeftControllerOffset.Mirror();
        return true;
    }

    private void StartHelper()
    {
        _logger.Info("Launching helper process");

        if (!File.Exists(_helperPath))
        {
            _logger.Critical("Failed to start helper process, helper executable not found");
            Working = false;
            ReasonIfNotWorking = Localization.Get("BSOM_ERR_HELPER_NOT_FOUND");
            return;
        }
        
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
            StartInfo = psi,
            EnableRaisingEvents = true
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
            _helperRunning = false;
            _logger.Notice("[VRInputHelper.Oculus] Process exited");
            Working = false;
            if (!_disposing)
            {
                _logger.Warn("Helper process exited unexpectedly");
                ReasonIfNotWorking = Localization.Get("BSOM_ERR_HELPER_EXITED");
            }
        };
        
        process.Start();
        _helperRunning = true;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _helperProcess = process;
    }

    private void CleanUpHelper()
    {
        var process = _helperProcess;
        _helperProcess = null;
        _helperRunning = false;
        if (process != null)
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
            process.Dispose();
        }
    }

    private async Task KillExistingAndStartNewHelper()
    {
        Working = false;
        ReasonIfNotWorking = "Starting helper process...";
        
        try
        {
            _logger.Debug("Killing existing helper process if it exists");
            // kill any existing helper if exists, should not happen but just in case
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/f /im {HelperName}",
                UseShellExecute = true,
                CreateNoWindow = true,
            };
            var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };
            process.Exited += (sender, args) =>
            {
                _logger.Debug("taskkill exited");
                process.Dispose();
            };
            process.Start();
        }
        catch (Exception e)
        {
            _logger.Critical("Failed to taskkill existing helper process");
            _logger.Critical(e);
        }
        
        _logger.Debug("Waiting for 3 second before starting new helper process");
        await Task.Delay(3000);
        
        try
        {
            StartHelper();
        }
        catch (Exception e)
        {
            _logger.Error("Failed to start helper process");
            _logger.Error(e);
            Working = false;
            ReasonIfNotWorking = Localization.Get("BSOM_ERR_HELPER_START_FAILED");
            CleanUpHelper();
        }
    }
    
    void IInitializable.Initialize()
    {
        UnityMainThreadTaskScheduler.Factory.StartNew(KillExistingAndStartNewHelper);
        Application.onBeforeRender += (this as ITickable).Tick;
    }
    
    void IDisposable.Dispose()
    {
        _disposing = true;
        Application.onBeforeRender -= (this as ITickable).Tick;
        CleanUpHelper();
    }
    
    void ITickable.Tick()
    {
        if (!_helperRunning) return;
        _sharedMemoryManager.Read(ref _poses);

        if (_poses.valid == 1)
        {
            Working = true;
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
        else
        {
            Working = false;
            ReasonIfNotWorking = Localization.Get("BSOM_ERR_CONTROLLERS_NOT_TRACKING");
            _leftPose = Pose.identity;
            _rightPose = Pose.identity;
        }
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