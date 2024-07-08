using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Noise Setting/Curve Noise")]
public class CurveNoiseSetting : NoiseSettingBase
{
    public AnimationCurve PositionNoise_X = new AnimationCurve();
    public AnimationCurve PositionNoise_Y = new AnimationCurve();
    public AnimationCurve PositionNoise_Z = new AnimationCurve();

    public AnimationCurve OrientationNoise_X = new AnimationCurve();
    public AnimationCurve OrientationNoise_Y = new AnimationCurve();
    public AnimationCurve OrientationNoise_Z = new AnimationCurve();

    public override void GetNoise(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot)
    {
        pos = GetPositionNoise(timeSinceSignalStart);
        rot = GetRotationNoise(timeSinceSignalStart);
    }

    public override Vector3 GetPositionNoise(float timeSinceSignalStart)
    {
        return new Vector3(PositionNoise_X.Evaluate(timeSinceSignalStart),
            PositionNoise_Y.Evaluate(timeSinceSignalStart),
            PositionNoise_Z.Evaluate(timeSinceSignalStart));
    }

    public override Quaternion GetRotationNoise(float timeSinceSignalStart)
    {
        return Quaternion.Euler(new Vector3(OrientationNoise_X.Evaluate(timeSinceSignalStart),
            OrientationNoise_Y.Evaluate(timeSinceSignalStart),
            OrientationNoise_Z.Evaluate(timeSinceSignalStart)));
    }
}