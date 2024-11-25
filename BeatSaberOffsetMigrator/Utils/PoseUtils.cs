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
    
    public static string Format(this Pose pose)
    {
        var euler = ClampAngle(pose.rotation.eulerAngles);
        return $"({pose.position.x:F3}, {pose.position.y:F3}, {pose.position.z:F3}) " + 
               $"({euler.x:F1}, {euler.y:F1}, {euler.z:F1})";
    }

    public static Pose Mirror(this Pose pose)
    {
        return new Pose(new Vector3(-pose.position.x, pose.position.y, pose.position.z),
            new Quaternion(pose.rotation.x, -pose.rotation.y, -pose.rotation.z, pose.rotation.w));
    }
}