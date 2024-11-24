using System;
using System.Collections;
using System.Collections.Generic;
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

public class OffsetHelper
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
    
    internal bool IsSupported => _vrInputHelper.Supported;
    
    internal bool IsWorking => _vrInputHelper.Working;

    internal Pose LeftRuntimePose => _vrInputHelper.GetLeftVRControllerPose();
    
    internal Pose RightRuntimePose => _vrInputHelper.GetRightVRControllerPose();
    
    internal Pose LeftOffset => CalculateOffset(LeftRuntimePose, LeftGamePose);
    internal Pose RightOffset => CalculateOffset(RightRuntimePose, RightGamePose);
    
    internal Pose UnityOffsetL { get; private set; } = Pose.identity;
    internal Pose UnityOffsetR { get; private set; } = Pose.identity;
    private Pose UnityOffsetLReversed { get; set; } = Pose.identity;
    private Pose UnityOffsetRReversed { get; set; } = Pose.identity;
    
    private readonly IReadOnlyDictionary<string, Offset> _deviceOffsetTable;
    
    internal string SelectedDeviceOffset { get; private set; } = "None";

    private OffsetHelper(SiraLog logger, PluginConfig config, IMenuControllerAccessor controllerAccessor)
    {
        _logger = logger;
        _config = config;
        _leftController = controllerAccessor.LeftController;
        _rightController = controllerAccessor.RightController;

        try
        {
            _logger.Debug("Loading device offset table");
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BeatSaberOffsetMigrator.DeviceOffsetTable.json");
            using var textReader = new StreamReader(stream!);
            using var jsonReader = new JsonTextReader(textReader);
            var serializer = new JsonSerializer();
            var table = serializer.Deserialize<Dictionary<string, Offset>>(jsonReader);
            // shouldn't really happen
            _deviceOffsetTable = table ?? throw new Exception("Deserialized table is null");
        }
        catch (Exception e)
        {
            _logger.Error("Failed to load device offset table: " + e.Message);
            _logger.Error(e);
            _deviceOffsetTable = new Dictionary<string, Offset>();
        }

        SetSelectedDevice("None");
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

    private void SetUnityOffset(Pose left, Pose right)
    {
        _logger.Debug($"Using Offsets L: {left.Format()}, R: {right.Format()}");
        UnityOffsetL = left;
        UnityOffsetR = right;
        UnityOffsetLReversed = CalculateOffset(left, Pose.identity);
        UnityOffsetRReversed = CalculateOffset(right, Pose.identity);
    }

    internal string[] GetDeviceList() => ["None", .._deviceOffsetTable.Keys.OrderBy(name => name), "Custom"];

    internal bool SetSelectedDevice(string device)
    {
        if (device == SelectedDeviceOffset) return true;
        if (device == "Custom")
        {
            SetUnityOffset(_config.UnityOffset.Left, _config.UnityOffset.Right);
            SelectedDeviceOffset = device;
            return true;
        }

        if (device == "None")
        {
            SetUnityOffset(Pose.identity, Pose.identity);
            SelectedDeviceOffset = device;
            return true;
        }
        
        if (!_deviceOffsetTable.TryGetValue(device, out var offset))
        {
            SetUnityOffset(Pose.identity, Pose.identity);
            SelectedDeviceOffset = "None";
            _logger.Warn($"Device offset {device} not found");
            return false;
        }
        
        SelectedDeviceOffset = device;
        SetUnityOffset(offset.Left, offset.Right);
        return true;
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
                ResetUnityOffset();
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
        
        _config.UnityOffset = new Offset(offsetL, offsetR);
        SetUnityOffset(offsetL, offsetR);
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
    
    internal void ResetUnityOffset()
    {
        _config.UnityOffset = Offset.Identity;
        UnityOffsetLReversed = Pose.identity;
        UnityOffsetRReversed = Pose.identity;
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