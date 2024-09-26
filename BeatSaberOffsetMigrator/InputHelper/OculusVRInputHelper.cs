using UnityEngine;
using Zenject;

namespace BeatSaberOffsetMigrator.InputHelper;

public class OculusVRInputHelper: IVRInputHelper, ITickable
{
    public string RuntimeName => "OculusVR";
    public bool Supported => true;
    
    private Pose _leftPose = Pose.identity;
    
    private Pose _rightPose = Pose.identity;
    
    void ITickable.Tick()
    {
        var lPose = OVRPlugin.GetNodePose(OVRPlugin.Node.HandLeft, OVRPlugin.Step.Render).ToOVRPose();
        var rPose = OVRPlugin.GetNodePose(OVRPlugin.Node.HandRight, OVRPlugin.Step.Render).ToOVRPose();
        _leftPose = new Pose(lPose.position, lPose.orientation);
        _rightPose = new Pose(rPose.position, rPose.orientation);
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