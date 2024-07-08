using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GPUInstancingUtil
{
    [CreateAssetMenu(menuName = "GPU Instancing Util/Instancing Data")]
    public class InstancingData : ScriptableObject
    {
        public bool ColorRequired;
        public Mesh UseMesh;
        public Material UseMat;
        public List<InstancingColor> InstancingList = new List<InstancingColor>();
    }

    [System.Serializable]
    public struct Instancing
    {
        public Instancing(Vector4 positionWS, Vector4 rotateScale)
        {
            this.positionWS = positionWS;
            this.rotateScale = rotateScale;
        }

        public Vector4 positionWS;
        public Vector4 rotateScale;
    }

    [System.Serializable]
    public struct InstancingColor
    {
        public InstancingColor(Vector4 positionWS, Vector4 rotateScale)
        {
            this.positionWS = positionWS;
            this.rotateScale = rotateScale;
            color = Vector3.one;
        }

        public Vector4 positionWS;
        public Vector4 rotateScale;
        public Vector3 color;
    }
}