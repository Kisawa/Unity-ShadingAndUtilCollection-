using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(PerlinNoiseSetting))]
public class PerlinNoiseSettingDrawer : Editor
{
    private const float vSpace = 2;
    private const float hSpace = 3;

    PerlinNoiseSetting Target { get { return target as PerlinNoiseSetting; } }

    // Needed for accessing string names of fields
#pragma warning disable 0649 // assigned but never used
    private PerlinNoiseSetting.TransformNoiseParams tpDef;
    private PerlinNoiseSetting.NoiseParams npDef;

    public float mPreviewTime = 1;
    public float mPreviewHeight = 5;
    private float mNoiseOffsetBase = 0;
    private float mNoiseOffset = 0;
    private bool mAnimatedPreview = false;
    GUIContent mAnimatedLabel = new GUIContent("Animated", "Animate the noise signal preview");

    SerializedProperty PositionNoiseProp;
    SerializedProperty OrientationNoiseProp;

    private ReorderableList[] mPosChannels;
    private ReorderableList[] mRotChannels;
    private static GUIContent[] mPoslabels = new GUIContent[]
    {
            new GUIContent("Position X"),
            new GUIContent("Position Y"),
            new GUIContent("Position Z")
    };
    private static GUIContent[] mRotlabels = new GUIContent[]
    {
            new GUIContent("Rotation X"),
            new GUIContent("Rotation Y"),
            new GUIContent("Rotation Z")
    };
    private static bool[] mPosExpanded = new bool[3];
    private static bool[] mRotExpanded = new bool[3];

    private void OnEnable()
    {
        PositionNoiseProp = serializedObject.FindProperty("PositionNoise");
        OrientationNoiseProp = serializedObject.FindProperty("OrientationNoise");
        mNoiseOffsetBase = Time.realtimeSinceStartup;
        mNoiseOffset = 0;
    }

    List<string> mExcluded = new List<string>();

    /// <summary>Get the property names to exclude in the inspector.</summary>
    /// <param name="excluded">Add the names to this list</param>
    protected void GetExcludedPropertiesInInspector(List<string> excluded)
    {
        excluded.Add("m_Script");
        excluded.Add(FieldPath(x => Target.PositionNoise));
        excluded.Add(FieldPath(x => Target.OrientationNoise));
    }

    protected void BeginInspector()
    {
        serializedObject.Update();
        mExcluded.Clear();
        GetExcludedPropertiesInInspector(mExcluded);
    }

    public override void OnInspectorGUI()
    {
        if (mPosChannels == null && PositionNoiseProp != null)
            mPosChannels = SetupReorderableLists(PositionNoiseProp, mPoslabels);
        if (mRotChannels == null && OrientationNoiseProp != null)
            mRotChannels = SetupReorderableLists(OrientationNoiseProp, mRotlabels);

        BeginInspector();

        Rect r = EditorGUILayout.GetControlRect();
        EditorGUI.BeginChangeCheck();
        float _previewTime = EditorGUI.Slider(r, "Preview Time", mPreviewTime, 0.01f, 3f);
        r = EditorGUILayout.GetControlRect();
        float labelWidth = GUI.skin.label.CalcSize(mAnimatedLabel).x + EditorGUIUtility.singleLineHeight;
        r.width -= labelWidth + hSpace;
        float _previewHeight = EditorGUI.Slider(r, "Preview Height", mPreviewHeight, 1f, 10f);
        r.x += r.width + hSpace; r.width = labelWidth;
        mAnimatedPreview = EditorGUI.ToggleLeft(r, mAnimatedLabel, mAnimatedPreview);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Param Changed");
            mPreviewTime = _previewTime;
            mPreviewHeight = _previewHeight;
        }
        EditorGUILayout.Separator();

        r = EditorGUILayout.GetControlRect();
        EditorGUI.LabelField(r, "Position Noise - amplitudes are in Distance units", EditorStyles.boldLabel);
        r = EditorGUILayout.GetControlRect(true, mPreviewHeight * EditorGUIUtility.singleLineHeight);
        if (Event.current.type == EventType.Repaint)
        {
            mSampleCachePos.SnapshotSample(mPreviewTime, r.size, Target.PositionNoise, mNoiseOffset, mAnimatedPreview);
            mSampleCachePos.DrawSamplePreview(r, 7);
        }
        for (int i = 0; i < mPosChannels.Length; ++i)
        {
            r = EditorGUILayout.GetControlRect();
            mPosExpanded[i] = EditorGUI.Foldout(r, mPosExpanded[i], mPoslabels[i], true);
            if (mPosExpanded[i])
            {
                r = EditorGUILayout.GetControlRect(true, mPreviewHeight * EditorGUIUtility.singleLineHeight);
                if (Event.current.type == EventType.Repaint)
                    mSampleCachePos.DrawSamplePreview(r, 1 << i);
                mPosChannels[i].DoLayoutList();
            }
        }
        EditorGUILayout.Separator();

        //r = EditorGUILayout.GetControlRect();
        //EditorGUI.LabelField(r, "Rotation Noise - amplitude units are degrees", EditorStyles.boldLabel);
        //r = EditorGUILayout.GetControlRect(true, mPreviewHeight * EditorGUIUtility.singleLineHeight);
        //if (Event.current.type == EventType.Repaint)
        //{
        //    mSampleCacheRot.SnapshotSample(mPreviewTime, r.size, Target.OrientationNoise, mNoiseOffset, mAnimatedPreview);
        //    mSampleCacheRot.DrawSamplePreview(r, 7);
        //}
        //for (int i = 0; i < mPosChannels.Length; ++i)
        //{
        //    r = EditorGUILayout.GetControlRect();
        //    mRotExpanded[i] = EditorGUI.Foldout(r, mRotExpanded[i], mRotlabels[i], true);
        //    if (mRotExpanded[i])
        //    {
        //        r = EditorGUILayout.GetControlRect(true, mPreviewHeight * EditorGUIUtility.singleLineHeight);
        //        if (Event.current.type == EventType.Repaint)
        //            mSampleCacheRot.DrawSamplePreview(r, 1 << i);
        //        mRotChannels[i].DoLayoutList();
        //    }
        //}

        serializedObject.ApplyModifiedProperties();

        // Make it live!
        if (mAnimatedPreview && Event.current.type == EventType.Repaint)
        {
            mNoiseOffset += Time.realtimeSinceStartup - mNoiseOffsetBase;
            Repaint();
        }
        mNoiseOffsetBase = Time.realtimeSinceStartup;
    }

    class SampleCache
    {
        private List<Vector3> mSampleCurveX = new List<Vector3>();
        private List<Vector3> mSampleCurveY = new List<Vector3>();
        private List<Vector3> mSampleCurveZ = new List<Vector3>();
        private List<Vector3> mSampleNoise = new List<Vector3>();

        public void SnapshotSample(float time, Vector2 areaSize, PerlinNoiseSetting.TransformNoiseParams[] signal, float noiseOffset, bool animated)
        {
            // These values give a smoother curve, more-or-less fitting in the window
            int numSamples = Mathf.RoundToInt(areaSize.x);
            if (animated)
                numSamples *= 2;
            const float signalScale = 0.75f;

            float maxVal = 0;
            for (int i = 0; i < signal.Length; ++i)
            {
                maxVal = Mathf.Max(maxVal, Mathf.Abs(signal[i].X.Amplitude * signalScale));
                maxVal = Mathf.Max(maxVal, Mathf.Abs(signal[i].Y.Amplitude * signalScale));
                maxVal = Mathf.Max(maxVal, Mathf.Abs(signal[i].Z.Amplitude * signalScale));
            }
            mSampleNoise.Clear();
            for (int i = 0; i < numSamples; ++i)
            {
                float t = (float)i / (numSamples - 1) * time + noiseOffset;
                Vector3 p = PerlinNoiseSetting.GetCombinedFilterResults(signal, t, Vector3.zero);
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

        public void DrawSamplePreview(Rect r, int channelMask)
        {
            EditorGUI.DrawRect(r, Color.black);
            var oldMatrix = Handles.matrix;
            Handles.matrix = Handles.matrix * Matrix4x4.Translate(r.position);
            if ((channelMask & 1) != 0)
            {
                Handles.color = new Color(1, 0.5f, 0, 0.8f);
                Handles.DrawPolyLine(mSampleCurveX.ToArray());
            }
            if ((channelMask & 2) != 0)
            {
                Handles.color = new Color(0, 1, 0, 0.8f);
                Handles.DrawPolyLine(mSampleCurveY.ToArray());
            }
            if ((channelMask & 4) != 0)
            {
                Handles.color = new Color(0, 0.5f, 1, 0.8f);
                Handles.DrawPolyLine(mSampleCurveZ.ToArray());
            }
            Handles.color = Color.black;
            Handles.DrawLine(new Vector3(1, 0, 0), new Vector3(r.width, 0, 0));
            Handles.DrawLine(new Vector3(0, r.height, 0), new Vector3(r.width, r.height, 0));
            Handles.matrix = oldMatrix;
        }
    }
    SampleCache mSampleCachePos = new SampleCache();
    SampleCache mSampleCacheRot = new SampleCache();

    private ReorderableList[] SetupReorderableLists(
        SerializedProperty property, GUIContent[] titles)
    {
        ReorderableList[] lists = new ReorderableList[3];
        for (int i = 0; i < 3; ++i)
            lists[i] = SetupReorderableList(property, i, new GUIContent("Components"));
        return lists;
    }

    private ReorderableList SetupReorderableList(
        SerializedProperty property, int channel, GUIContent title)
    {
        ChannelList list = new ChannelList(
            property.serializedObject, property, channel, title);

        list.drawHeaderCallback = (Rect rect) =>
        {
            GUIContent steadyLabel = new GUIContent("(non-random wave if checked)");
            float steadyLabelWidth = GUI.skin.label.CalcSize(steadyLabel).x;

            Rect r = rect;
            EditorGUI.LabelField(r, list.mTitle);
            r.x = rect.x + rect.width - steadyLabelWidth; r.width = steadyLabelWidth;
            EditorGUI.LabelField(r, steadyLabel);
        };

        list.drawElementCallback
            = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                switch (list.mChannel)
                {
                    case 0: DrawNoiseChannel(rect, element.FindPropertyRelative("X")); break;
                    case 1: DrawNoiseChannel(rect, element.FindPropertyRelative("Y")); break;
                    case 2: DrawNoiseChannel(rect, element.FindPropertyRelative("Z")); break;
                    default: break;
                }
            };

        list.onAddCallback = (ReorderableList l) =>
        {
            var index = l.serializedProperty.arraySize;
            ++l.serializedProperty.arraySize;
            SerializedProperty p = l.serializedProperty.GetArrayElementAtIndex(index);
            ClearComponent(p.FindPropertyRelative("X"));
            ClearComponent(p.FindPropertyRelative("Y"));
            ClearComponent(p.FindPropertyRelative("Z"));
        };

        list.onRemoveCallback = (ReorderableList l) =>
        {
            // Can't just delete because the component arrays are connected
            SerializedProperty p = l.serializedProperty.GetArrayElementAtIndex(l.index);
            bool IsClear
                = (list.mChannel == 0 || IsClearComponent(p.FindPropertyRelative("X")))
                && (list.mChannel == 1 || IsClearComponent(p.FindPropertyRelative("Y")))
                && (list.mChannel == 2 || IsClearComponent(p.FindPropertyRelative("Z")));
            if (IsClear)
                l.serializedProperty.DeleteArrayElementAtIndex(l.index);
            else switch (list.mChannel)
                {
                    case 0: ClearComponent(p.FindPropertyRelative("X")); break;
                    case 1: ClearComponent(p.FindPropertyRelative("Y")); break;
                    case 2: ClearComponent(p.FindPropertyRelative("Z")); break;
                    default: break;
                }
        };

        return list;
    }

    class ChannelList : ReorderableList
    {
        public int mChannel;
        public GUIContent mTitle;

        public ChannelList(
            SerializedObject serializedObject,
            SerializedProperty elements,
            int channel, GUIContent title)
        : base(serializedObject, elements, true, true, true, true)
        {
            mChannel = channel;
            mTitle = title;
        }
    };

    private GUIContent steadyLabel;
    private GUIContent freqLabel;
    private float freqLabelWidth;
    private GUIContent ampLabel;
    private float ampLabelWidth;

    private void InitializeLabels(SerializedProperty property)
    {
        if (steadyLabel == null)
        {
            SerializedProperty p = property.FindPropertyRelative("Constant");
            steadyLabel = new GUIContent(p.displayName, p.tooltip) { text = " " };
        }
        if (freqLabel == null)
        {
            SerializedProperty p = property.FindPropertyRelative("Frequency");
            freqLabel = new GUIContent(p.displayName, p.tooltip);
            freqLabelWidth = GUI.skin.label.CalcSize(freqLabel).x;
        }
        if (ampLabel == null)
        {
            SerializedProperty p = property.FindPropertyRelative("Amplitude");
            ampLabel = new GUIContent(p.displayName, p.tooltip);
            ampLabelWidth = GUI.skin.label.CalcSize(ampLabel).x;
        }
    }

    private void DrawNoiseChannel(Rect rect, SerializedProperty property)
    {
        InitializeLabels(property);

        Rect r = rect;
        r.height -= vSpace;
        r.width -= EditorGUIUtility.singleLineHeight + hSpace;
        r.width /= 2;

        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = freqLabelWidth;
        EditorGUI.PropertyField(r, property.FindPropertyRelative("Frequency"), freqLabel);

        r.x += r.width + hSpace;
        EditorGUIUtility.labelWidth = ampLabelWidth;
        EditorGUI.PropertyField(r, property.FindPropertyRelative("Amplitude"), ampLabel);

        r.y -= 1;
        r.x += r.width + hSpace; r.width = EditorGUIUtility.singleLineHeight + hSpace;
        EditorGUIUtility.labelWidth = hSpace;
        EditorGUI.PropertyField(r, property.FindPropertyRelative("Constant"), steadyLabel);

        EditorGUIUtility.labelWidth = oldLabelWidth;
    }

    // SerializedProperty is a NoiseSettings.NoiseParam
    void ClearComponent(SerializedProperty p)
    {
        p.FindPropertyRelative("Amplitude").floatValue = 0;
        p.FindPropertyRelative("Frequency").floatValue = 0;
        p.FindPropertyRelative("Constant").boolValue = false;
    }

    // SerializedProperty is a NoiseSettings.NoiseParam
    bool IsClearComponent(SerializedProperty p)
    {
        return p.FindPropertyRelative("Amplitude").floatValue == 0
            && p.FindPropertyRelative("Frequency").floatValue == 0;
    }

    /// <summary>Returns a string path from an expression - mostly used to retrieve serialized properties
    /// without hardcoding the field path. Safer, and allows for proper refactoring.</summary>
    /// <typeparam name="TType">Magic expression</typeparam>
    /// <typeparam name="TValue">Magic expression</typeparam>
    /// <param name="expr">Magic expression</param>
    /// <returns>The string version of the field path</returns>
    public static string GetFieldPath<TType, TValue>(Expression<Func<TType, TValue>> expr)
    {
        MemberExpression me;
        switch (expr.Body.NodeType)
        {
            case ExpressionType.MemberAccess:
                me = expr.Body as MemberExpression;
                break;
            default:
                throw new InvalidOperationException();
        }

        var members = new List<string>();
        while (me != null)
        {
            members.Add(me.Member.Name);
            me = me.Expression as MemberExpression;
        }

        var sb = new StringBuilder();
        for (int i = members.Count - 1; i >= 0; i--)
        {
            sb.Append(members[i]);
            if (i > 0) sb.Append('.');
        }
        return sb.ToString();
    }

    string FieldPath<TValue>(Expression<Func<PerlinNoiseSetting, TValue>> expr)
    {
        return GetFieldPath(expr);
    }
}

