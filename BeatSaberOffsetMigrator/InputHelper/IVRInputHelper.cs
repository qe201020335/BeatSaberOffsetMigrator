using UnityEngine;

namespace BeatSaberOffsetMigrator.InputHelper;

public interface IVRInputHelper
{
    string RuntimeName { get; }
    
    bool Supported { get; }
    
    Pose GetLeftVRControllerPose();
    
    Pose GetRightVRControllerPose();
}