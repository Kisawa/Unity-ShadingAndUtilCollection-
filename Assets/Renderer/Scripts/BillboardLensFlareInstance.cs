using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, DisallowMultipleComponent]
public class BillboardLensFlareInstance : MonoBehaviour
{
    class ShaderProp
    {
        public static readonly int _BaseColor = Shader.PropertyToID("_BaseColor");
        public static readonly int _VisibilityTestRadius = Shader.PropertyToID("_VisibilityTestRadius");
        public static readonly int _DepthTestBias = Shader.PropertyToID("_DepthTestBias");
        public static readonly int _StartFadeinDistanceWorldUnit = Shader.PropertyToID("_StartFadeinDistanceWorldUnit");
        public static readonly int _EndFadeinDistanceWorldUnit = Shader.PropertyToID("_EndFadeinDistanceWorldUnit");
        public static readonly int _FlickerSpeed = Shader.PropertyToID("_FlickerSpeed");
        public static readonly int _FlickerSeed = Shader.PropertyToID("_FlickerSeed");
        public static readonly int _FlickerFadeinMin = Shader.PropertyToID("_FlickerFadeinMin");
        public static readonly int _FlickerSwell = Shader.PropertyToID("_FlickerSwell");
    }

    [ColorUsage(true, true)]
    public Color BaseColor = Color.white;
    [Space(10)]
    [UnityEngine.Range(0, 1)]
    public float VisibilityTestRadius = .05f;
    [UnityEngine.Range(-1, 1)]
    public float DepthTestBias = -.001f;
    [Space(10)]
    public float StartFadeinDistanceWorldUnit = .05f;
    public float EndFadeinDistanceWorldUnit = .5f;
    [Space(10)]
    [UnityEngine.Range(0, 5)]
    public float FlickerSpeed = 3;
    [UnityEngine.Range(0, 1)]
    public float FlickerSeed = 0;
    [UnityEngine.Range(0, 1)]
    public float FlickerFadeinMin = .5f;
    [UnityEngine.Range(0, 3)]
    public float FlickerSwell = 1.2f;

    new Renderer renderer;

    private void OnEnable()
    {
        renderer = GetComponent<MeshRenderer>();
        if (renderer != null && renderer.sharedMaterial.shader.name != "Project/BillboardLensFlare")
            renderer = null;
        RefreshMat();
    }

    private void Update()
    {
        RefreshMat();
    }

    void RefreshMat()
    {
        if (renderer == null)
            return;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor(ShaderProp._BaseColor, BaseColor);
        block.SetFloat(ShaderProp._VisibilityTestRadius, VisibilityTestRadius);
        block.SetFloat(ShaderProp._DepthTestBias, DepthTestBias);
        block.SetFloat(ShaderProp._StartFadeinDistanceWorldUnit, StartFadeinDistanceWorldUnit);
        block.SetFloat(ShaderProp._EndFadeinDistanceWorldUnit, EndFadeinDistanceWorldUnit);
        block.SetFloat(ShaderProp._FlickerSpeed, FlickerSpeed);
        block.SetFloat(ShaderProp._FlickerSeed, FlickerSeed);
        block.SetFloat(ShaderProp._FlickerFadeinMin, FlickerFadeinMin);
        block.SetFloat(ShaderProp._FlickerSwell, FlickerSwell);
        renderer.SetPropertyBlock(block);
    }
}