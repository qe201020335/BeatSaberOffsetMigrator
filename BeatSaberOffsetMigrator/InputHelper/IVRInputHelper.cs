using UnityEngine;

namespace BeatSaberOffsetMigrator.InputHelper;

public interface IVRInputHelper
{
    string RuntimeName { get; }
    
    bool Supported { get; }
    
    bool Working { get; }
    
    string ReasonIfNotWorking { get; }
    
    Pose GetLeftVRControllerPose();
    
    Pose GetRightVRControllerPose();
}