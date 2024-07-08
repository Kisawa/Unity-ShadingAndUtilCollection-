using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ScreenDrawer
{
    [CustomEditor(typeof(GameObjectAsset))]
    public class GameObjectAssetEditor : Editor
    {
        GameObjectAsset asset;

        private void OnEnable()
        {
            asset = serializedObject.targetObject as GameObjectAsset;
        }

        public override void OnInspectorGUI()
        {
            Rect rect = EditorGUILayout.BeginVertical();
            rect = ScreenDrawerAssetEditor.AssetBaseDraw(rect, asset);
            rect = ScreenDrawerAssetEditor.GameObjectAssetDraw(rect, asset);
            GUILayout.Space(rect.y + rect.height);
            EditorGUILayout.EndVertical();
        }
    }
}