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

        private const int Delay = 1000 / 120; // 120Hz
        
        private Program(OvrSession session)
        {
            _session = session;
        }
        
        private void Run()
        {
            for (;;)
            {
                var poses = _session.GetTrackingState(0, OvrBool.False).HandPoses;
                var controllerPose = new ControllerPose
                {
                    valid = 1,
                    lposx = poses.Item0.ThePose.Position.X,
                    lposy = poses.Item0.ThePose.Position.Y,
                    lposz = poses.Item0.ThePose.Position.Z,
                    lrotx = poses.Item0.ThePose.Orientation.X,
                    lroty = poses.Item0.ThePose.Orientation.Y,
                    lrotz = poses.Item0.ThePose.Orientation.Z,
                    lrotw = poses.Item0.ThePose.Orientation.W,
                    rposx = poses.Item1.ThePose.Position.X,
                    rposy = poses.Item1.ThePose.Position.Y,
                    rposz = poses.Item1.ThePose.Position.Z,
                    rrotx = poses.Item1.ThePose.Orientation.X,
                    rroty = poses.Item1.ThePose.Orientation.Y,
                    rrotz = poses.Item1.ThePose.Orientation.Z,
                    rrotw = poses.Item1.ThePose.Orientation.W
                };
                
                _sharedMemoryManager.Write(ref controllerPose);
                
                Console.WriteLine($"({controllerPose.lposx}, {controllerPose.lposy}, {controllerPose.lposz}), ({controllerPose.rposx}, {controllerPose.rposy}, {controllerPose.rposz})");
                
                Thread.Sleep(Delay);
            }
        }
        
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var clientCode = OvrClient.TryInitialize(new OvrInitParams { Flags = OvrInitFlags.Invisible }, out var ovrClient);
            if (clientCode < 0)
            {
                Console.WriteLine($"Failed to initialize OVR client, error code: {clientCode}");
                return;
            }
            
            var sessionCode = ovrClient.TryCreateSession(out var session);
            if (sessionCode < 0)
            {
                Console.WriteLine($"Failed to create OVR session, error code: {sessionCode}");
                ovrClient.Dispose();
                return;
            }
            
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                session.Dispose();
                ovrClient.Dispose();
            };
            
            new Program(session).Run();
        }
    }
}