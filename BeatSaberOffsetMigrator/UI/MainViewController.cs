using System;
using System.Linq;
using System.Text;
using BeatSaberMarkupLanguage.Attributes;
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
    
    [UIValue("ApplyOffset")]
    private bool ApplyOffset
    {
        get => _config.ApplyOffset;
        set
        {
            _config.ApplyOffset = value;
        }
    }

    [UIComponent("info_text")]
    private TMP_Text _infoText = null!;

    [UIValue("supported")]
    private bool OffsetSupported => _offsetHelper.IsSupported;

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

    [UIValue("controller-list-items")]
    private object[] _controllerList => _offsetHelper.GetDeviceList();
    
    [UIValue("controller-list-choice")]
    private string SelectedController
    {
        get => _offsetHelper.SelectedDeviceOffset;
        set
        {
            if (!_offsetHelper.SetSelectedDevice(value))
            {
                NotifyPropertyChanged();
            }
            
            NotifyPropertyChanged(nameof(IsCustomController));
        }
    }
    
    [UIValue("custom-device")]
    private bool IsCustomController => _offsetHelper.SelectedDeviceOffset == "Custom";

    [UIAction("#post-parse")]
    private void OnParsed()
    {
        _parsed = true;
        
        if (!_offsetHelper.IsSupported)
        {
            _infoText.text = "<color=red>Current runtime is not supported</color>";
        }
        
        EnableAdvance = false; // will also reset the value in the VRControllerPatch
    }

    private void Update()
    {
        if (!_parsed || !OffsetSupported) return;

        var builder = new StringBuilder(256);
        // Should use events and not polling, but I am too lazy
        if (_offsetHelper.SelectedDeviceOffset == "Custom")
        {
            builder.Append("Using custom Unity offset:\n" +
                           $"L: {_offsetHelper.UnityOffsetL.Format()}\n" +
                           $"R: {_offsetHelper.UnityOffsetR.Format()}\n");
        }
        if (_easyOffsetManager.CurrentPresetName != string.Empty)
        {
            builder.Append($"Using EasyOffset preset: {_easyOffsetManager.CurrentPresetName}\n");
            var preset = _easyOffsetManager.CurrentPreset!;
            builder.Append($"L: {preset.LeftOffset.Format()}\n" +
                           $"R: {preset.RightOffset.Format()}");
        }
        else
        {
            builder.Append("No EasyOffset preset selected or failed to load.");
        }
        
        _infoText.text = builder.ToString();
    }
    
    [UIAction("reset_unity_offset")]
    private void ResetUnityOffset()
    {
        _offsetHelper.ResetUnityOffset();
    }
}