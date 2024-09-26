using System;
using nkast.LibOVR;
using UnityEngine;
using Zenject;

namespace BeatSaberOffsetMigrator.InputHelper;

public class OculusVRInputHelper: IVRInputHelper, ITickable, IDisposable
{
    private readonly OvrSession? _session;
    private readonly OvrClient? _client;
    public string RuntimeName => "OculusVR";
    public bool Supported => true;
    
    private Pose _leftPose = Pose.identity;
    
    private Pose _rightPose = Pose.identity;

    public OculusVRInputHelper()
    {
        var initParams = new OvrInitParams
        {
            Flags = OvrInitFlags.Invisible
        };
        var clientResult = OvrClient.TryInitialize(initParams, out var ovrClient);
        if (clientResult != 0)
        {
            Plugin.Log.Error($"Failed to initialize OVR client, error code: {clientResult}");
            return;
        }
        
        var sessionResult = ovrClient.TryCreateSession(out var session);
        if (sessionResult != 0)
        {
            Plugin.Log.Error($"Failed to create OVR session, error code: {sessionResult}");
            return;
        }
        
        _client = ovrClient;
        _session = session;
    }
    
    void IDisposable.Dispose()
    {
        _session?.Dispose();
        _client?.Dispose();
    }
    
    void ITickable.Tick()
    {
        if (_session == null) return;
        var poses = _session.GetTrackingState(0, OvrBool.False).HandPoses;
        
        _leftPose = ConvertPose(poses.Item0.ThePose);
        _rightPose = ConvertPose(poses.Item1.ThePose);
    }
    
    public Pose GetLeftVRControllerPose()
    {
        return _leftPose;
    }

    public Pose GetRightVRControllerPose()
    {
        return _rightPose;
    }

    private static Pose ConvertPose(OvrPosef ovrPose)
    {
        return new Pose
        {
            position = new Vector3(ovrPose.Position.X, ovrPose.Position.Y, ovrPose.Position.Z),
            rotation = new Quaternion(ovrPose.Orientation.X, ovrPose.Orientation.Y, ovrPose.Orientation.Z, ovrPose.Orientation.W)
        };
    }
}