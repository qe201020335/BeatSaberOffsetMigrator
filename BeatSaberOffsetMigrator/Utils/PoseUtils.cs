using UnityEngine;

namespace BeatSaberOffsetMigrator.Utils;

public static class PoseUtils
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
        
    public static Transform Offset(this Transform transform, Pose offset)
    {
        transform.Translate(offset.position);
        transform.Rotate(offset.rotation.eulerAngles);
        return transform;
    }
}