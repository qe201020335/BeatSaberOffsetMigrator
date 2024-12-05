using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.Installers;
using BeatSaberOffsetMigrator.Utils;
using IPA.Utilities;
using Newtonsoft.Json;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace BeatSaberOffsetMigrator.EO;

public class EasyOffsetManager
{
    private static readonly string EasyOffsetPresetsPath = Path.Combine(UnityGame.UserDataPath, "EasyOffset", "Presets");
    
    //values copied from 1.29 EasyOffset
    private static readonly Pose OculusVRExtraOffset = new Pose(new Vector3(0f, 0f, 0.055f), Quaternion.Euler(-40f, 0f, 0f));
    
    [Inject]
    private readonly SiraLog _logger = null!;
    
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject(Id = AppInstaller.IsOVRBindingKey)]
    private readonly bool IsOvr;

    private readonly JsonSerializer _serializer = new JsonSerializer();
    
    public Preset? CurrentPreset { get; private set; } = null;

    public string CurrentPresetName { get; private set; } = string.Empty;

    [Inject]
    private void Init()
    {
        LoadPreset(_config.SelectedEasyOffsetPreset);
    }

    public IList<string> GetPresets()
    {
        _logger.Debug("Getting preset files from UserData");
        if (!Directory.Exists(EasyOffsetPresetsPath))
        {
            _logger.Warn("EasyOffset presets directory does not exist");
            return Array.Empty<string>();
        }

        try
        {
            return Directory.GetFiles(EasyOffsetPresetsPath, "*.json").Select(Path.GetFileName).ToArray();
        }
        catch (Exception e)
        {
            _logger.Critical("Failed to get easyoffet preset files: " + e.Message);
            _logger.Critical(e);
            return Array.Empty<string>();
        }
    }
    
    public bool LoadPreset(string name)
    {
        _config.SelectedEasyOffsetPreset = name;
        
        if (string.IsNullOrWhiteSpace(name))
        {
            CurrentPreset = null;
            CurrentPresetName = string.Empty;
            return true;
        }
        
        var path = Path.Combine(EasyOffsetPresetsPath, name);
        if (!File.Exists(path))
        {
            _logger.Warn($"Preset {name} does not exist");
            return false;
        }
        
        // Load the preset
        try
        {
            _logger.Info("Loading preset: " + name);
            using var textReader = new StreamReader(path);
            using var jsonReader = new JsonTextReader(textReader);
            CurrentPreset = _serializer.Deserialize<Preset>(jsonReader);
            var success = CurrentPreset != null;
            CurrentPresetName = success ? name : string.Empty;
            return success;
        }
        catch (Exception e)
        {
            _logger.Critical($"Failed to load preset: {e.Message}");
            _logger.Critical(e);
            CurrentPreset = null;
            CurrentPresetName = string.Empty;
            return false;
        }
    }

    public void ApplyOffset(Transform transform, XRNode node)
    {
        if (CurrentPreset == null) return;
        
        if (IsOvr)
        {
            ApplyExtraOculusOffset(transform);
        }
        
        switch (node)
        {
            case XRNode.LeftHand:
                CurrentPreset.TransformLeft(transform);
                break;
            case XRNode.RightHand:
                CurrentPreset.TransformRight(transform);
                break;
            default:
                return;
        }
    }
    
    private void ApplyExtraOculusOffset(Transform transform)
    {
        transform.rotation *= OculusVRExtraOffset.rotation;
        transform.position += transform.rotation * OculusVRExtraOffset.position;
    }
}