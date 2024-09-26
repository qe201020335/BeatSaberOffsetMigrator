using System.Runtime.InteropServices;

namespace BeatSaberOffsetMigrator.Shared;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct ControllerPose
{
    public int valid;
    
    public float lposx;
    public float lposy;
    public float lposz;
    public float lrotx;
    public float lroty;
    public float lrotz;
    public float lrotw;
    
    public float rposx;
    public float rposy;
    public float rposz;
    public float rrotx;
    public float rroty;
    public float rrotz;
    public float rrotw;
}