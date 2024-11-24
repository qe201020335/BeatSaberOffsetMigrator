using System;
using System.Runtime.CompilerServices;
using BeatSaberOffsetMigrator.Utils;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Utilities.Async;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace BeatSaberOffsetMigrator.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; } = null!;

        public virtual bool ApplyOffset { get; set; } = false;

        protected virtual Vector3 LeftOffsetPosition { get; set; } = Vector3.zero;
        
        protected virtual Vector3 LeftOffsetRotationEuler { get; set; } = Vector3.zero;
       
        protected virtual Vector3 RightOffsetPosition { get; set; } = Vector3.zero;
        
        protected virtual Vector3 RightOffsetRotationEuler { get; set; } = Vector3.zero;

        protected virtual Vector3 LeftUnityOffsetPosition { get; set; } = Vector3.zero;
        
        protected virtual Vector3 LeftUnityOffsetRotationEuler { get; set; } = Vector3.zero;
        
        protected virtual Vector3 RightUnityOffsetPosition { get; set; } = Vector3.zero;
        
        protected virtual Vector3 RightUnityOffsetRotationEuler { get; set; } = Vector3.zero;
        
        
        [Ignore]
        public Pose LeftOffset
        {
            get => new Pose(LeftOffsetPosition, Quaternion.Euler(LeftOffsetRotationEuler));
            set
            {
                LeftOffsetPosition = value.position;
                LeftOffsetRotationEuler = PoseUtils.ClampAngle(value.rotation.eulerAngles);
            }
        }
        
        [Ignore]
        public Pose RightOffset
        {
            get => new Pose(RightOffsetPosition, Quaternion.Euler(RightOffsetRotationEuler));
            set
            {
                RightOffsetPosition = value.position;
                RightOffsetRotationEuler = PoseUtils.ClampAngle(value.rotation.eulerAngles);
            }
        }

        [Ignore]
        public Pose LeftUnityOffset
        {
            get => new Pose(LeftUnityOffsetPosition, Quaternion.Euler(LeftUnityOffsetRotationEuler));
            set
            {
                LeftUnityOffsetPosition = value.position;
                LeftUnityOffsetRotationEuler = PoseUtils.ClampAngle(value.rotation.eulerAngles);
            }
        }
        
        [Ignore]
        public Pose RightUnityOffset
        {
            get => new Pose(RightUnityOffsetPosition, Quaternion.Euler(RightUnityOffsetRotationEuler));
            set
            {
                RightUnityOffsetPosition = value.position;
                RightUnityOffsetRotationEuler = PoseUtils.ClampAngle(value.rotation.eulerAngles);
            }
        }

        internal event Action? ConfigDidChange;

        private void HandleConfigChanged()
        {
            Plugin.Log.Trace("Config changed, broadcasting event");
            var listener = ConfigDidChange;
            UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                listener?.Invoke();
            });
        }
        
        public virtual void Changed()
        {
            HandleConfigChanged();
        }

        public virtual void OnReload()
        {
            HandleConfigChanged();
        }
    }
}