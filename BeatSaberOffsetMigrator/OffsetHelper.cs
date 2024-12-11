using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.EO;
using BeatSaberOffsetMigrator.InputHelper;
using BeatSaberOffsetMigrator.Models;
using BeatSaberOffsetMigrator.Utils;
using Newtonsoft.Json;
using SiraUtil.Logging;
using SiraUtil.Services;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace BeatSaberOffsetMigrator;

public class OffsetHelper: IInitializable, IDisposable
{
    private readonly SiraLog _logger;
    
    private readonly PluginConfig _config;
    
    private readonly VRController _leftController;

    private readonly VRController _rightController;

    [Inject]
    private readonly IVRInputHelper _vrInputHelper = null!;
    
    [Inject]
    private readonly IVRPlatformHelper _vrPlatformHelper = null!;
    
    internal Pose LeftGamePose { get; set; }
    
    internal Pose RightGamePose { get; set; }
    
    internal bool IsRuntimeSupported => _vrInputHelper.Supported;
    
    internal bool IsRuntimePoseValid => _vrInputHelper.Working;

    internal Pose LeftRuntimePose => _vrInputHelper.GetLeftVRControllerPose();
    
    internal Pose RightRuntimePose => _vrInputHelper.GetRightVRControllerPose();
    
    internal Pose LeftOffset => CalculateOffset(LeftRuntimePose, LeftGamePose);
    internal Pose RightOffset => CalculateOffset(RightRuntimePose, RightGamePose);
    
    internal Pose UnityOffsetL { get; private set; } = Pose.identity;
    internal Pose UnityOffsetR { get; private set; } = Pose.identity;
    private Pose UnityOffsetLReversed { get; set; } = Pose.identity;
    private Pose UnityOffsetRReversed { get; set; } = Pose.identity;

    private bool _usingCustomOffset;
    internal bool RuntimeOffsetValid { get; private set; }

    private OffsetHelper(SiraLog logger, PluginConfig config, IMenuControllerAccessor controllerAccessor)
    {
        _logger = logger;
        _config = config;
        _leftController = controllerAccessor.LeftController;
        _rightController = controllerAccessor.RightController; 
    }

    void IInitializable.Initialize()
    {
        RefreshRuntimeOffset();
        _config.ConfigDidChange += OnConfigChanged;
        _vrPlatformHelper.controllersDidChangeReferenceEvent += ControllerReferenceChanged;
    }
    
    void IDisposable.Dispose()
    {
        _config.ConfigDidChange -= OnConfigChanged;
        _vrPlatformHelper.controllersDidChangeReferenceEvent -= ControllerReferenceChanged;
    }

    private void OnConfigChanged()
    {
        // If we just switched to using custom offset or we were using it but should not anymore
        if (_config.UseCustomRuntimeOffset || _usingCustomOffset)  
        {
            RefreshRuntimeOffset();
        }
    }

    private void ControllerReferenceChanged()
    {
        _logger.Debug("Controller reference changed, loading controller offsets");
        if (!_config.UseCustomRuntimeOffset)
        {
            RefreshRuntimeOffset();
        }
    }

    internal void RefreshRuntimeOffset()
    {
        _logger.Debug("Refreshing runtime offset");
        _usingCustomOffset = _config.UseCustomRuntimeOffset;
        if (_usingCustomOffset)
        {
            SetRuntimeOffset(_config.CustomRuntimeOffset.Left, _config.CustomRuntimeOffset.Right);
            RuntimeOffsetValid = true;
        }
        else
        {
            var success = _vrInputHelper.TryGetControllerOffset(out var left, out var right);
            RuntimeOffsetValid = success;
            if (success)
            {
                SetRuntimeOffset(left, right);
            }
            else
            {
                _logger.Warn("Failed to get runtime controller offset from VR input helper");
                SetRuntimeOffset(Pose.identity, Pose.identity);
            }
        }
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

    private void SetRuntimeOffset(Pose left, Pose right)
    {
        _logger.Debug($"Using Runtime Offsets L: {left.Format()}, R: {right.Format()}");
        UnityOffsetL = left;
        UnityOffsetR = right;
        UnityOffsetLReversed = CalculateOffset(left, Pose.identity);
        UnityOffsetRReversed = CalculateOffset(right, Pose.identity);
        _logger.Debug($"Reversed Runtime Offsets L: {UnityOffsetLReversed.FormatQ()}, R: {UnityOffsetRReversed.FormatQ()}");
    }
    
    internal IEnumerator SaveUnityOffset()
    {
        var count = _config.OffsetSampleCount;
        var leftPoses = new List<Pose>(count);
        var rightPoses = new List<Pose>(count);
        for (int i = 0; i < count; i++)
        {
            if (!_vrPlatformHelper.GetNodePose(XRNode.LeftHand, _leftController.nodeIdx, out var leftPos, out var leftRot) ||
                !_vrPlatformHelper.GetNodePose(XRNode.RightHand, _rightController.nodeIdx, out var rightPos, out var rightRot))
            {
                _logger.Error("Failed to get node pose");
                yield break;
            }
            
            leftPoses.Add(new Pose(leftPos, leftRot));
            rightPoses.Add(new Pose(rightPos, rightRot));
            yield return null;
        }
        

        var unityL = AveragePose(leftPoses);
        var unityR = AveragePose(rightPoses);
        
        var offsetL = CalculateOffset(LeftRuntimePose, unityL);
        var offsetR = CalculateOffset(RightRuntimePose, unityR);
        
        _config.CustomRuntimeOffset = new Offset(offsetL, offsetR);
        SetRuntimeOffset(offsetL, offsetR);
    }
    
    internal void RevertUnityOffset(Transform t, XRNode node)
    {
        Pose offset;
        switch (node)
        {
            case XRNode.LeftHand:
                offset = UnityOffsetLReversed;
                break;
            case XRNode.RightHand:
                offset = UnityOffsetRReversed;
                break;
            default:
                return;
        }

        t.Offset(offset);
    }
    
    private Pose AveragePose(List<Pose> poses)
    {
        var count = poses.Count;

        if (count == 0) return Pose.identity;
        
        double posx = 0, posy = 0, posz = 0, rotx = 0, roty = 0, rotz = 0, rotw = 0;
        foreach (var pose in poses)
        {
            posx += pose.position.x;
            posy += pose.position.y;
            posz += pose.position.z;
            rotx += pose.rotation.x;
            roty += pose.rotation.y;
            rotz += pose.rotation.z;
            rotw += pose.rotation.w;
        }
        
        var avgPos = new Vector3((float)(posx / count), (float)(posy / count), (float)(posz / count));
        var avgRot = new Quaternion((float)(rotx / count), (float)(roty / count), (float)(rotz / count), (float)(rotw / count));
        
        return new Pose(avgPos, avgRot);
    }
}