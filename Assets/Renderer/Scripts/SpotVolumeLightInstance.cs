using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, DisallowMultipleComponent, RequireComponent(typeof(Light)), RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class SpotVolumeLightInstance : MonoBehaviour
{
    static readonly int _LightIndex = Shader.PropertyToID("_LightIndex");
    static readonly int _ColorTint = Shader.PropertyToID("_ColorTint");
    static readonly int _Color = Shader.PropertyToID("_Color");
    static readonly int _ShapeSoftness = Shader.PropertyToID("_ShapeSoftness");
    static readonly int _ScatterStrength = Shader.PropertyToID("_ScatterStrength");
    static readonly int _CookieSmoothness = Shader.PropertyToID("_CookieSmoothness");
    static readonly int _IlluminantRadius = Shader.PropertyToID("_IlluminantRadius");
    static readonly int _FadeOutDistance = Shader.PropertyToID("_FadeOutDistance");

    public int LightIndex = 0;
    [ColorUsage(true, true)]
    public Color ColorTint = Color.white;
    [Range(0, 15)]
    public float AngleBias = 1;
    [Range(0, 1)]
    public float ScatterStrength = .05f;
    [Range(0, 1)]
    public float CookieSmoothness = .5f;
    [Range(0, .05f)]
    public float IlluminantRadius = 0;
    public float FadeOutDistance = 10;

    Transform trans;
    Light _light;
    Renderer _renderer;
    MaterialPropertyBlock block;

    private void OnEnable()
    {
        trans = transform;
        _light = GetComponent<Light>();
        _renderer = GetComponent<Renderer>();
#if UNITY_EDITOR
        _renderer.hideFlags = HideFlags.HideInInspector;
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.hideFlags = HideFlags.HideInInspector;
        if (meshFilter.sharedMesh == null)
            meshFilter.sharedMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/DefaultAssets/SpotVolumeLightMesh.mesh");
        if(_renderer.sharedMaterial == null)
            _renderer.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Renderer/DefaultMaterial/Default_SpotVolumeLight.mat");
#endif
        block = new MaterialPropertyBlock();
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (_light.type != LightType.Spot)
            return;
        float radius = Mathf.Sin((_light.spotAngle + AngleBias) * Mathf.Deg2Rad * .5f) * _light.range;
        Vector3 scale = new Vector3(radius * 2, radius * 2, _light.range);
        trans.localScale = scale;

        if (SpotVolumeLightHelper.Self != null)
            LightIndex = SpotVolumeLightHelper.Self.GetLightIndex(_light);
        block.SetInt(_LightIndex, LightIndex);
        block.SetColor(_ColorTint, ColorTint);
        block.SetColor(_Color, _light.color * Mathf.LinearToGammaSpace(_light.intensity) * .1f);
        float shapeSoftness = 1 - _light.innerSpotAngle / _light.spotAngle;
        block.SetFloat(_ShapeSoftness, shapeSoftness);
        block.SetFloat(_ScatterStrength, ScatterStrength);
        block.SetFloat(_CookieSmoothness, CookieSmoothness);
        block.SetFloat(_IlluminantRadius, IlluminantRadius);
        block.SetFloat(_FadeOutDistance, FadeOutDistance);
        _renderer.SetPropertyBlock(block);
    }
}