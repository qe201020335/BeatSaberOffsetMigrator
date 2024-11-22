using System;
using BeatSaberOffsetMigrator.Utils;
using Zenject;

namespace BeatSaberOffsetMigrator.EO;

public class EasyOffsetExporter
{
    [Inject]
    private readonly OffsetHelper _offsetHelper = null!;

    public bool IsEasyOffsetInstalled { get; } = ModUtils.IsModInstalled("EasyOffset");

    public bool IsEasyOffsetDisabled => !EasyOffset.PluginConfig.Enabled;

    public bool ExportToEastOffset()
    {
        if (!_offsetHelper.IsWorking)
        {
            Plugin.Log.Warn("OffsetHelper is not working, cannot export to EasyOffset");
            return false;
        }
        
        try
        {
            var result = EasyOffset.ConfigMigration.UniversalImport();
            if (result != EasyOffset.ConfigImportResult.Success)
            {
                Plugin.Log.Warn($"Failed to exported to EasyOffset: {result}");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Plugin.Log.Error("Failed to let EasyOffset's UniversalImport import the offset");
            Plugin.Log.Error(e);
            return false;
        }
    }
}