using System;
using Newtonsoft.Json;
using UnityEngine;

namespace BeatSaberOffsetMigrator.EO;

public class Preset
{
    [JsonProperty("LeftSaberZOffset")]
    private float _leftZOffset;
    private float LeftZOffset => _leftZOffset * 0.01f;

    [JsonProperty("LeftSaberPivotPosition")]
    private Vector3 _leftPivotPos;
    private Vector3 LeftPivotPos => _leftPivotPos * 0.01f;

    [JsonProperty("LeftSaberRotationEuler")]
    private Vector3 LeftRotEuler { get; set; }
    private Quaternion LeftRotation => Quaternion.Euler(LeftRotEuler);

    [JsonProperty("RightSaberZOffset")]
    private float _rightZOffset;
    private float RightZOffset => _rightZOffset * 0.01f;

    [JsonProperty("RightSaberPivotPosition")]
    private Vector3 _rightPivotPos;
    private Vector3 RightPivotPos => _rightPivotPos * 0.01f;

    [JsonProperty("RightSaberRotationEuler")]
    private Vector3 RightRotEuler { get; set; }
    private Quaternion RightRotation => Quaternion.Euler(RightRotEuler);

    [JsonIgnore]
    private Pose? _leftOffset = null;

    [JsonIgnore]
    public Pose LeftOffset
    {
        get
        {
            if (_leftOffset == null)
            {
                var pos = LeftPivotPos + LeftRotation * new Vector3(0, 0, LeftZOffset);
                _leftOffset = new Pose(pos, LeftRotation);
            }

            return _leftOffset.Value;
        }
    }
    
    [JsonIgnore]
    private Pose? _rightOffset = null;
    
    [JsonIgnore]
    public Pose RightOffset
    {
        get
        {
            if (_rightOffset == null)
            {
                var pos = RightPivotPos + RightRotation * new Vector3(0, 0, RightZOffset);
                _rightOffset = new Pose(pos, RightRotation);
            }

            return _rightOffset.Value;
        }
    }
    
}