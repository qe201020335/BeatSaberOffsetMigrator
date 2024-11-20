using UnityEngine;

namespace BeatSaberOffsetMigrator.InputHelper;

public class UnsupportedVRInputHelper: IVRInputHelper
{
    public string RuntimeName => "Unsupported";
    
    public bool Supported => false;
    
    public string ReasonIfNotWorking { get; set; } = "Unsupported runtime";
    
    public bool Working => false;
    
    public Pose GetLeftVRControllerPose()
    {
        return Pose.identity;
    }

    public Pose GetRightVRControllerPose()
    {
        return Pose.identity;
    }
}