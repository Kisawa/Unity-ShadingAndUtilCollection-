using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class StarRailRenderer : MonoBehaviour
{
    static readonly int _HeadForwardDirection = Shader.PropertyToID("_HeadForwardDirection");
    static readonly int _HeadRightDirection = Shader.PropertyToID("_HeadRightDirection");

    public Transform HeadBone;
    public Transform HeadBoneForward;
    public Transform HeadBoneRight;

    Renderer[] renderers;
    MaterialPropertyBlock block;

    private void OnEnable()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        if(block == null)
            block = new MaterialPropertyBlock();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            renderer.GetPropertyBlock(block);
            if (HeadBone != null)
            {
                if (HeadBoneForward != null)
                    block.SetVector(_HeadForwardDirection, Vector3.Normalize(HeadBoneForward.position - HeadBone.position));
                if (HeadBoneRight != null)
                    block.SetVector(_HeadRightDirection, Vector3.Normalize(HeadBoneRight.position - HeadBone.position));
            }
            renderer.SetPropertyBlock(block);
        }
    }
}