using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Noise Setting/Perlin Noise")]
public class PerlinNoiseSetting : NoiseSettingBase
{
    public TransformNoiseParams[] PositionNoise = new TransformNoiseParams[3];
    public TransformNoiseParams[] OrientationNoise = new TransformNoiseParams[3];

    public override Vector3 GetPositionNoise(float timeSinceSignalStart)
    {
        return GetCombinedFilterResults(PositionNoise, timeSinceSignalStart, Vector3.zero);
    }

    public override Quaternion GetRotationNoise(float timeSinceSignalStart)
    {
        return Quaternion.Euler(GetCombinedFilterResults(OrientationNoise, timeSinceSignalStart, Vector3.zero));
    }

    public override void GetNoise(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot)
    {
        pos = GetPositionNoise(timeSinceSignalStart);
        rot = GetRotationNoise(timeSinceSignalStart);
    }

    public static Vector3 GetCombinedFilterResults(TransformNoiseParams[] noiseParams, float time, Vector3 timeOffsets)
    {
        Vector3 pos = Vector3.zero;
        if (noiseParams != null)
        {
            for (int i = 0; i < noiseParams.Length; ++i)
                pos += noiseParams[i].GetValueAt(time, timeOffsets);
        }
        return pos;
    }

    [Serializable]
    public struct TransformNoiseParams
    {
        [Tooltip("Noise definition for X-axis")]
        public NoiseParams X;
        [Tooltip("Noise definition for Y-axis")]
        public NoiseParams Y;
        [Tooltip("Noise definition for Z-axis")]
        public NoiseParams Z;

        public Vector3 GetValueAt(float time, Vector3 timeOffsets)
        {
            return new Vector3(
                X.GetValueAt(time, timeOffsets.x),
                Y.GetValueAt(time, timeOffsets.y),
                Z.GetValueAt(time, timeOffsets.z));
        }
    }

    [Serializable]
    public struct NoiseParams
    {
        [Tooltip("The frequency of noise for this channel.  Higher magnitudes vibrate faster.")]
        public float Frequency;

        [Tooltip("The amplitude of the noise for this channel.  Larger numbers vibrate higher.")]
        public float Amplitude;

        [Tooltip("If checked, then the amplitude and frequency will not be randomized.")]
        public bool Constant;

        public float GetValueAt(float time, float timeOffset)
        {
            float t = (Frequency * time) + timeOffset;
            if (Constant)
                return Mathf.Cos(t * 2 * Mathf.PI) * Amplitude * 0.5f;
            return (Mathf.PerlinNoise(t, 0f) - 0.5f) * Amplitude;
        }
    }
}