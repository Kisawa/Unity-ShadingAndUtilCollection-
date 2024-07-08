using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(MaterialDataCache))]
public class MaterialDataCacheInspector : Editor
{
    MaterialDataCache self;

    SerializedProperty materialProp;
    SerializedProperty rampNamesProp;
    SerializedProperty rampGradientsProp;

    ReorderableList rampList;

    private void OnEnable()
    {
        self = serializedObject.targetObject as MaterialDataCache;
        materialProp = serializedObject.FindProperty("material");
        rampNamesProp = serializedObject.FindProperty("rampNames");
        rampGradientsProp = serializedObject.FindProperty("rampGradients");

        rampList = new ReorderableList(self.rampNames, typeof(string), false, true, false, false);
        rampList.drawElementCallback = DrawRampItem;
        rampList.drawHeaderCallback = DrawRampHeader;
        rampList.elementHeight = 17;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(materialProp);
        EditorGUI.EndDisabledGroup();
        GUILayout.Space(10);
        rampList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawRampHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Ramp Gradient");
    }

    void DrawRampItem(Rect rect, int index, bool isActive, bool isFocused)
    {
        string name = (string)rampList.list[index];
        Gradient gradient = self.rampGradients[index];
        float width = rect.width;
        rect.width = width * .33f;
        EditorGUI.TextField(rect, name);
        rect.x += width * .35f;
        rect.width = width * .65f;
        EditorGUI.BeginChangeCheck();
        Gradient _gradient = EditorGUI.GradientField(rect, gradient);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(self, "Material Data Cache Changed Gradient");
            Gradient newGradient = new Gradient();
            newGradient.mode = _gradient.mode;
            newGradient.SetKeys(_gradient.colorKeys, _gradient.alphaKeys);
            self.rampGradients[index] = newGradient;
            EditorUtility.SetDirty(self);
        }
    }
}