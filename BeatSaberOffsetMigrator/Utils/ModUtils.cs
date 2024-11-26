using System.Linq;
using IPA.Loader;

namespace BeatSaberOffsetMigrator.Utils;

public class ModUtils
{
    public static bool IsModInstalled(string id)
    {
        return PluginManager.EnabledPlugins.Any(p => p.Id == id);
    }
}