using System;
using System.Linq;
using IPA.Loader;
using UnityEngine;

namespace BeatSaberOffsetMigrator;

public static class Utils
{
    public static Vector3 ClampAngle(Vector3 euler)
    {
        return new Vector3(
            ClampAngle(euler.x),
            ClampAngle(euler.y),
            ClampAngle(euler.z)
        );
    }
    
    private static float ClampAngle(float angle)
    {
        angle = (angle + 360) % 360;
        if (angle > 180)
        {
            angle -= 360;
        }

        return angle;
    }
    
    public static bool IsModInstalled(string id)
    {
        return PluginManager.EnabledPlugins.Any(p => p.Id == id);
    }
}