﻿using System;
using System.Text;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberOffsetMigrator.EO;
using BeatSaberOffsetMigrator.Patches;
using SiraUtil.Logging;
using TMPro;
using Zenject;

namespace BeatSaberOffsetMigrator.UI;

[ViewDefinition("BeatSaberOffsetMigrator.UI.BSML.RightView.bsml")]
[HotReload(RelativePathToLayout = @"BSML\RightView.bsml")]
public class RightViewController : BSMLAutomaticViewController
{
    [Inject]
    private readonly SiraLog _logger = null!;

    [Inject]
    private readonly OffsetHelper _offsetHelper = null!;

    [Inject]
    private readonly EasyOffsetManager _easyOffsetManager = null!;

    [Inject]
    private readonly VRControllerPatch _vrControllerPatch = null!;

    private bool _parsed = false;

    [UIComponent("info_text")]
    private TMP_Text _infoText = null!;

    [UIValue("supported")]
    private bool OffsetSupported => _offsetHelper.IsSupported;

    [UIValue("UseGenOffset")]
    private bool UseXROffset
    {
        get => _vrControllerPatch.UseGeneratedOffset;
        set => _vrControllerPatch.UseGeneratedOffset = value;
    }

    [UIValue("preset-list-items")]
    private object[] PresetNames => ["None", .._easyOffsetManager.GetPresets()];

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

    [UIAction("#post-parse")]
    private void OnParsed()
    {
        _parsed = true;
    }

    private void Update()
    {
        if (!_parsed) return;

        var builder = new StringBuilder(256);
        // Should use events and not polling, but I am too lazy
        if (_offsetHelper.UnityOffsetSaved)
        {
            builder.Append("Unity offset:\n" +
                           $"L: {_offsetHelper.UnityOffsetL.Format()}\n" +
                           $"R: {_offsetHelper.UnityOffsetR.Format()}");
        }
        else
        {
            builder.Append("Unity offset not recorded");
        }
        
        builder.Append("\n");
        if (_easyOffsetManager.CurrentPresetName != string.Empty)
        {
            builder.Append($"Current preset: {_easyOffsetManager.CurrentPresetName}\n");
            var preset = _easyOffsetManager.CurrentPreset!;
            builder.Append($"L: {preset.LeftOffset.Format()}\n" +
                           $"R: {preset.RightOffset.Format()}");
        }
        else
        {
            builder.Append("No preset selected or failed to load.");
        }
        
        _infoText.text = builder.ToString();
    }
    
    [UIAction("reset_unity_offset")]
    private void ResetUnityOffset()
    {
        _offsetHelper.ResetUnityOffset();
    }
}