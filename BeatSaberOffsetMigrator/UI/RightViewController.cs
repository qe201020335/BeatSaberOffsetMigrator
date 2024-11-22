using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberOffsetMigrator.Patches;
using SiraUtil.Logging;
using TMPro;
using Zenject;

namespace BeatSaberOffsetMigrator.UI;

[ViewDefinition("BeatSaberOffsetMigrator.UI.BSML.RightView.bsml")]
[HotReload(RelativePathToLayout = @"BSML\RightView.bsml")]
public class RightViewController: BSMLAutomaticViewController
{
    [Inject]
    private readonly SiraLog _logger = null!;
    
    [Inject]
    private readonly OffsetHelper _offsetHelper = null!;
    
    [Inject]
    private readonly VRControllerPatch _vrControllerPatch = null!;
    
    private bool _parsed = false;

    [UIComponent("info_text")]
    private TMP_Text _infoText = null!; 
    
    [UIValue("supported")]
    private bool OffsetSupported => _offsetHelper.IsSupported;
    
    [UIValue("UseXROffset")]
    private bool UseXROffset
    {
        get => _vrControllerPatch.UseGeneratedOffset;
        set => _vrControllerPatch.UseGeneratedOffset = value;
    }

    [UIAction("#post-parse")]
    private void OnParsed()
    {
        _parsed = true;
    }
    
    private void Update()
    {
        if (!_parsed) return;

        // Should use events and not polling, but I am too lazy
        if (_offsetHelper.UnityOffsetSaved)
        {
            _infoText.text = "Unity offset:\n" +
                             $"L: {_offsetHelper.UnityOffsetL.Format()}\n" +
                             $"R: {_offsetHelper.UnityOffsetR.Format()}";
        }
        else
        {
            _infoText.text = "Unity offset not saved";
        }
    }


    
}