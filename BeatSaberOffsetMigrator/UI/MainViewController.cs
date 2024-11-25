﻿using System;
using System.Collections;
using System.Linq;
using System.Text;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.EO;
using BeatSaberOffsetMigrator.Patches;
using BeatSaberOffsetMigrator.Utils;
using SiraUtil.Logging;
using TMPro;
using Zenject;

namespace BeatSaberOffsetMigrator.UI;

[ViewDefinition("BeatSaberOffsetMigrator.UI.BSML.MainView.bsml")]
[HotReload(RelativePathToLayout = @"BSML\MainView.bsml")]
public class MainViewController : BSMLAutomaticViewController
{
    [Inject]
    private readonly SiraLog _logger = null!;
    
    [Inject]
    private readonly PluginConfig _config = null!;

    [Inject]
    private readonly OffsetHelper _offsetHelper = null!;

    [Inject]
    private readonly EasyOffsetManager _easyOffsetManager = null!;

    [Inject]
    private readonly VRControllerPatch _vrControllerPatch = null!;

    private bool _parsed = false;
    
    private readonly StringBuilder _builder = new StringBuilder(256);
    
    [UIValue("ApplyOffset")]
    private bool ApplyOffset
    {
        get => _config.ApplyOffset;
        set
        {
            _config.ApplyOffset = value;
        }
    }

    [UIComponent("info_text1")]
    private TMP_Text _infoText1 = null!;
    
    [UIComponent("info_text2")]
    private TMP_Text _infoText2 = null!;

    [UIValue("supported")]
    private bool OffsetSupported => _offsetHelper.IsRuntimeSupported;

    private bool _enableAdvance = false;

    [UIValue("EnableAdvance")]
    public bool EnableAdvance
    {
        get => _enableAdvance;
        set
        {
            _vrControllerPatch.UseGeneratedOffset = !value;
            if (_enableAdvance != value)
            {
                _enableAdvance = value;
                NotifyPropertyChanged();
            }
        }
    }

    [UIValue("preset-list-items")]
    private object[] _presetNames = ["None"];

    [UIComponent("preset-list")]
    private DropDownListSetting _presetList = null!;

    [UIValue("preset-list-choice")]
    private string SelectedPreset
    {
        get
        {
            var presetName = _easyOffsetManager.CurrentPresetName;
            return string.IsNullOrWhiteSpace(presetName) ? "None" : presetName;
        }
        set
        {
            if (value == "None")
            {
                _easyOffsetManager.LoadPreset(string.Empty);
            }
            else
            {
                _easyOffsetManager.LoadPreset(value);
            }
        }
    }

    [UIValue("UseCustomOffset")]
    private bool UseCustomOffset
    {
        get => _config.UseCustomRuntimeOffset;
        set => _config.UseCustomRuntimeOffset = value;
    }

    [UIAction("#post-parse")]
    private void OnParsed()
    {
        _parsed = true;
        
        if (!_offsetHelper.IsRuntimeSupported)
        {
            _infoText1.text = "<color=red>Current runtime is not supported</color>";
            _infoText2.text = "";
        }
        
        EnableAdvance = false; // will also reset the value in the VRControllerPatch
        RefreshPresets();
    }
    
    [UIAction("refresh_offset")]
    private void RefreshOffset()
    {
        _offsetHelper.RefreshRuntimeOffset();
    }

    [UIAction("refresh_presets")]
    private void RefreshPresets()
    {
        object[] list = ["None", .._easyOffsetManager.GetPresets()];
        _presetNames = list;
        _presetList.Values = list;
        _presetList.UpdateChoices();
        _presetList.ReceiveValue();
    }

    private void Update()
    {
        if (!_parsed || !OffsetSupported) return;

        _builder.Clear();
        // Should use events and not polling, but I am too lazy
        if (UseCustomOffset)
        {
            _builder.Append("Using custom runtime offset:\n" +
                            $"L: {_offsetHelper.UnityOffsetL.Format()}\n" +
                            $"R: {_offsetHelper.UnityOffsetR.Format()}");
        }
        else if (_offsetHelper.RuntimeOffsetValid)
        {
            _builder.Append("Using offset for current device\n" +
                            $"L: {_offsetHelper.UnityOffsetL.Format()}\n" +
                            $"R: {_offsetHelper.UnityOffsetR.Format()}");
        }
        else
        {
            _builder.Append("<color=#FF0000>Failed to get runtime offset</color>\nCheck logs for details.");
        }

        _infoText1.text = _builder.ToString();
        
        _builder.Clear();
        if (_easyOffsetManager.CurrentPresetName != string.Empty)
        {
            _builder.Append($"Using EasyOffset preset: {_easyOffsetManager.CurrentPresetName}\n");
            var preset = _easyOffsetManager.CurrentPreset!;
            _builder.Append($"L: {preset.LeftOffset.Format()}\n" +
                            $"R: {preset.RightOffset.Format()}");
        }
        else
        {
            _builder.Append("No EasyOffset preset selected or failed to load selected preset.");
        }
        
        _infoText2.text = _builder.ToString();
    }
}