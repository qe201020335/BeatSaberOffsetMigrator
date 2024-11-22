﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    
    [Inject]
    private readonly SiraLog _logger = null!;

    private readonly JsonSerializer _serializer = new JsonSerializer();
    
    public Preset? CurrentPreset { get; private set; } = null;

    public string CurrentPresetName { get; private set; } = string.Empty;

    public IList<string> GetPresets()
    {
        return Directory.GetFiles(EasyOffsetPresetsPath, "*.json").Select(Path.GetFileName).ToArray();
    }
    
    public bool LoadPreset(string name)
    {
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
}