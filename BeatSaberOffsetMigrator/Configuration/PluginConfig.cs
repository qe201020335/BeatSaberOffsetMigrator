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
        
        [SerializedName("LeftOffsetRotation")]
        protected virtual Quat _LeftOffsetRotation { get; set; } = new Quat(Quaternion.identity);
       
        public virtual Vector3 RightOffsetPosition { get; set; } = Vector3.zero;
        
        [SerializedName("RightOffsetRotation")]
        protected virtual Quat _RightOffsetRotation { get; set; } = new Quat(Quaternion.identity);
        
        [Ignore]
        public virtual Quaternion LeftOffsetRotation
        {
            get => _LeftOffsetRotation.ToQuaternion();
            set => _LeftOffsetRotation = new Quat(value);
        }
        
        [Ignore]
        public virtual Quaternion RightOffsetRotation
        {
            get => _RightOffsetRotation.ToQuaternion();
            set => _RightOffsetRotation = new Quat(value);
        }
    }

    internal struct Quat
    {
        public float x;
        public float y;
        public float z;
        public float w;
        
        public Quat(Quaternion quat)
        {
            x = quat.x;
            y = quat.y;
            z = quat.z;
            w = quat.w;
        }
        
        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }
}