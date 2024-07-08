using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GPUInstancingUtil
{
    public class BrushCache : ScriptableObject
    {
        public GPUInstancingBrush.BrushType brushType;
        public int BrushLayer;
        public int MaskLayer;
        public float BrushRange = 2;
        public float Density = 15;
        public float HitHeight = 2;
        public float Bias = 0;
        public GPUInstancingBrush.Axis MeshOriginAxis = GPUInstancingBrush.Axis.Forward;
        public float ForwardFaceNormal = 1;
        public float RandomSelfRotate = Mathf.PI;
        public float RandomScaleMin = .5f;
        public float RandomScaleMax = 1f;
        public Vector2 RandomValueRange;

        public void RecordProp(GPUInstancingBrush brush)
        {
            if (brush == null)
                return;
            BrushLayer = brush.BrushLayer;
            MaskLayer = brush.MaskLayer;
            BrushRange = brush.BrushRange;
            Density = brush.Density;
            HitHeight = brush.HitHeight;
            Bias = brush.Bias;
            MeshOriginAxis = brush.MeshOriginAxis;
            ForwardFaceNormal = brush.ForwardFaceNormal;
            RandomSelfRotate = brush.RandomSelfRotate;
            RandomScaleMin = brush.RandomScaleMin;
            RandomScaleMax = brush.RandomScaleMax;
            RandomValueRange = brush.RandomValueRange;
            EditorUtility.SetDirty(this);
        }

        public static BrushCache GetBrushCache(InstancingData instancingData, GPUInstancingBrush brush)
        {
            if (instancingData == null)
                return null;
            string dataPath = AssetDatabase.GetAssetPath(instancingData);
            if (string.IsNullOrEmpty(dataPath))
                return null;
            string guid = AssetDatabase.GUIDFromAssetPath(dataPath).ToString();
            return GetBrushCache(guid, GPUInstancingBrush.BrushType.Instancing, brush);
        }

        public static BrushCache GetBrushCache(GameObject prefab, GPUInstancingBrush brush)
        {
            if (prefab == null)
                return null;
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath))
                return null;
            string guid = AssetDatabase.GUIDFromAssetPath(prefabPath).ToString();
            return GetBrushCache(guid, GPUInstancingBrush.BrushType.GameObject, brush);
        }

        static BrushCache GetBrushCache(string guid, GPUInstancingBrush.BrushType brushType, GPUInstancingBrush brush)
        {
            string cachePath = $"{GetUtilPath()}/Editor/BrushCache/{guid}.asset";
            BrushCache cache = AssetDatabase.LoadAssetAtPath<BrushCache>(cachePath);
            if (cache == null)
            {
                cache = CreateInstance<BrushCache>();
                cache.RecordProp(brush);
                AssetDatabase.CreateAsset(cache, cachePath);
                AssetDatabase.ImportAsset(cachePath);
            }
            else if (brush != null)
                brush.ApplyCache(cache);
            cache.brushType = brushType;
            EditorUtility.SetDirty(cache);
            return cache;
        }

        static string GetUtilPath()
        {
            InstancingData test = CreateInstance<InstancingData>();
            MonoScript monoScript = MonoScript.FromScriptableObject(test);
            string path = AssetDatabase.GetAssetPath(monoScript);
            int index = path.IndexOf("/InstancingData.cs");
            path = path.Substring(0, index);
            DestroyImmediate(test);
            return path;
        }
    }
}