using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class GroupBasedMaterialEditor : ShaderGUI
{
    public static void CheckQueue(Material material, Shader shader = null)
    {
        if (material == null)
            return;
        if (shader == null)
            shader = material.shader;
        CheckSettingName(shader, out string queue, out string cull, out string ztest, out string zwrite, out string srcBlend, out string dstBlend, out string blendOp, out string queueOffset);
        if (queue == null)
            return;
        if (material.HasProperty(queue))
        {
            CommonEditor.Queue _queue = (CommonEditor.Queue)material.GetFloat(queue);
            CommonEditor.SetQueueDefaultProp(material, _queue, ztest, zwrite, srcBlend, dstBlend, blendOp, queueOffset);
        }
        else
        {
            int queueIndex = shader.FindPropertyIndex(queue);
            if (queueIndex > -1)
            {
                CommonEditor.Queue _queue = (CommonEditor.Queue)shader.GetPropertyDefaultFloatValue(queueIndex);
                CommonEditor.SetQueueDefaultProp(material, _queue, ztest, zwrite, srcBlend, dstBlend, blendOp, queueOffset);
            }
        }
    }

    static void CheckSettingName(Shader shader, out string queue, out string cull, out string ztest, out string zwrite, out string srcBlend, out string dstBlend, out string blendOp, out string queueOffset)
    {
        queue = cull = ztest = zwrite = srcBlend = dstBlend = blendOp = queueOffset = "";
        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            string name = shader.GetPropertyName(i);
            string displayName = shader.GetPropertyDescription(i);
            string data = CheckDrawerHeader(displayName, out string[] extra);
            switch (data)
            {
                case "Queue":
                    queue = name;
                    break;
                case "Cull":
                    cull = name;
                    break;
                case "ZTest":
                    ztest = name;
                    break;
                case "ZWrite":
                    zwrite = name;
                    break;
                case "SrcBlend":
                    srcBlend = name;
                    break;
                case "DstBlend":
                    dstBlend = name;
                    break;
                case "BlendOp":
                    blendOp = name;
                    break;
                case "QueueOffset":
                    queueOffset = name;
                    break;
            }
        }
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);
        CheckQueue(material, newShader);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        DrawerData.RefreshArray(properties);

        Material mat = materialEditor.target as Material;
        Shader shader = mat.shader;
        DrawerData data = DrawerData.Get(shader);
        if (data == null)
        {
            base.OnGUI(materialEditor, properties);
            return;
        }
        List<CommonEditor.Group> groups = new List<CommonEditor.Group>();
        foreach (var item in data.GroupProps)
        {
            groups.Add(CommonEditor.GroupPop(this, data, item.Key));
        }
        MaterialProperty queueProp = null, cullProp = null, ztestProp = null, zwriteProp = null, srcBlend = null, dstBlend = null, blendOpProp = null, queueOffsetProp = null;
        List<MaterialProperty> others = new List<MaterialProperty>();

        for (int i = 0; i < properties.Length; i++)
        {
            MaterialProperty prop = properties[i];
            if (prop.flags == MaterialProperty.PropFlags.HideInInspector || prop.flags == MaterialProperty.PropFlags.PerRendererData)
                continue;
            if (prop.name == data.QueueProp)
            {
                queueProp = prop;
                continue;
            }
            if (prop.name == data.CullProp)
            {
                cullProp = prop;
                continue;
            }
            if (prop.name == data.ZTestProp)
            {
                ztestProp = prop;
                continue;
            }
            if (prop.name == data.ZWriteProp)
            {
                zwriteProp = prop;
                continue;
            }
            if (prop.name == data.SrcBlendProp)
            {
                srcBlend = prop;
                continue;
            }
            if (prop.name == data.DstBlendProp)
            {
                dstBlend = prop;
                continue;
            }
            if (prop.name == data.BlendOpProp)
            {
                blendOpProp = prop;
                continue;
            }
            if (prop.name == data.QueueOffsetProp)
            {
                queueOffsetProp = prop;
                continue;
            }
            if (TrySetGroupFeatureProp(data, groups, prop))
                continue;
            if (TryPushGroup(groups, prop))
                continue;
            others.Add(prop);
        }
        for (int i = 0; i < groups.Count; i++)
        {
            groups[i].Draw(materialEditor);
            GUILayout.Space(10);
        }
        for (int i = 0; i < others.Count; i++)
        {
            MaterialProperty prop = others[i];
            if (TryDrawFeatureProp(data, prop))
                continue;
            if (TryDrawTextureFeatureProp(data, materialEditor, prop))
                continue;
            if (TryDrawToggleProp(data, prop))
                continue;
            if (TryDrawRampProp(data, prop))
                continue;
            materialEditor.DefaultShaderProperty(prop, CheckDisplayName(prop.displayName));
        }
        GUILayout.Space(10);
        CommonEditor.DrawSetting(materialEditor, queueProp, cullProp, ztestProp, zwriteProp, srcBlend, dstBlend, blendOpProp, queueOffsetProp);
        GUILayout.Space(20);
        CommonEditor.DrawPassActive(materialEditor);
    }

    bool TryDrawFeatureProp(DrawerData data, MaterialProperty prop)
    {
        if (data.Features.TryGetValue(prop.name, out string[] feature))
        {
            CommonEditor.DrawFeature(prop, feature);
            return true;
        }
        return false;
    }

    bool TryDrawTextureFeatureProp(DrawerData data, MaterialEditor editor, MaterialProperty prop)
    {
        if (data.TextureFeatures.TryGetValue(prop.name, out string feature))
        {
            CommonEditor.DrawTextureFeature(editor, prop, feature);
            return true;
        }
        return false;
    }

    bool TryDrawToggleProp(DrawerData data, MaterialProperty prop)
    {
        if (data.BoolProps.Contains(prop.name))
        {
            CommonEditor.DrawToggle(prop);
            return true;
        }
        return false;
    }

    bool TryDrawRampProp(DrawerData data, MaterialProperty prop)
    {
        if (data.RampProps.TryGetValue(prop.name, out int texelSize))
        {
            CommonEditor.DrawRamp(prop, texelSize);
            return true;
        }
        return false;
    }

    bool TrySetGroupFeatureProp(DrawerData data, List<CommonEditor.Group> groups, MaterialProperty prop)
    {
        if (data.GroupFeatures.TryGetValue(prop.name, out (string, string[]) groupFeature))
        {
            for (int i = 0; i < groups.Count; i++)
            {
                CommonEditor.Group group = groups[i];
                if (group.Hearder == groupFeature.Item1)
                {
                    group.SetFeatureProp(prop, groupFeature.Item2);
                    return true;
                }
            }
        }
        return false;
    }

    bool TryPushGroup(List<CommonEditor.Group> groups, MaterialProperty prop)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            CommonEditor.Group group = groups[i];
            if (GroupDrawer.IsInGroup(group.Hearder, prop))
            {
                group.AddMaterialProp(prop);
                return true;
            }
        }
        return false;
    }

    public static string CheckDisplayName(string str)
    { 
        return str.Split('/')[0].Trim();
    }

    public static string CheckDrawerHeader(string str, out string[] extra)
    {
        extra = new string[0];
        string[] strs = str.Split('/');
        if (strs.Length <= 1)
            return "";
        string data = strs[1].Trim();
        string[] d = data.Split('(');
        if (d.Length <= 1)
        {
            if (d.Length == 1)
                return d[0].Trim();
            return "";
        }
        data = d[0].Trim();
        string extraStr = d[1].Trim();
        extraStr = extraStr.Remove(extraStr.Length - 1);
        if (extraStr.Length <= 0)
            return data;
        extra = extraStr.Split(',');
        for (int i = 0; i < extra.Length; i++)
            extra[i] = extra[i].Trim();
        return data;
    }
}

public class DrawerData
{
    static Dictionary<Shader, DrawerData> Cache;

    public static DrawerData Get(Shader shader)
    {
        if (Cache == null)
            return null;
        if (Cache.TryGetValue(shader, out DrawerData data))
            return data;
        return null;
    }

    public static DrawerData Pop(Shader shader)
    {
        if (Cache == null)
            Cache = new Dictionary<Shader, DrawerData>();
        if (!Cache.TryGetValue(shader, out DrawerData data))
        {
            data = new DrawerData();
            Cache.Add(shader, data);
        }
        return data;
    }

    public static DrawerData Pop(MaterialProperty prop)
    {
        if (prop == null)
            return null;
        Shader shader = (prop.targets[0] as Material).shader;
        return Pop(shader);
    }

    public static void RefreshArray(MaterialProperty[] properties)
    {
        for (int i = 0; i < properties.Length; i++)
        {
            MaterialProperty prop = properties[i];
            string data = GroupBasedMaterialEditor.CheckDrawerHeader(prop.displayName, out string[] extra);
            switch (data)
            {
                case "Group":
                    GroupDrawer.Apply(prop, extra);
                    break;
                case "GroupFeature":
                    GroupFeatureDrawer.Apply(prop, extra);
                    break;
                case "TextureFeature":
                    TextureFeatureDrawer.Apply(prop, extra);
                    break;
                case "Feature":
                    FeatureDrawer.Apply(prop, extra);
                    break;
                case "Bool":
                    BoolDrawer.Push(prop);
                    break;
                case "Ramp":
                    int texelSize = extra.Length > 0 ? RampDrawer.GetTexelSize(extra[0]) : RampDrawer.DefaultTexelSize;
                    RampDrawer.Push(prop, texelSize);
                    break;
                case "Queue":
                    QueueDrawer.Push(prop);
                    break;
                case "Cull":
                    CullDrawer.Push(prop);
                    break;
                case "ZTest":
                    ZTestDrawer.Push(prop);
                    break;
                case "ZWrite":
                    ZWriteDrawer.Push(prop);
                    break;
                case "SrcBlend":
                    SrcBlendDrawer.Push(prop);
                    break;
                case "DstBlend":
                    DstBlendDrawer.Push(prop);
                    break;
                case "BlendOp":
                    BlendOpDrawer.Push(prop);
                    break;
                case "QueueOffset":
                    QueueOffsetDrawer.Push(prop);
                    break;
            }
        }
    }

    public Dictionary<string, List<string>> GroupProps = new Dictionary<string, List<string>>();
    public Dictionary<string, (string, string[])> GroupFeatures = new Dictionary<string, (string, string[])>();
    public Dictionary<string, string> TextureFeatures = new Dictionary<string, string>();
    public Dictionary<string, string[]> Features = new Dictionary<string, string[]>();
    public List<string> BoolProps = new List<string>();
    public Dictionary<string, int> RampProps = new Dictionary<string, int>();
    public string QueueProp, CullProp, ZTestProp, ZWriteProp, SrcBlendProp, DstBlendProp, BlendOpProp, QueueOffsetProp;
}

public abstract class DrawerBase : MaterialPropertyDrawer
{
    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor) { }

    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {
        return 0;
    }
}

public class GroupDrawer : DrawerBase
{
    public static bool IsInGroup(string header, MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (prop == null)
            return false;
        if (!data.GroupProps.ContainsKey(header))
            return false;
        return data.GroupProps[header].Contains(prop.name);
    }

    static void PushPorp(string header, MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (prop == null)
            return;
        if (!data.GroupProps.TryGetValue(header, out List<string> propList))
        {
            propList = new List<string>();
            data.GroupProps.Add(header, propList);
        }
        if (!propList.Contains(prop.name))
            propList.Add(prop.name);
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
    }

    public static void Apply(MaterialProperty prop, string[] data)
    {
        if (data == null || data.Length == 0)
            return;
        string header = data[0];
        string extraType = data.Length > 1 ? data[1] : "";
        string[] extra = data.Length > 2 ? new string[data.Length - 2] : null;
        for (int i = 2; i < data.Length; i++)
            extra[i - 2] = data[i];
        ExtraDrawerType type = ExtraDrawerType.None;
        switch (extraType)
        {
            case "Feature":
                if (extra != null && extra.Length != 0)
                    type = ExtraDrawerType.Feature;
                break;
            case "TextureFeature":
                if (extra != null && extra.Length != 0)
                    type = ExtraDrawerType.TextureFeature;
                break;
            case "Bool":
                type = ExtraDrawerType.Bool;
                break;
            case "Ramp":
                type = ExtraDrawerType.Ramp;
                break;
        }
        PushPorp(header, prop);
        switch (type)
        {
            case ExtraDrawerType.Feature:
                FeatureDrawer.Push(extra, prop);
                break;
            case ExtraDrawerType.TextureFeature:
                TextureFeatureDrawer.Push(extra[0], prop);
                break;
            case ExtraDrawerType.Bool:
                BoolDrawer.Push(prop);
                break;
            case ExtraDrawerType.Ramp:
                int texelSize = extra.Length > 0 ? RampDrawer.GetTexelSize(extra[0]) : RampDrawer.DefaultTexelSize;
                RampDrawer.Push(prop, texelSize);
                break;
        }
    }

    string header;
    ExtraDrawerType extraType;
    string[] extra;

    public GroupDrawer(string header)
    {
        this.header = header;
    }

    public GroupDrawer(string header, string extraType, params string[] extra)
    {
        this.header = header;
        switch (extraType)
        {
            case "Feature":
                if (extra != null && extra.Length != 0)
                    this.extraType = ExtraDrawerType.Feature;
                break;
            case "TextureFeature":
                if (extra != null && extra.Length != 0)
                    this.extraType = ExtraDrawerType.TextureFeature;
                break;
            case "Bool":
                this.extraType = ExtraDrawerType.Bool;
                break;
            case "Ramp":
                this.extraType = ExtraDrawerType.Ramp;
                break;
        }
        this.extra = extra;
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        PushPorp(header, prop);
        switch (extraType)
        {
            case ExtraDrawerType.Feature:
                FeatureDrawer.Push(extra, prop);
                break;
            case ExtraDrawerType.TextureFeature:
                TextureFeatureDrawer.Push(extra[0], prop);
                break;
            case ExtraDrawerType.Bool:
                BoolDrawer.Push(prop);
                break;
            case ExtraDrawerType.Ramp:
                int texelSize = extra.Length > 0 ? RampDrawer.GetTexelSize(extra[0]) : RampDrawer.DefaultTexelSize;
                RampDrawer.Push(prop, texelSize);
                break;
        }
    }
}

public class GroupFeatureDrawer : DrawerBase
{
    public static void Push(MaterialProperty prop, string group, string[] feature)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;

        for (int i = 0; i < feature.Length; i++)
        {
            string _feature = feature[i];
            if (string.IsNullOrEmpty(_feature))
                continue;
            for (int j = 0; j < prop.targets.Length; j++)
            {
                Material mat = prop.targets[j] as Material;
                bool res = prop.type == MaterialProperty.PropType.Texture ? mat.GetTexture(prop.name) != null : feature.Length == 1 ? mat.GetFloat(prop.name) == 1 : mat.GetFloat(prop.name) == i;
                if (mat.IsKeywordEnabled(_feature) == res)
                    continue;
                if (res)
                    mat.EnableKeyword(_feature);
                else
                    mat.DisableKeyword(_feature);
            }
        }

        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures[prop.name] = (group, feature);
        else
            data.GroupFeatures.Add(prop.name, (group, feature));

        if (data.GroupProps.TryGetValue(group, out List<string> props))
        {
            int index = props.IndexOf(prop.name);
            if (index > -1)
                props.RemoveAt(index);
        }
        else
            data.GroupProps.Add(group, new List<string>());
        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public static void Apply(MaterialProperty prop, string[] data)
    {
        if (data == null || data.Length <= 1)
            return;
        string group = data[0];
        string[] feature = new string[data.Length - 1];
        for (int i = 1; i < data.Length; i++)
            feature[i - 1] = data[i];
        Push(prop, group, feature);
    }

    string group;
    string[] feature;

    public GroupFeatureDrawer(string group, params string[] feature)
    {
        this.feature = feature;
        this.group = group;
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop, group, feature);
    }
}

public class TextureFeatureDrawer : DrawerBase
{
    string feature;

    public static void Push(string feature, MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        for (int j = 0; j < prop.targets.Length; j++)
        {
            Material mat = prop.targets[j] as Material;
            bool res = mat.GetTexture(prop.name) != null;
            if (mat.IsKeywordEnabled(feature) == res)
                continue;
            if (res)
                mat.EnableKeyword(feature);
            else
                mat.DisableKeyword(feature);
        }

        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures[prop.name] = feature;
        else
            data.TextureFeatures.Add(prop.name, feature);

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public static void Apply(MaterialProperty prop, string[] data)
    {
        if (data == null || data.Length == 0)
            return;
        string feature = data[0];
        Push(feature, prop);
    }

    public TextureFeatureDrawer(string feature)
    {
        this.feature = feature;
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(feature, prop);
    }
}

public class FeatureDrawer : DrawerBase
{
    string[] feature;

    public static void Push(string[] feature, MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        for (int i = 0; i < feature.Length; i++)
        {
            string _feature = feature[i];
            if (string.IsNullOrEmpty(_feature))
                continue;
            for (int j = 0; j < prop.targets.Length; j++)
            {
                Material mat = prop.targets[j] as Material;
                bool res = feature.Length == 1 ? mat.GetFloat(prop.name) == 1 : mat.GetFloat(prop.name) == i;
                if (mat.IsKeywordEnabled(_feature) == res)
                    continue;
                if (res && _feature.ToUpper() != "_NONE")
                    mat.EnableKeyword(_feature);
                else
                    mat.DisableKeyword(_feature);
            }
        }

        if (data.Features.ContainsKey(prop.name))
            data.Features[prop.name] = feature;
        else
            data.Features.Add(prop.name, feature);

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public static void Apply(MaterialProperty prop, string[] data)
    {
        if (data == null || data.Length == 0)
            return;
        string[] feature = data;
        Push(feature, prop);
    }

    public FeatureDrawer(params string[] feature)
    {
        this.feature = feature;
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(feature, prop);
    }
}

public class BoolDrawer : DrawerBase
{
    public static void Push(MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        if (!data.BoolProps.Contains(prop.name))
            data.BoolProps.Add(prop.name);

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop);
    }
}

public class RampDrawer : DrawerBase
{
    public static readonly int DefaultTexelSize = 512;
    int texelSize = DefaultTexelSize;

    public static int GetTexelSize(string extra)
    {
        if (string.IsNullOrEmpty(extra))
            return DefaultTexelSize;
        string str = extra.Substring(1, extra.Length - 1);
        return int.Parse(str);
    }

    public static void Push(MaterialProperty prop, int texelSize)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        if (!data.RampProps.ContainsKey(prop.name))
            data.RampProps.Add(prop.name, texelSize);
        else
            data.RampProps[prop.name] = texelSize;

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
    }

    public RampDrawer(int texelSize)
    {
        this.texelSize = texelSize;
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop, texelSize);
    }
}

public class QueueDrawer : DrawerBase
{
    public static void Push(MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        data.QueueProp = prop.name;

        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop);
    }
}

public class CullDrawer : DrawerBase
{
    public static void Push(MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        data.CullProp = prop.name;

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop);
    }
}

public class ZTestDrawer : DrawerBase
{
    public static void Push(MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        data.ZTestProp = prop.name;

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop);
    }
}

public class ZWriteDrawer : DrawerBase
{
    public static void Push(MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        data.ZWriteProp = prop.name;

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop);
    }
}

public class SrcBlendDrawer : DrawerBase
{
    public static void Push(MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        data.SrcBlendProp = prop.name;

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop);
    }
}

public class DstBlendDrawer : DrawerBase
{
    public static void Push(MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        data.DstBlendProp = prop.name;

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop);
    }
}

public class BlendOpDrawer : DrawerBase
{
    public static void Push(MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        data.BlendOpProp = prop.name;

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.QueueOffsetProp == prop.name)
            data.QueueOffsetProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop);
    }
}

public class QueueOffsetDrawer : DrawerBase
{
    public static void Push(MaterialProperty prop)
    {
        DrawerData data = DrawerData.Pop(prop);
        if (data == null)
            return;
        data.QueueOffsetProp = prop.name;

        if (data.QueueProp == prop.name)
            data.QueueProp = "";
        if (data.CullProp == prop.name)
            data.CullProp = "";
        if (data.ZTestProp == prop.name)
            data.ZTestProp = "";
        if (data.ZWriteProp == prop.name)
            data.ZWriteProp = "";
        if (data.SrcBlendProp == prop.name)
            data.SrcBlendProp = "";
        if (data.DstBlendProp == prop.name)
            data.DstBlendProp = "";
        if (data.BlendOpProp == prop.name)
            data.BlendOpProp = "";
        if (data.GroupFeatures.ContainsKey(prop.name))
            data.GroupFeatures.Remove(prop.name);
        if (data.Features.ContainsKey(prop.name))
            data.Features.Remove(prop.name);
        if (data.TextureFeatures.ContainsKey(prop.name))
            data.TextureFeatures.Remove(prop.name);
        if (data.BoolProps.Contains(prop.name))
            data.BoolProps.Remove(prop.name);
        if (data.RampProps.ContainsKey(prop.name))
            data.RampProps.Remove(prop.name);
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        Push(prop);
    }
}

public enum ExtraDrawerType
{
    None,
    Feature,
    TextureFeature,
    Bool,
    Ramp
}