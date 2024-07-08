using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancingUtil;
using System.Linq;

[ExecuteInEditMode]
public class InstancingController : MonoBehaviour
{
    public static List<InstancingController> List = new List<InstancingController>();

    static int _InstancingBuffer = Shader.PropertyToID("_InstancingBuffer");
    static int _PositionOffset = Shader.PropertyToID("_PositionOffset");

    public InstancingData Data;
    public Vector3 Offset;

    public ComputeBuffer instancingBuffer { get; private set; }
    public ComputeBuffer argsBuffer { get; private set; }
    public Material material { get; private set; }
    public Vector3 PositionOffset => trans.position + Offset;

    Transform trans;

    private void Awake()
    {
        trans = transform;
    }

    private void OnEnable()
    {
        List.Add(this);
        RefreshInstancingView();
    }

    private void OnDisable()
    {
        List.Remove(this);
        ClearBuffer();
    }

    public bool IsAvailable()
    {
        if (!DataUseful() || instancingBuffer == null || argsBuffer == null || material == null)
            return false;
        return true;
    }

    bool DataUseful()
    {
        if (Data == null || Data.UseMesh == null || Data.UseMat == null)
            return false;
        return true;
    }

    public void ResetOffset()
    {
        Offset = -trans.position;
    }

    public void RefreshInstancingView()
    {
        if (!DataUseful())
            return;
        ClearBuffer();
        if (Data.InstancingList.Count == 0)
            return;
#if UNITY_EDITOR
        //if (Application.isPlaying)
        //    material = Instantiate(Data.UseMat);
        //else
            material = Data.UseMat;
#else
        material = Instantiate(Data.UseMat);
#endif

        if (Data.ColorRequired)
        {
            instancingBuffer = new ComputeBuffer(Data.InstancingList.Count, sizeof(float) * 11);
            instancingBuffer.SetData(Data.InstancingList);
        }
        else
        {
            List<Instancing> instancings = Data.InstancingList.Select(x => new Instancing(x.positionWS, x.rotateScale)).ToList();
            instancingBuffer = new ComputeBuffer(instancings.Count, sizeof(float) * 8);
            instancingBuffer.SetData(instancings);
        }
        material.SetBuffer(_InstancingBuffer, instancingBuffer);

        argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = Data.UseMesh.GetIndexCount(0);
        args[1] = (uint)Data.InstancingList.Count;
        args[2] = Data.UseMesh.GetIndexStart(0);
        args[3] = Data.UseMesh.GetBaseVertex(0);
        argsBuffer.SetData(args);
    }

    void ClearBuffer()
    {
        if (instancingBuffer != null)
            instancingBuffer.Release();
        instancingBuffer = null;
        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
#if UNITY_EDITOR
        //if (Application.isPlaying)
        //{
        //    if (material != null)
        //        DestroyImmediate(material);
        //}
#else
        if (material != null)
                    DestroyImmediate(material);
#endif
    }
}