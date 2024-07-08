using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ScreenDrawer
{
    public static class ScreenDrawerAssetEditor
    {
        static readonly float LineHeight = 25;
        static ScreenDrawerFeature screenDrawerFeature => ScreenDrawerFeature.Self;

        static Dictionary<AssetBase, ScreenDrawerFeature.EventType> playEventTypeDic = new Dictionary<AssetBase, ScreenDrawerFeature.EventType>();

        public static Rect PlayButtonDrawer(Rect rect, AssetBase asset)
        {
            if (screenDrawerFeature == null || asset == null)
                return rect;
            rect.y += LineHeight * 2;
            rect.height = LineHeight * .7f;
            ScreenDrawerFeature.EventType type;
            if (!playEventTypeDic.TryGetValue(asset, out type))
            {
                type = ScreenDrawerFeature.EventType.After;
                playEventTypeDic.Add(asset, type);
            }
            if (screenDrawerFeature.Contains(asset, ScreenDrawerFeature.EventType.Before))
                type = ScreenDrawerFeature.EventType.Before;
            else if (screenDrawerFeature.Contains(asset, ScreenDrawerFeature.EventType.Between))
                type = ScreenDrawerFeature.EventType.Between;
            bool playing = screenDrawerFeature.Contains(asset, type);
            EditorGUI.BeginDisabledGroup(playing);
            EditorGUI.BeginChangeCheck();
            type = (ScreenDrawerFeature.EventType)EditorGUI.EnumPopup(rect, "Event Type", type);
            if (EditorGUI.EndChangeCheck())
                playEventTypeDic[asset] = type;
            EditorGUI.EndDisabledGroup();
            rect.y += LineHeight;
            if (GUI.Button(rect, playing ? "Stop" : "Play"))
            {
                if (playing)
                {
                    asset.Disable();
                    screenDrawerFeature.Unload(asset, type);
                }
                else
                {
                    asset.Enable();
                    screenDrawerFeature.Setup(asset, type);
                }
                screenDrawerFeature.Create();
            }
            return rect;
        }

        public static float GetHeight(AssetBase asset, bool drawPlayButton = true)
        {
            float buttonHeight = drawPlayButton ? LineHeight * 3 : 0;
            QuadAsset quadAsset = asset as QuadAsset;
            if (quadAsset != null)
                return LineHeight * 7 + buttonHeight;
            GameObjectAsset gameObjectAsset = asset as GameObjectAsset;
            if (gameObjectAsset != null)
                return LineHeight * 12 + buttonHeight;
            return LineHeight * 3 + buttonHeight;
        }

        public static Rect AssetBaseDraw(Rect rect, AssetBase asset)
        {
            EditorGUI.BeginChangeCheck();

            Rect rect0 = rect;
            rect0.height = LineHeight;
            int mask = 0;
            for (int i = 0; i < asset.Pass.Count; i++)
                mask |= 1 << asset.Pass[i];
            EditorGUI.BeginChangeCheck();
            mask = EditorGUI.MaskField(rect0, "Pass", mask, new string[] { "0", "1", "2", "3", "4" });

            Rect rect1 = rect0;
            rect1.y += LineHeight;
            AnchorType anchor = (AnchorType)EditorGUI.EnumPopup(rect1, "Anchor", asset.anchor);

            Rect rect2 = rect1;
            rect2.y += LineHeight;
            Vector2Int pixelSize = EditorGUI.Vector2IntField(rect2, "Pixel Size", asset.pixelSize);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "Porp Changed");
                List<int> pass = new List<int>();
                for (int i = 0; i < 5; i++)
                {
                    int j = 1 << i;
                    if ((mask & j) != j)
                        continue;
                    pass.Add(i);
                }
                asset.Pass = pass;
                asset.anchor = anchor;
                asset.pixelSize = pixelSize;
                EditorUtility.SetDirty(asset);
                asset.RefreshMesh();
            }
            return rect2;
        }

        public static Rect QuadAssetDraw(Rect rect, QuadAsset asset, bool drawPlayButton = true)
        {
            EditorGUI.BeginChangeCheck();
            Rect rect1 = rect;
            rect1.y += LineHeight;
            rect1.height += LineHeight * 2;
            Rect texRect = rect1;
            texRect.width = LineHeight * 3;
            texRect.x += rect1.width - texRect.width;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ObjectField(texRect, asset.mat == null ? null : asset.mat.GetTexture(AssetBase._MainTex), typeof(Texture2D), false);
            EditorGUI.EndDisabledGroup();
            Rect texLabelRect = rect1;
            texLabelRect.width -= texRect.width;
            texLabelRect.height = LineHeight;
            EditorGUI.LabelField(texLabelRect, "Texture: ");
            Rect scaleOffsetRect = rect1;
            scaleOffsetRect.width -= texRect.width + LineHeight * 2;
            scaleOffsetRect.height = LineHeight * 2;
            scaleOffsetRect.x += LineHeight;
            scaleOffsetRect.y += LineHeight;
            EditorGUI.BeginDisabledGroup(asset.tilingOffset.Active);
            Rect activeRect = scaleOffsetRect;
            activeRect.width = 20;
            activeRect.height -= 10;
            scaleOffsetRect.x += 20;
            scaleOffsetRect.width -= 20;
            EditorGUI.Toggle(activeRect, asset.tilingOffset.Active);
            Vector4 scaleOffset = MaterialEditor.TextureScaleOffsetProperty(scaleOffsetRect, asset.tilingOffset.Val);
            EditorGUI.EndDisabledGroup();

            Rect rect2 = rect1;
            rect2.y += LineHeight * 3 + LineHeight * .1f;
            rect2.height = LineHeight * .8f;
            Material material = EditorGUI.ObjectField(rect2, "Material", asset.mat, typeof(Material), false) as Material;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "Porp Changed");
                asset.tilingOffset.OriginVal = scaleOffset;
                asset.mat = material;
                EditorUtility.SetDirty(asset);
                if (screenDrawerFeature != null)
                    screenDrawerFeature.Create();
            }
            if (drawPlayButton)
                rect2 = PlayButtonDrawer(rect2, asset);
            return rect2;
        }

        public static Rect GameObjectAssetDraw(Rect rect, GameObjectAsset asset, bool drawPlayButton = true)
        {
            EditorGUI.BeginChangeCheck();
            Rect rect1 = rect;
            rect1.y += LineHeight;
            rect1.height = LineHeight * .8f;
            GameObject prefab = EditorGUI.ObjectField(rect1, "Prefab", asset.Prefab, typeof(GameObject), false) as GameObject;

            Rect rect2 = rect1;
            rect2.y += LineHeight;
            Vector3 offset = EditorGUI.Vector3Field(rect2, "Offset", asset.Offset);

            rect2.y += LineHeight;
            Vector3 eularAngle = EditorGUI.Vector3Field(rect2, "Rotation", asset.eularAngle);

            rect2.y += LineHeight;
            Vector3 scale = EditorGUI.Vector3Field(rect2, "Scale", asset.Scale);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "Porp Changed");
                asset.Prefab = prefab;
                asset.Offset = offset;
                asset.eularAngle = eularAngle;
                asset.Scale = scale;
                if (prefab == null)
                    asset.pixelSize = Vector2Int.zero;
                else
                {
                    GameObject _obj = Object.Instantiate(prefab);
                    _obj.transform.eulerAngles = eularAngle;
                    Renderer[] renderers = _obj.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds bound = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; i++)
                            bound.Encapsulate(renderers[i].bounds);
                        float f = bound.size.x / bound.size.y;
                        asset.pixelSize = new Vector2Int(Mathf.CeilToInt(Screen.height * f), Screen.height);
                    }
                    Object.DestroyImmediate(_obj);
                }
                EditorUtility.SetDirty(asset);
                asset.RefreshMesh();
            }

            EditorGUI.BeginChangeCheck();
            Rect rect3 = rect2;
            rect3.y += LineHeight;
            Vector2 bias = EditorGUI.Vector2Field(rect3, "Bounds Bias", asset.Bias);

            Rect rect4 = rect3;
            rect4.y += LineHeight;
            rect4.height += LineHeight * 2;
            Rect texRect = rect4;
            texRect.width = LineHeight * 3;
            texRect.x += rect4.width - texRect.width;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ObjectField(texRect, asset.texture, typeof(RenderTexture), false);
            EditorGUI.EndDisabledGroup();
            Rect texLabelRect = rect4;
            texLabelRect.width -= texRect.width;
            texLabelRect.height = LineHeight;
            EditorGUI.LabelField(texLabelRect, "Texture: ");
            Rect scaleOffsetRect = rect4;
            scaleOffsetRect.width -= texRect.width + LineHeight * 2;
            scaleOffsetRect.height = LineHeight * 2;
            scaleOffsetRect.x += LineHeight;
            scaleOffsetRect.y += LineHeight;
            EditorGUI.BeginDisabledGroup(asset.tilingOffset.Active);
            Rect activeRect = scaleOffsetRect;
            activeRect.width = 20;
            activeRect.height -= 10;
            scaleOffsetRect.x += 20;
            scaleOffsetRect.width -= 20;
            EditorGUI.Toggle(activeRect, asset.tilingOffset.Active);
            Vector4 scaleOffset = MaterialEditor.TextureScaleOffsetProperty(scaleOffsetRect, asset.tilingOffset.Val);
            EditorGUI.EndDisabledGroup();

            Rect rect5 = rect4;
            rect5.y += LineHeight * 3 + LineHeight * .1f;
            rect5.height = LineHeight * .8f;
            Material material = EditorGUI.ObjectField(rect5, "Material", asset.mat, typeof(Material), false) as Material;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "Porp Changed");
                asset.Bias = bias;
                asset.tilingOffset.OriginVal = scaleOffset;
                if (asset.texture != null)
                    material.SetTexture(AssetBase._MainTex, asset.texture);
                asset.mat = material;
                EditorUtility.SetDirty(asset);
                if (screenDrawerFeature != null)
                    screenDrawerFeature.Create();
            }
            if (drawPlayButton)
                rect5 = PlayButtonDrawer(rect5, asset);
            return rect5;
        }
    }
}