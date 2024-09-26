using System;
using BeatSaberOffsetMigrator.Shared;
using UnityEngine;
using Zenject;

namespace BeatSaberOffsetMigrator.InputHelper;

public class OculusVRInputHelper: IVRInputHelper, ITickable
{
    public string RuntimeName => "OculusVR";
    public bool Supported => true;
    
    private readonly OVRHelperSharedMemoryManager _sharedMemoryManager = OVRHelperSharedMemoryManager.CreateReadOnly();
    
    private Pose _leftPose = Pose.identity;
    
    private Pose _rightPose = Pose.identity;
    
    private ControllerPose _poses;
    
    void ITickable.Tick()
    {
        _sharedMemoryManager.Read(ref _poses);
        if (_poses.valid != 1) return;
        
        _leftPose = new Pose
        {
            position = new Vector3(_poses.lposx, _poses.lposy, _poses.lposz),
            rotation = new Quaternion(_poses.lrotx, _poses.lroty, _poses.lrotz, _poses.lrotw)
        };
        
        _rightPose = new Pose
        {
            position = new Vector3(_poses.rposx, _poses.rposy, _poses.rposz),
            rotation = new Quaternion(_poses.rrotx, _poses.rroty, _poses.rrotz, _poses.rrotw)
        };
    }
    
    public Pose GetLeftVRControllerPose()
    {
        return _leftPose;
    }

    public Pose GetRightVRControllerPose()
    {
        return _rightPose;
    }
}