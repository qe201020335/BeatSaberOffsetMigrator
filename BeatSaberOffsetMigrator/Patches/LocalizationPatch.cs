using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BGLib.Polyglot;
using HarmonyLib;
using UnityEngine;

namespace BeatSaberOffsetMigrator.Patches;

[HarmonyPatch(typeof(LocalizationAsyncInstaller), nameof(LocalizationAsyncInstaller.LoadResourcesBeforeInstall))]
public class LocalizationPatch
{
    private static TextAsset? _asset = null;
    
    [HarmonyPrepare]
    private static bool Prepare()
    {
        if (_asset != null) return true;
        
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BeatSaberOffsetMigrator.Assets.Localization.csv");
        if (stream == null)  // really should not happen
        {
            Plugin.Log.Error("Failed to load localization csv text from resource!");
            return false;
        }

        using var reader = new StreamReader(stream);

        var content = reader.ReadToEnd();
        _asset = new TextAsset(content);
        return true;
    }
    
    [HarmonyPrefix]
    private static void Prefix(IList<TextAsset> assets)
    {
        assets.Add(_asset!);
    }
}