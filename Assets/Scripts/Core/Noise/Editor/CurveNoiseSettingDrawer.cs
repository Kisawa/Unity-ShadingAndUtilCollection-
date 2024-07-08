using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CurveNoiseSetting))]
public class CurveNoiseSettingDrawer : Editor
{
    const float labelWidth = 100;

    public float mPreviewTime = 1;
    public float mPreviewHeight = 5;

    CurveNoiseSetting self;

    SerializedProperty PositionNoise_XProp;
    SerializedProperty PositionNoise_YProp;
    SerializedProperty PositionNoise_ZProp;
    SerializedProperty OrientationNoise_XProp;
    SerializedProperty OrientationNoise_YProp;
    SerializedProperty OrientationNoise_ZProp;

    SampleCache positionCache = new SampleCache();
    SampleCache rotationCache = new SampleCache();

    private void OnEnable()
    {
        self = target as CurveNoiseSetting;
        PositionNoise_XProp = serializedObject.FindProperty("PositionNoise_X");
        PositionNoise_YProp = serializedObject.FindProperty("PositionNoise_Y");
        PositionNoise_ZProp = serializedObject.FindProperty("PositionNoise_Z");
        OrientationNoise_XProp = serializedObject.FindProperty("OrientationNoise_X");
        OrientationNoise_YProp = serializedObject.FindProperty("OrientationNoise_Y");
        OrientationNoise_ZProp = serializedObject.FindProperty("OrientationNoise_Z");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        Rect rect = EditorGUILayout.GetControlRect();
        float _previewTime = EditorGUI.Slider(rect, "Preview Time", mPreviewTime, 0.01f, 3f);
        rect = EditorGUILayout.GetControlRect();
        float _previewHeight = EditorGUI.Slider(rect, "Preview Height", mPreviewHeight, 1f, 10f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Param Changed");
            mPreviewTime = _previewTime;
            mPreviewHeight = _previewHeight;
        }
        EditorGUILayout.Separator();

        rect = EditorGUILayout.GetControlRect();
        EditorGUI.LabelField(rect, "Position Noise", EditorStyles.boldLabel);
        rect = EditorGUILayout.GetControlRect(true, mPreviewHeight * EditorGUIUtility.singleLineHeight);
        if (Event.current.type == EventType.Repaint)
        {
            positionCache.SnapshotSample(mPreviewTime, rect.size, self.PositionNoise_X, self.PositionNoise_Y, self.PositionNoise_Z);
            positionCache.DrawSamplePreview(rect);
        }
        DrawCurve("Position X", PositionNoise_XProp);
        DrawCurve("Position Y", PositionNoise_YProp);
        DrawCurve("Position Z", PositionNoise_ZProp);

        //GUILayout.Space(20);
        //rect = EditorGUILayout.GetControlRect();
        //EditorGUI.LabelField(rect, "Rotation Noise", EditorStyles.boldLabel);
        //rect = EditorGUILayout.GetControlRect(true, mPreviewHeight * EditorGUIUtility.singleLineHeight);
        //if (Event.current.type == EventType.Repaint)
        //{
        //    positionCache.SnapshotSample(mPreviewTime, rect.size, self.OrientationNoise_X, self.OrientationNoise_Y, self.OrientationNoise_Z);
        //    positionCache.DrawSamplePreview(rect);
        //}
        //DrawCurve("Rotation X", OrientationNoise_XProp);
        //DrawCurve("Rotation Y", OrientationNoise_YProp);
        //DrawCurve("Rotation Z", OrientationNoise_ZProp);
        serializedObject.ApplyModifiedProperties();
    }

    void DrawCurve(string displayName, SerializedProperty prop)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(displayName, GUILayout.Width(labelWidth));
        EditorGUILayout.PropertyField(prop, new GUIContent());
        EditorGUILayout.EndHorizontal();
    }

    class SampleCache
    {
        private List<Vector3> mSampleCurveX = new List<Vector3>();
        private List<Vector3> mSampleCurveY = new List<Vector3>();
        private List<Vector3> mSampleCurveZ = new List<Vector3>();
        private List<Vector3> mSampleNoise = new List<Vector3>();

        public void SnapshotSample(float time, Vector2 areaSize, AnimationCurve x, AnimationCurve y, AnimationCurve z)
        {
            // These values give a smoother curve, more-or-less fitting in the window
            int numSamples = Mathf.RoundToInt(areaSize.x);
            const float signalScale = 1.2f;

            float maxVal = 0;
            if (x != null)
            {
                for (int i = 0; i < x.length; i++)
                {
                    maxVal = Mathf.Max(maxVal, x.keys[i].value * signalScale);
                }
            }
            if (y != null)
            {
                for (int i = 0; i < y.length; i++)
                {
                    maxVal = Mathf.Max(maxVal, y.keys[i].value * signalScale);
                }
            }
            if (z != null)
            {
                for (int i = 0; i < z.length; i++)
                {
                    maxVal = Mathf.Max(maxVal, z.keys[i].value * signalScale);
                }
            }
            mSampleNoise.Clear();
            for (int i = 0; i < numSamples; ++i)
            {
                float t = (float)i / (numSamples - 1) * time;
                Vector3 p = new Vector3(x.Evaluate(t), y.Evaluate(t), z.Evaluate(t));
                mSampleNoise.Add(p);
            }
            mSampleCurveX.Clear();
            mSampleCurveY.Clear();
            mSampleCurveZ.Clear();
            float halfHeight = areaSize.y / 2;
            float yOffset = halfHeight;
            for (int i = 0; i < numSamples; ++i)
            {
                float t = (float)i / (numSamples - 1);
                Vector3 p = mSampleNoise[i];
                mSampleCurveX.Add(new Vector3(areaSize.x * t, halfHeight * Mathf.Clamp(-p.x / maxVal, -1, 1) + yOffset, 0));
                mSampleCurveY.Add(new Vector3(areaSize.x * t, halfHeight * Mathf.Clamp(-p.y / maxVal, -1, 1) + yOffset, 0));
                mSampleCurveZ.Add(new Vector3(areaSize.x * t, halfHeight * Mathf.Clamp(-p.z / maxVal, -1, 1) + yOffset, 0));
            }
        }

        public void DrawSamplePreview(Rect r)
        {
            EditorGUI.DrawRect(r, Color.black);
            var oldMatrix = Handles.matrix;
            Handles.matrix = Handles.matrix * Matrix4x4.Translate(r.position);
            Handles.color = new Color(1, 0.5f, 0, 0.8f);
            Handles.DrawPolyLine(mSampleCurveX.ToArray());
            Handles.color = new Color(0, 1, 0, 0.8f);
            Handles.DrawPolyLine(mSampleCurveY.ToArray());
            Handles.color = new Color(0, 0.5f, 1, 0.8f);
            Handles.DrawPolyLine(mSampleCurveZ.ToArray());
            Handles.color = Color.black;
            Handles.DrawLine(new Vector3(1, 0, 0), new Vector3(r.width, 0, 0));
            Handles.DrawLine(new Vector3(0, r.height, 0), new Vector3(r.width, r.height, 0));
            Handles.matrix = oldMatrix;
        }
    }
}
