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

        public virtual Vector3 LeftOffsetPosition { get; set; } = Vector3.zero;
        
        protected virtual Vector3 LeftOffsetRotationEuler { get; set; } = Vector3.zero;
       
        public virtual Vector3 RightOffsetPosition { get; set; } = Vector3.zero;
        
        protected virtual Vector3 RightOffsetRotationEuler { get; set; } = Vector3.zero;
        
        [Ignore]
        public virtual Quaternion LeftOffsetRotation
        {
            get => Quaternion.Euler(LeftOffsetRotationEuler);
            set => LeftOffsetRotationEuler = PoseUtils.ClampAngle(value.eulerAngles);
        }
        
        [Ignore]
        public virtual Quaternion RightOffsetRotation
        {
            get => Quaternion.Euler(RightOffsetRotationEuler);
            set => RightOffsetRotationEuler = PoseUtils.ClampAngle(value.eulerAngles);
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