using System;

namespace BeatSaberOffsetMigrator;

public class EasyOffsetExporter
{
    private static readonly Lazy<bool> _isEasyOffsetInstalled = new Lazy<bool>(() => Utils.IsModInstalled("EasyOffset"));

    public static bool IsEasyOffsetInstalled => _isEasyOffsetInstalled.Value;

    public static bool IsEasyOffsetDisabled => !EasyOffset.PluginConfig.Enabled;

    public static bool ExportToEastOffset()
    {
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