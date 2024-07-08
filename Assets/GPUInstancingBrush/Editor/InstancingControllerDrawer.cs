using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InstancingController))]
public class InstancingControllerDrawer : Editor
{
    InstancingController self;
    SerializedProperty DataProp;
    SerializedProperty OffsetProp;

    private void OnEnable()
    {
        self = (InstancingController)target;
        DataProp = serializedObject.FindProperty("Data");
        OffsetProp = serializedObject.FindProperty("Offset");

        Undo.undoRedoPerformed += self.RefreshInstancingView;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= self.RefreshInstancingView;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(DataProp);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            self.RefreshInstancingView();
        }
        GUILayout.Space(10);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(OffsetProp);
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("Reset Offset"))
            OffsetProp.vector3Value = -self.transform.position;
        serializedObject.ApplyModifiedProperties();
    }
}
