using System;
using System.Threading;
using BeatSaberOffsetMigrator.Shared;
using nkast.LibOVR;

namespace VRInputHelper.Oculus
{
    internal class Program
    {
        private readonly OvrSession _session;
        
        private readonly OVRHelperSharedMemoryManager _sharedMemoryManager = OVRHelperSharedMemoryManager.CreateWriteOnly();

        private readonly TimeSpan Delay = TimeSpan.FromMilliseconds(1000.0 / 360); // 360Hz (should be enough, also multiple of 72, 90, 120)

        private const OvrStatusBits TrackedFlags = OvrStatusBits.OrientationTracked | OvrStatusBits.OrientationValid | 
                                                   OvrStatusBits.PositionTracked | OvrStatusBits.PositionValid;
        
        private Program(OvrSession session)
        {
            _session = session;
            _session.SetTrackingOriginType(OvrTrackingOrigin.FloorLevel);
        }
        
        private void Run()
        {
            for (;;)
            {
                var a = _session.GetSessionStatus(out var sessionStatus);
                if (a < 0)
                {
                    Console.Error.WriteLine($"Failed to get session status, error code: {a}");
                }
                if (sessionStatus.ShouldQuit == OvrBool.True) break;
                if (sessionStatus.ShouldRecenter == OvrBool.True)
                {
                    Console.WriteLine("Recentering tracking origin");
                    _session.RecenterTrackingOrigin(OvrTrackingOrigin.FloorLevel);
                }

                var state = _session.GetTrackingState(0, OvrBool.False);
                var status = state.HandStatusFlags;
                
                ControllerPose controllerPose;
                if (status.Item0.HasFlag(TrackedFlags) && status.Item1.HasFlag(TrackedFlags))
                {
                    var poses = state.HandPoses;
                    controllerPose = new ControllerPose
                    {
                        valid = 1,
                        lposx = poses.Item0.ThePose.Position.X,
                        lposy = poses.Item0.ThePose.Position.Y,
                        lposz = - poses.Item0.ThePose.Position.Z,
                        lrotx = - poses.Item0.ThePose.Orientation.X,
                        lroty = - poses.Item0.ThePose.Orientation.Y,
                        lrotz = poses.Item0.ThePose.Orientation.Z,
                        lrotw = poses.Item0.ThePose.Orientation.W,
                        rposx = poses.Item1.ThePose.Position.X,
                        rposy = poses.Item1.ThePose.Position.Y,
                        rposz = - poses.Item1.ThePose.Position.Z,
                        rrotx = - poses.Item1.ThePose.Orientation.X,
                        rroty = - poses.Item1.ThePose.Orientation.Y,
                        rrotz = poses.Item1.ThePose.Orientation.Z,
                        rrotw = poses.Item1.ThePose.Orientation.W
                    };
                    
                    // Console.WriteLine($"({controllerPose.lposx}, {controllerPose.lposy}, {controllerPose.lposz}), ({controllerPose.rposx}, {controllerPose.rposy}, {controllerPose.rposz})");
                    
                }
                else
                {
#if DEBUG
                    Console.WriteLine("Not all controllers are tracking normally!");
#endif
                    controllerPose = new ControllerPose { valid = 0 };
                }
                
                _sharedMemoryManager.Write(ref controllerPose);
                
                
                Thread.Sleep(Delay);
            }
            
            var invalid = new ControllerPose { valid = 0 };
            _sharedMemoryManager.Write(ref invalid);
        }
        
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var clientCode = OvrClient.TryInitialize(new OvrInitParams { Flags = OvrInitFlags.Invisible }, out var ovrClient);
            if (clientCode < 0)
            {
                Console.Error.WriteLine($"Failed to initialize OVR client, error code: {clientCode}");
                return;
            }
            
            var sessionCode = ovrClient.TryCreateSession(out var session);
            if (sessionCode < 0)
            {
                Console.Error.WriteLine($"Failed to create OVR session, error code: {sessionCode}");
                ovrClient.Dispose();
                return;
            }
            
            var program = new Program(session);
            
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                var invalid = new ControllerPose { valid = 0 };
                program._sharedMemoryManager.Write(ref invalid);
                session.Dispose();
                ovrClient.Dispose();
            };
            
            program.Run();
        }
    }
}