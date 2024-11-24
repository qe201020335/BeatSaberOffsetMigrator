using BeatSaberOffsetMigrator.Utils;
using IPA.Config.Stores.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace BeatSaberOffsetMigrator.Models;


public struct Offset(Pose left, Pose right)
{
    public static readonly Offset Identity = new Offset(Pose.identity, Pose.identity);
    
    [JsonProperty("LeftPosition")]
    [SerializedName("LeftPosition")]
    public Vector3 LeftPosition = left.position;

    [JsonProperty("LeftRotationEuler")]
    [SerializedName("LeftRotationEuler")]
    public Vector3 LeftRotationEuler = PoseUtils.ClampAngle(left.rotation.eulerAngles);

    [JsonProperty("RightPosition")]
    [SerializedName("RightPosition")]
    public Vector3 RightPosition = right.position;

    [JsonProperty("RightRotationEuler")]
    [SerializedName("RightRotationEuler")]
    public Vector3 RightRotationEuler = PoseUtils.ClampAngle(right.rotation.eulerAngles);

    public Pose Left => new Pose(LeftPosition, Quaternion.Euler(LeftRotationEuler));

    public Pose Right => new Pose(RightPosition, Quaternion.Euler(RightRotationEuler));
}