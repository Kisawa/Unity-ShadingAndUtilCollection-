using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ScreenDrawer
{
    [CustomEditor(typeof(QuadAsset))]
    public class QuadAssetEditor : Editor
    {
        QuadAsset asset;

        private void OnEnable()
        {
            asset = serializedObject.targetObject as QuadAsset;
        }

        public override void OnInspectorGUI()
        {
            Rect rect = EditorGUILayout.BeginVertical();
            rect = ScreenDrawerAssetEditor.AssetBaseDraw(rect, asset);
            rect = ScreenDrawerAssetEditor.QuadAssetDraw(rect, asset);
            GUILayout.Space(rect.y + rect.height);
            EditorGUILayout.EndVertical();
        }
    }
}