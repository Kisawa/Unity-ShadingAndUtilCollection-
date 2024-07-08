using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CustomTimeline
{
    [CustomEditor(typeof(ScreenDrawerAsset))]
    public class ScreenDrawerAssetEditor : UnityEditor.Editor
    {
        SerializedProperty curveProp;
        SerializedProperty EventTypeProp;
        SerializedProperty TilingOffsetProp;
        SerializedProperty DSRProp;
        SerializedProperty DSRDepthProp;
        SerializedProperty DSRFOVFactorProp;
        SerializedProperty DSRPositionFactorProp;
        SerializedProperty DSREularAngleFactorProp;

        private void OnEnable()
        {
            curveProp = serializedObject.FindProperty("curve");
            SerializedProperty settingProp = serializedObject.FindProperty("setting");
            EventTypeProp = settingProp.FindPropertyRelative("EventType");
            TilingOffsetProp = settingProp.FindPropertyRelative("TilingOffset");
            DSRProp = settingProp.FindPropertyRelative("DSR");
            DSRDepthProp = settingProp.FindPropertyRelative("DSRDepth");
            DSRFOVFactorProp = settingProp.FindPropertyRelative("DSRFOVFactor");
            DSRPositionFactorProp = settingProp.FindPropertyRelative("DSRPositionFactor");
            DSREularAngleFactorProp = settingProp.FindPropertyRelative("DSREularAngleFactor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(curveProp, new GUIContent(), GUILayout.Width(EditorGUIUtility.currentViewWidth - 120));
            if (GUILayout.Button("Inv"))
            {
                float min = 0, max = 0;
                float[] inTangent = new float[curveProp.animationCurveValue.length];
                float[] outTangent = new float[curveProp.animationCurveValue.length];
                for (int i = 0; i < curveProp.animationCurveValue.length; i++)
                {
                    Keyframe keyframe = curveProp.animationCurveValue.keys[i];
                    min = Mathf.Min(min, keyframe.value);
                    max = Mathf.Max(max, keyframe.value);
                    inTangent[i] = keyframe.inTangent;
                    outTangent[i] = keyframe.outTangent;
                }
                Keyframe[] keyframes = curveProp.animationCurveValue.keys;
                for (int i = 0; i < keyframes.Length; i++)
                {
                    Keyframe keyframe = keyframes[i];
                    keyframe.value = remap(keyframe.value, min, max, max, min);
                    keyframe.inTangent = -inTangent[keyframes.Length - i - 1];
                    keyframe.outTangent = -outTangent[keyframes.Length - i - 1];
                    keyframes[i] = keyframe;
                }
                curveProp.animationCurveValue = new AnimationCurve(keyframes);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            EditorGUILayout.PropertyField(EventTypeProp);

            GUILayout.Space(10);
            Rect rect = EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            Vector4 tilingOffset = MaterialEditor.TextureScaleOffsetProperty(rect, TilingOffsetProp.vector4Value);
            if (EditorGUI.EndChangeCheck())
                TilingOffsetProp.vector4Value = tilingOffset;
            GUILayout.Space(40);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            EditorGUILayout.PropertyField(DSRProp);
            GUILayout.Space(10);
            EditorGUILayout.PropertyField(DSRDepthProp);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("DSR Factor:");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(DSRFOVFactorProp);
            EditorGUILayout.PropertyField(DSRPositionFactorProp);
            EditorGUILayout.PropertyField(DSREularAngleFactorProp);
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        static float remap(float num, float inMin, float inMax, float outMin, float outMax)
        {
            return outMin + (num - inMin) * (outMax - outMin) / (inMax - inMin);
        }
    }
}