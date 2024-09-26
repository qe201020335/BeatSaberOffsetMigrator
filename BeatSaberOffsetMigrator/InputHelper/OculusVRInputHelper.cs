using System;
using BeatSaberOffsetMigrator.Shared;
using UnityEngine;
using Zenject;

namespace BeatSaberOffsetMigrator.InputHelper;

public class OculusVRInputHelper: IVRInputHelper, ITickable, IDisposable
{
    public string RuntimeName => "OculusVR";
    public bool Supported => true;
    
    private readonly OVRHelperSharedMemoryManager _sharedMemoryManager = OVRHelperSharedMemoryManager.CreateReadOnly();
    
    private Pose _leftPose = Pose.identity;
    
    private Pose _rightPose = Pose.identity;
    
    private ControllerPose poses = default;

    // public OculusVRInputHelper()
    // {
    //     var initParams = new OvrInitParams
    //     {
    //         Flags = OvrInitFlags.Invisible
    //     };
    //     var clientResult = OvrClient.TryInitialize(initParams, out var ovrClient);
    //     if (clientResult < 0)
    //     {
    //         Plugin.Log.Error($"Failed to initialize OVR client, error code: {clientResult}");
    //         return;
    //     }
    //     
    //     // var ovrClient = new OvrClient();  // Hacky way of not initialize the client because unity already did it with its OpenXR
    //     
    //     var sessionResult = ovrClient.TryCreateSession(out var session);
    //     if (sessionResult < 0)
    //     {
    //         Plugin.Log.Error($"Failed to create OVR session, error code: {sessionResult}");
    //         return;
    //     }
    //     
    //     _client = ovrClient;
    //     _session = session;
    // }
    
    void IDisposable.Dispose()
    {
        // _session?.Dispose();
        // _client?.Dispose();
    }
    
    void ITickable.Tick()
    {
        // if (_session == null) return;
        // var poses = _session.GetTrackingState(0, OvrBool.False).HandPoses;

        _sharedMemoryManager.Read(ref poses);
        if (poses.valid != 1) return;
        
        _leftPose = new Pose
        {
            position = new Vector3(poses.lposx, poses.lposy, poses.lposz),
            rotation = new Quaternion(poses.lrotx, poses.lroty, poses.lrotz, poses.lrotw)
        };
        
        _rightPose = new Pose
        {
            position = new Vector3(poses.rposx, poses.rposy, poses.rposz),
            rotation = new Quaternion(poses.rrotx, poses.rroty, poses.rrotz, poses.rrotw)
        };
        
        // _leftPose = ConvertPose(poses.Item0.ThePose);
        // _rightPose = ConvertPose(poses.Item1.ThePose);
    }
    
    public Pose GetLeftVRControllerPose()
    {
        return _leftPose;
    }

    public Pose GetRightVRControllerPose()
    {
        return _rightPose;
    }

    // private static Pose ConvertPose(OvrPosef ovrPose)
    // {
    //     return new Pose
    //     {
    //         position = new Vector3(ovrPose.Position.X, ovrPose.Position.Y, ovrPose.Position.Z),
    //         rotation = new Quaternion(ovrPose.Orientation.X, ovrPose.Orientation.Y, ovrPose.Orientation.Z, ovrPose.Orientation.W)
    //     };
    // }
}