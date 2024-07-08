using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NoiseSettingBase : ScriptableObject
{
    public abstract Vector3 GetPositionNoise(float timeSinceSignalStart);

    public abstract Quaternion GetRotationNoise(float timeSinceSignalStart);

    public abstract void GetNoise(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot);
}