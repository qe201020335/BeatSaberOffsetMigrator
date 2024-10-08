﻿using UnityEngine;

namespace BeatSaberOffsetMigrator.InputHelper;

public class UnsupportedVRInputHelper: IVRInputHelper
{
    public string RuntimeName => "Unsupported";
    
    public bool Supported => false;
    
    public Pose GetLeftVRControllerPose()
    {
        return Pose.identity;
    }

    public Pose GetRightVRControllerPose()
    {
        return Pose.identity;
    }
}