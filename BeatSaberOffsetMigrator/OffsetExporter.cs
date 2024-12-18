using System.Collections.Generic;
using System.Linq;
using BeatSaber.GameSettings;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaberOffsetMigrator;

public class OffsetExporter: IAffinity
{
    // [Inject]
    private readonly SiraLog _logger;

    [Inject]
    private readonly IVRPlatformHelper _vrPlatformHelper = null!;
    
    // [Inject]
    private readonly ControllerProfilesModel _controllerProfilesModel;
    
    private readonly IReadOnlyList<int> _customProfileIndices;

    public IEnumerable<int> CustomProfileIndices => _customProfileIndices;

    public OffsetExporter(SiraLog logger, ControllerProfilesModel controllerProfilesModel)
    {
        _logger = logger;
        _controllerProfilesModel = controllerProfilesModel;
        
        var profiles = _controllerProfilesModel.profiles;
        var indices = new List<int>();
        for (var i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            if (profile.modifiable)
            {
                indices.Add(i);
            }
        }
        
        _customProfileIndices = indices;
        _logger.Debug("Custom Controller Profiles: " + string.Join(", ", _customProfileIndices));
    }

    public string GetProfileName(int index)
    {
        var profile = _controllerProfilesModel._profiles[index];
        return ControllerProfilesSettingsViewController.GetControllerProfileDisplayName(profile);
    }
    
    public void ExportOffset(int index, bool alternativeHandling)
    {
        _logger.Notice($"Exporting current offset to game settings. Index: {index}, AlternativeHandling: {alternativeHandling}");
        var profile = _controllerProfilesModel._profiles[index];
        // TODO export
        
        
        profile.alternativeHandling = alternativeHandling;
        _controllerProfilesModel.UpdateSelectedProfile(index, true);
    }
}