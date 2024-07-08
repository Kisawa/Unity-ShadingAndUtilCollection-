using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public static class CommonEditor
{
    public static readonly string RampFolderPath = "Assets/Ramps";
    #region Group
    static Dictionary<ShaderGUI, List<Group>> GroupCache;

    public static Group GroupPop(ShaderGUI core, DrawerData data, string header)
    {
        List<Group> cache = CheckGroupCacheCore(core);
        Group group = cache.FirstOrDefault(x => x.Hearder == header);
        if (group == null)
        {
            group = new Group(data, header);
            cache.Add(group);
        }
        group.Refresh(data);
        return group;
    }

    static List<Group> CheckGroupCacheCore(ShaderGUI core)
    {
        if (GroupCache == null)
            GroupCache = new Dictionary<ShaderGUI, List<Group>>();
        if (!GroupCache.TryGetValue(core, out List<Group> cache))
        {
            cache = new List<Group>();
            GroupCache.Add(core, cache);
        }
        return cache;
    }

    public class Group
    {
        public string Hearder => header;

        DrawerData data;
        string header;
        string[] feature;
        List<MaterialProperty> props = new List<MaterialProperty>();
        MaterialProperty featurePorp;
        bool fold = true;

        public Group(DrawerData data, string header)
        {
            this.data = data;
            this.header = header;
        }

        public void Refresh(DrawerData data)
        {
            this.data = data;
            props.Clear();
            featurePorp = null;
        }

        public void SetFeatureProp(MaterialProperty prop, string[] feature)
        {
            this.featurePorp = prop;
            this.feature = feature;
        }

        public void AddMaterialProp(MaterialProperty prop)
        {
            if (!props.Contains(prop))
                props.Add(prop);
        }

        public void Draw(MaterialEditor editor)
        {
            if (editor == null || (props.Count == 0 && feature == null))
                return;
            if (featurePorp == null)
                fold = BeginGroup(header, fold);
            else
                fold = BeginGroup(header, fold, editor, featurePorp, feature);
            if (fold)
            {
                for (int i = 0; i < props.Count; i++)
                {
                    MaterialProperty prop = props[i];
                    if (prop == null)
                        continue;
                    if (data.Features.TryGetValue(prop.name, out string[] features))
                    {
                        DrawFeature(prop, features);
                        continue;
                    }
                    if (data.TextureFeatures.TryGetValue(prop.name, out string feature))
                    {
                        DrawTextureFeature(editor, prop, feature);
                        continue;
                    }
                    if (data.BoolProps.Contains(prop.name))
                    {
                        DrawToggle(prop);
                        continue;
                    }
                    if (data.RampProps.TryGetValue(prop.name, out int texelSize))
                    {
                        DrawRamp(prop, texelSize);
                        continue;
                    }
                    editor.DefaultShaderProperty(prop, GroupBasedMaterialEditor.CheckDisplayName(prop.displayName));
                }
            }
            EndGroup();
        }
    }
    #endregion

    #region Common
    public static GUIStyle Style_Group => GUI.skin.GetStyle("ObjectField");
    public static GUIStyle Style_HeaderText => GUI.skin.GetStyle("HeaderLabel");
    public static GUIStyle Style_Toggle => GUI.skin.GetStyle("OL ToggleWhite");
    public static GUIStyle Style_ToggleMixed => GUI.skin.GetStyle("OL ToggleMixed");
    public static GUIStyle Style_Fold => GUI.skin.GetStyle("Titlebar Foldout");

    static bool onceGroupToggle;

    public static bool BeginGroup(string label, bool fold)
    {
        EditorGUILayout.BeginVertical(Style_Group);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);
        EditorGUILayout.LabelField(label, Style_HeaderText);
        fold = EditorGUILayout.Toggle(fold, Style_Fold);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(2);

        EditorGUI.indentLevel++;
        onceGroupToggle = true;
        if (!onceGroupToggle)
            EditorGUI.BeginDisabledGroup(!onceGroupToggle);
        return fold;
    }

    public static bool BeginGroup(string label, bool fold, ref bool toggle)
    {
        EditorGUILayout.BeginVertical(Style_Group);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);
        toggle = EditorGUILayout.Toggle(toggle, Style_Toggle, GUILayout.Width(20));
        EditorGUILayout.LabelField(label, Style_HeaderText);
        fold = EditorGUILayout.Toggle(fold, Style_Fold);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
        EditorGUI.indentLevel++;
        onceGroupToggle = toggle;
        if (!onceGroupToggle)
            EditorGUI.BeginDisabledGroup(!onceGroupToggle);
        return fold;
    }

    public static bool BeginGroup(string label, bool fold, MaterialEditor editor, MaterialProperty featureProp, string[] feature)
    {
        EditorGUILayout.BeginVertical(Style_Group);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);
        bool toggle = true;
        if (feature.Length == 1 && featureProp.type != MaterialProperty.PropType.Texture)
        {
            EditorGUI.BeginChangeCheck();
            toggle = EditorGUILayout.Toggle(featureProp.hasMixedValue ? true : featureProp.floatValue == 1, featureProp.hasMixedValue ? Style_ToggleMixed : Style_Toggle, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                featureProp.floatValue = toggle ? 1 : 0;
                if (!string.IsNullOrEmpty(feature[0]))
                    SetFeatureState(featureProp, feature[0], toggle);
            }
        }
        EditorGUILayout.LabelField(label, Style_HeaderText);
        fold = EditorGUILayout.Toggle(fold, Style_Fold);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
        EditorGUI.indentLevel++;
        if (featureProp.type == MaterialProperty.PropType.Texture)
        {
            EditorGUI.showMixedValue = featureProp.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            toggle = editor.TextureProperty(featureProp, GroupBasedMaterialEditor.CheckDisplayName(featureProp.displayName), !featureProp.flags.HasFlag(MaterialProperty.PropFlags.NoScaleOffset)) != null;
            if (EditorGUI.EndChangeCheck())
            {
                if (!string.IsNullOrEmpty(feature[0]))
                    SetFeatureState(featureProp, feature[0], toggle);
            }
            EditorGUI.showMixedValue = false;
        }
        else if (feature.Length > 1)
        {
            EditorGUI.showMixedValue = featureProp.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            int index = EditorGUILayout.Popup(GroupBasedMaterialEditor.CheckDisplayName(featureProp.displayName), featureProp.hasMixedValue ? -1 : (int)featureProp.floatValue, feature);
            if (EditorGUI.EndChangeCheck())
            {
                featureProp.floatValue = index;
                for (int i = 0; i < feature.Length; i++)
                {
                    string _feature = feature[i];
                    if (!string.IsNullOrEmpty(_feature))
                        SetFeatureState(featureProp, _feature, index == i);
                }
            }
            EditorGUI.showMixedValue = false;
        }
        onceGroupToggle = toggle;
        if (!onceGroupToggle)
            EditorGUI.BeginDisabledGroup(!onceGroupToggle);
        return fold;
    }

    public static void EndGroup()
    {
        if (!onceGroupToggle)
            EditorGUI.EndDisabledGroup();
        EditorGUI.indentLevel--;
        GUILayout.Space(5);
        EditorGUILayout.EndVertical();
    }

    public static void DrawFeature(MaterialProperty prop, string[] feature)
    {
        if (feature.Length == 0)
            return;
        EditorGUI.showMixedValue = prop.hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), GUILayout.Width(200));
        EditorGUI.BeginChangeCheck();
        if (feature.Length == 1)
        {
            bool open = EditorGUILayout.Toggle(prop.hasMixedValue ? false : prop.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = open ? 1 : 0;
                if (!string.IsNullOrEmpty(feature[0]))
                    SetFeatureState(prop, feature[0], open);
            }
        }
        else
        {
            int index = EditorGUILayout.Popup(prop.hasMixedValue ? -1 : (int)prop.floatValue, feature);
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = index;
                for (int i = 0; i < feature.Length; i++)
                {
                    string _feature = feature[i];
                    if (!string.IsNullOrEmpty(_feature) && _feature.ToUpper() != "_NONE")
                        SetFeatureState(prop, _feature, index == i);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.showMixedValue = false;
    }

    public static void DrawTextureFeature(MaterialEditor editor, MaterialProperty prop, string feature)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        Texture tex = editor.TextureProperty(prop, GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), !prop.flags.HasFlag(MaterialProperty.PropFlags.NoScaleOffset));
        if (EditorGUI.EndChangeCheck())
        {
            if (!string.IsNullOrEmpty(feature))
                SetFeatureState(prop, feature, tex != null);
        }
        EditorGUI.showMixedValue = false;
    }

    public static void DrawToggle(MaterialProperty prop)
    {
        EditorGUI.showMixedValue = prop.hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), GUILayout.Width(200));
        EditorGUI.BeginChangeCheck();
        bool toggle = EditorGUILayout.Toggle(prop.hasMixedValue ? false : prop.floatValue == 1);
        if (EditorGUI.EndChangeCheck())
            prop.floatValue = toggle ? 1 : 0;
        EditorGUILayout.EndHorizontal();
        EditorGUI.showMixedValue = false;
    }

    public static void DrawRamp(MaterialProperty prop, int texelSize)
    {
        EditorGUI.showMixedValue = prop.hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), GUILayout.Width(EditorGUIUtility.currentViewWidth * .36f));
        EditorGUI.BeginChangeCheck();
        Material mat = (Material)prop.targets[0];
        MaterialDataCache data = GetMaterialDataCache(mat);
        Gradient gradient = data.GetRampGradient(prop);
        Gradient _gradient = EditorGUILayout.GradientField(gradient);
        if (EditorGUI.EndChangeCheck())
        {
            Gradient newGradient = new Gradient();
            newGradient.mode = _gradient.mode;
            newGradient.SetKeys(_gradient.colorKeys, _gradient.alphaKeys);
            Undo.RecordObject(mat, "GroupBasedMaterialEditor Ramp Changed");
            Undo.RecordObject(data, "GroupBasedMaterialEditor Ramp Changed");
            data.SetRampGradient(prop, newGradient);
            EditorUtility.SetDirty(data);
            for (int i = 1; i < prop.targets.Length; i++)
            {
                Material _mat = (Material)prop.targets[i];
                MaterialDataCache _data = GetMaterialDataCache(_mat);
                Gradient _newGradient = new Gradient();
                _newGradient.mode = _gradient.mode;
                _newGradient.SetKeys(_gradient.colorKeys, _gradient.alphaKeys);
                Undo.RecordObject(_mat, "GroupBasedMaterialEditor Ramp Changed");
                Undo.RecordObject(_data, "GroupBasedMaterialEditor Ramp Changed");
                _data.SetRampGradient(prop, _newGradient);
                EditorUtility.SetDirty(_data);
            }
            Texture2D texture = GetRampTex(_gradient, texelSize);
            prop.textureValue = texture;
        }
        string matPath = AssetDatabase.GetAssetPath(mat);
        string texPath = AssetDatabase.GetAssetPath(prop.textureValue);
        bool canSave = prop.textureValue != null && !string.IsNullOrEmpty(matPath) && string.IsNullOrEmpty(texPath);
        EditorGUI.BeginDisabledGroup(!canSave);
        if (GUILayout.Button("Save", GUILayout.Width(60)))
        {
            Texture2D _tex = SaveTex(prop.textureValue as Texture2D, matPath, prop.name);
            if (_tex != null)
                prop.textureValue = _tex;
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        Texture tex = EditorGUILayout.ObjectField(prop.textureValue, typeof(Texture), false) as Texture;
        if (EditorGUI.EndChangeCheck())
        {
            prop.textureValue = tex;
        }
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(data, typeof(MaterialDataCache), false, GUILayout.Width(100));
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUI.showMixedValue = false;
    }

    static bool advanced;
    public static void DrawSetting(MaterialEditor materialEditor, MaterialProperty queueProp, MaterialProperty cullProp, MaterialProperty ztestProp, MaterialProperty zwriteProp, MaterialProperty srcBlend, MaterialProperty dstBlend, MaterialProperty blendOpProp, MaterialProperty queueOffsetProp)
    {
        CheckQueueResult(queueProp, queueOffsetProp);
        EditorGUILayout.BeginVertical("SelectionRect");
        if (queueProp != null)
            DrawQueueSetting(queueProp, ztestProp, zwriteProp, srcBlend, dstBlend, blendOpProp, queueOffsetProp);
        if (cullProp != null)
            DrawCullSetting(cullProp);
        if (queueOffsetProp != null || ztestProp != null || zwriteProp != null || srcBlend != null || dstBlend != null || blendOpProp != null)
        {
            EditorGUILayout.BeginHorizontal();
            advanced = EditorGUILayout.Toggle(advanced, Style_Fold, GUILayout.Width(20));
            EditorGUILayout.LabelField("Advanced");
            EditorGUILayout.EndHorizontal();
            if (advanced)
            {
                if (queueProp != null && queueOffsetProp != null)
                    DrawQueueOffsetSetting(queueProp, queueOffsetProp);
                if (ztestProp != null)
                    DrawZTestSetting(ztestProp);
                if (zwriteProp != null)
                    DrawZWriteSetting(zwriteProp);
                if (srcBlend != null)
                    DrawBlendSetting(srcBlend);
                if (dstBlend != null)
                    DrawBlendSetting(dstBlend);
                if (blendOpProp != null)
                    DrawBlendOpSetting(blendOpProp);
            }
            GUILayout.Space(10);
        }
        DrawBakeSetting(materialEditor);
        EditorGUILayout.EndVertical();
    }

    public static void DrawQueueSetting(MaterialProperty prop, MaterialProperty ztestProp, MaterialProperty zwriteProp, MaterialProperty srcBlendProp, MaterialProperty dstBlendProp, MaterialProperty blendOpProp, MaterialProperty queueOffsetProp)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), GUILayout.Width(200));
        Queue queue = (Queue)EditorGUILayout.EnumPopup((Queue)(prop.hasMixedValue ? -1 : prop.floatValue));
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            prop.floatValue = (float)queue;
            for (int i = 0; i < prop.targets.Length; i++)
            {
                Material mat = prop.targets[i] as Material;
                SetQueueDefaultProp(mat, queue,
                    ztestProp == null ? "" : ztestProp.name,
                    zwriteProp == null ? "" : zwriteProp.name,
                    srcBlendProp == null ? "" : srcBlendProp.name,
                    dstBlendProp == null ? "" : dstBlendProp.name,
                    blendOpProp == null ? "" : blendOpProp.name,
                    queueOffsetProp == null ? "" : queueOffsetProp.name);
            }
        }
        EditorGUI.showMixedValue = false;
    }

    public static void CheckQueueResult(MaterialProperty queueProp, MaterialProperty offsetProp)
    {
        if (queueProp == null)
            return;
        for (int i = 0; i < queueProp.targets.Length; i++)
        {
            Material mat = queueProp.targets[i] as Material;
            int res = -1;
            if (offsetProp != null)
                res = GetQueueResult((Queue)mat.GetFloat(queueProp.name), (int)mat.GetFloat(offsetProp.name));
            else
                res = GetQueueResult((Queue)mat.GetFloat(queueProp.name), 0);
            if (res != -1 && mat.renderQueue != res)
            {
                mat.renderQueue = res;
                EditorUtility.SetDirty(mat);
            }
        }
    }

    public static int GetQueueResult(Queue queue, int queueOffset)
    {
        switch (queue)
        {
            case Queue.Background:
                return 1000 + queueOffset;
            case Queue.Geometry:
                return 2000 + queueOffset;
            case Queue.AlphaTest:
                return 2450 + queueOffset;
            case Queue.Transparent:
                return 3000 + queueOffset;
        }
        return 2000;
    }

    public static void SetQueueDefaultProp(Material mat, Queue queue, string ztest, string zwrite, string srcBlend, string dstBlend, string blendOp, string queueOffset)
    {
        int offset = string.IsNullOrEmpty(queueOffset) ? 0 : (int)mat.GetFloat(queueOffset);
        switch (queue)
        {
            case Queue.Background:
                mat.renderQueue = 1000 + offset;
                if (!string.IsNullOrEmpty(ztest))
                    mat.SetFloat(ztest, (float)CompareFunction.LessEqual);
                if (!string.IsNullOrEmpty(zwrite))
                    mat.SetFloat(zwrite, 1);
                if (!string.IsNullOrEmpty(srcBlend))
                    mat.SetFloat(srcBlend, (float)BlendMode.One);
                if (!string.IsNullOrEmpty(dstBlend))
                    mat.SetFloat(dstBlend, (float)BlendMode.Zero);
                if (!string.IsNullOrEmpty(blendOp))
                    mat.SetFloat(blendOp, (float)BlendOp.Add);
                break;
            case Queue.Geometry:
                mat.renderQueue = 2000 + offset;
                if (!string.IsNullOrEmpty(ztest))
                    mat.SetFloat(ztest, (float)CompareFunction.LessEqual);
                if (!string.IsNullOrEmpty(zwrite))
                    mat.SetFloat(zwrite, 1);
                if (!string.IsNullOrEmpty(srcBlend))
                    mat.SetFloat(srcBlend, (float)BlendMode.One);
                if (!string.IsNullOrEmpty(dstBlend))
                    mat.SetFloat(dstBlend, (float)BlendMode.Zero);
                if (!string.IsNullOrEmpty(blendOp))
                    mat.SetFloat(blendOp, (float)BlendOp.Add);
                break;
            case Queue.AlphaTest:
                mat.renderQueue = 2450 + offset;
                if (!string.IsNullOrEmpty(ztest))
                    mat.SetFloat(ztest, (float)CompareFunction.LessEqual);
                if (!string.IsNullOrEmpty(zwrite))
                    mat.SetFloat(zwrite, 1);
                if (!string.IsNullOrEmpty(srcBlend))
                    mat.SetFloat(srcBlend, (float)BlendMode.One);
                if (!string.IsNullOrEmpty(dstBlend))
                    mat.SetFloat(dstBlend, (float)BlendMode.Zero);
                if (!string.IsNullOrEmpty(blendOp))
                    mat.SetFloat(blendOp, (float)BlendOp.Add);
                break;
            case Queue.Transparent:
                mat.renderQueue = 3000 + offset;
                if (!string.IsNullOrEmpty(ztest))
                    mat.SetFloat(ztest, (float)CompareFunction.LessEqual);
                if (!string.IsNullOrEmpty(zwrite))
                    mat.SetFloat(zwrite, 0);
                if (!string.IsNullOrEmpty(srcBlend))
                    mat.SetFloat(srcBlend, (float)BlendMode.SrcAlpha);
                if (!string.IsNullOrEmpty(dstBlend))
                    mat.SetFloat(dstBlend, (float)BlendMode.OneMinusSrcAlpha);
                if (!string.IsNullOrEmpty(blendOp))
                    mat.SetFloat(blendOp, (float)BlendOp.Add);
                break;
        }
        EditorUtility.SetDirty(mat);
    }

    public static void DrawCullSetting(MaterialProperty prop)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), GUILayout.Width(200));
        CullMode cull = (CullMode)EditorGUILayout.EnumPopup((CullMode)(prop.hasMixedValue ? -1 : prop.floatValue));
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
            prop.floatValue = (float)cull;
        EditorGUI.showMixedValue = false;
    }

    public static void DrawZTestSetting(MaterialProperty prop)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), GUILayout.Width(200));
        CompareFunction res = (CompareFunction)EditorGUILayout.EnumPopup((CompareFunction)(prop.hasMixedValue ? -1 : prop.floatValue));
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
            prop.floatValue = (float)res;
        EditorGUI.showMixedValue = false;
    }

    public static void DrawZWriteSetting(MaterialProperty prop)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), GUILayout.Width(200));
        bool res = EditorGUILayout.Toggle(prop.hasMixedValue ? true : prop.floatValue == 1);
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
            prop.floatValue = res ? 1 : 0;
        EditorGUI.showMixedValue = false;
    }

    public static void DrawBlendSetting(MaterialProperty prop)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), GUILayout.Width(200));
        BlendMode res = (BlendMode)EditorGUILayout.EnumPopup((BlendMode)(prop.hasMixedValue ? -1 : prop.floatValue));
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
            prop.floatValue = (float)res;
        EditorGUI.showMixedValue = false;
    }

    public static void DrawBlendOpSetting(MaterialProperty prop)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), GUILayout.Width(200));
        BlendOp res = (BlendOp)EditorGUILayout.EnumPopup((BlendOp)(prop.hasMixedValue ? -1 : prop.floatValue));
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
            prop.floatValue = (float)res;
        EditorGUI.showMixedValue = false;
    }

    public static void DrawQueueOffsetSetting(MaterialProperty queueProp, MaterialProperty prop)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GroupBasedMaterialEditor.CheckDisplayName(prop.displayName), GUILayout.Width(200));
        int res = EditorGUILayout.IntField(prop.hasMixedValue ? -1 : (int)prop.floatValue);
        EditorGUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
        {
            prop.floatValue = res;
            Queue queue = (Queue)queueProp.floatValue;
            for (int i = 0; i < prop.targets.Length; i++)
            {
                Material _mat = prop.targets[i] as Material;
                switch (queue)
                {
                    case Queue.Background:
                        _mat.renderQueue = 1000 + res;
                        break;
                    case Queue.Geometry:
                        _mat.renderQueue = 2000 + res;
                        break;
                    case Queue.AlphaTest:
                        _mat.renderQueue = 2450 + res;
                        break;
                    case Queue.Transparent:
                        _mat.renderQueue = 3000 + res;
                        break;
                }
                EditorUtility.SetDirty(_mat);
            }
        }
        EditorGUI.showMixedValue = false;
    }

    public static void DrawBakeSetting(MaterialEditor materialEditor)
    {
        Material mat = materialEditor.target as Material;

        MaterialGlobalIlluminationFlags res = mat.globalIlluminationFlags;
        bool hasMixedValue = false;
        for (int j = 0; j < materialEditor.targets.Length; j++)
        {
            Material _mat = materialEditor.targets[j] as Material;
            if (_mat.globalIlluminationFlags != res)
            {
                hasMixedValue = true;
                break;
            }
        }
        EditorGUI.showMixedValue = hasMixedValue;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(Mathf.Max(0, EditorGUIUtility.currentViewWidth - 253));
        EditorGUILayout.LabelField("GI Flag", GUILayout.Width(70));
        EditorGUI.BeginChangeCheck();
        res = (MaterialGlobalIlluminationFlags)EditorGUILayout.EnumPopup(hasMixedValue ? (MaterialGlobalIlluminationFlags)(-1) : mat.globalIlluminationFlags, GUILayout.Width(150));
        if (EditorGUI.EndChangeCheck())
        {
            for (int i = 0; i < materialEditor.targets.Length; i++)
            {
                Material _mat = materialEditor.targets[i] as Material;
                Undo.RecordObject(_mat, "Material GlobalIlluminationFlags Changed");
                _mat.globalIlluminationFlags = res;
                EditorUtility.SetDirty(_mat);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.showMixedValue = false;
    }

    public static void DrawPassActive(MaterialEditor materialEditor)
    {
        Material mat = materialEditor.target as Material;
        Shader shader = mat.shader;
        List<(string, string)> passNames = new List<(string, string)>();
        for (int i = 0; i < mat.passCount; i++)
        {
            string name = mat.GetPassName(i);
            string lightModeTag = shader.FindPassTagValue(i, new ShaderTagId("LightMode")).name;
            if (lightModeTag == "UniversalForward")
                continue;
            passNames.Add((name, lightModeTag));
        }
        if (passNames.Count > 0)
        {
            EditorGUILayout.BeginVertical("SelectionRect");
            EditorGUILayout.LabelField("Pass Active");
        }
        EditorGUI.indentLevel++;
        for (int i = 0; i < passNames.Count; i++)
        {
            (string, string) passName = passNames[i];
            bool res = mat.GetShaderPassEnabled(passName.Item1);
            bool hasMixedValue = false;
            for (int j = 0; j < materialEditor.targets.Length; j++)
            {
                Material _mat = materialEditor.targets[j] as Material;
                if (_mat.GetShaderPassEnabled(passName.Item1) != res)
                {
                    hasMixedValue = true;
                    break;
                }
            }
            EditorGUI.showMixedValue = hasMixedValue;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            res = EditorGUILayout.Toggle(hasMixedValue ? false : res, GUILayout.Width(30));
            if (EditorGUI.EndChangeCheck())
            {
                for (int j = 0; j < materialEditor.targets.Length; j++)
                {
                    Material _mat = materialEditor.targets[j] as Material;
                    Undo.RecordObject(_mat, "Material Pass Active Changed");
                    _mat.SetShaderPassEnabled(passName.Item1, res);
                    EditorUtility.SetDirty(_mat);
                }
            }
            EditorGUILayout.LabelField($"{passName.Item1}  |  {passName.Item2}");
            EditorGUILayout.EndHorizontal();
            EditorGUI.showMixedValue = false;
        }
        EditorGUI.indentLevel--;
        if (passNames.Count > 0)
            EditorGUILayout.EndVertical();
    }

    static Dictionary<Material, MaterialDataCache> instanceMaterialDataCaches = new Dictionary<Material, MaterialDataCache>();
    static MaterialDataCache GetMaterialDataCache(Material mat)
    {
        if (mat == null)
            return null;
        string matPath = AssetDatabase.GetAssetPath(mat);
        if (string.IsNullOrEmpty(matPath))
        {
            if (!instanceMaterialDataCaches.TryGetValue(mat, out MaterialDataCache _data))
            {
                _data = ScriptableObject.CreateInstance<MaterialDataCache>();
                _data.name = mat.name;
                _data.material = mat;
            }
            return _data;
        }
        string guid = AssetDatabase.GUIDFromAssetPath(matPath).ToString();
        string folderPath = "Assets/Editor";
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets", "Editor");
        folderPath = "Assets/Editor/MaterialDataCaches";
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/Editor", "MaterialDataCaches");
        
        string path = $"{folderPath}/{guid}.asset";
        MaterialDataCache data = AssetDatabase.LoadAssetAtPath<MaterialDataCache>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MaterialDataCache>();
            data.name = mat.name;
            data.material = mat;
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.ImportAsset(path);
        }
        return data;
    }

    static Texture2D GetRampTex(Gradient gradient, int texelSize)
    {
        Color[] color = new Color[texelSize];
        for (int i = 0; i < texelSize; i++)
            color[i] = gradient.Evaluate(i / (float)texelSize);
        Texture2D texture = new Texture2D(texelSize, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(color);
        texture.Apply();
        return texture;
    }

    static Texture2D SaveTex(Texture2D texture, string matPath, string propName)
    {
        if (texture == null || string.IsNullOrEmpty(matPath) || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture)))
            return null;
        int index = matPath.LastIndexOf(".");
        string path = matPath.Substring(0, index) + $"-{propName}_ramp.png";
        byte[] bytes = texture.EncodeToPNG();
        FileStream file = File.Open(path, FileMode.Create);
        BinaryWriter write = new BinaryWriter(file);
        write.Write(bytes);
        file.Close();
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = TextureImporter.GetAtPath(path) as TextureImporter;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    public enum Queue
    {
        Background,
        Geometry,
        AlphaTest,
        Transparent
    }
    #endregion

    #region Feature
    public static void SetFeatureState(MaterialProperty prop, string feature, bool res)
    {
        for (int i = 0; i < prop.targets.Length; i++)
        {
            Material mat = prop.targets[i] as Material;
            if (res)
                mat.EnableKeyword(feature);
            else
                mat.DisableKeyword(feature);
        }
    }
    #endregion
}