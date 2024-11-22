using UnityEngine;

namespace BeatSaberOffsetMigrator.EO;

public static class EOExtensions
{
    public static void TransformLeft(this Preset preset, Transform t)
    {
        Transform(t, preset.LeftOffset);
    }
    
    public static void TransformRight(this Preset preset, Transform t)
    {
        Transform(t, preset.RightOffset);
    }
    
    private static void Transform(Transform t, Pose offset)
    {
        t.Translate(offset.position);
        t.rotation *= offset.rotation;
    }
}