using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
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
            set => LeftOffsetRotationEuler = Utils.ClampAngle(value.eulerAngles);
        }
        
        [Ignore]
        public virtual Quaternion RightOffsetRotation
        {
            get => Quaternion.Euler(RightOffsetRotationEuler);
            set => RightOffsetRotationEuler = Utils.ClampAngle(value.eulerAngles);
        }
    }
}