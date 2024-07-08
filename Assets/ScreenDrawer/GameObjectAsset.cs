using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScreenDrawer
{
    [CreateAssetMenu(menuName = "Screen Draw/Asset/GameObject Asset")]
    public class GameObjectAsset : AssetBase
    {
        public static readonly string TextureShader = "Project/SingleScreenDrawerLoader";
        public static readonly string IgnoreMainRendererLayer = "IgnoreMainRenderer";
        public static readonly Vector3 Position = Vector3.zero;

        public GameObject Prefab;
        public Vector3 Offset;
        public Vector3 eularAngle;
        public Vector3 Scale = Vector3.one;
        public Vector2 Bias;

        public CommandBuffer cmd { get; private set; }
        public GameObject gameObject { get; private set; }
        public Renderer[] renderers { get; private set; }
        public RenderTexture texture { get; private set; }
        public Bounds bounds { get; private set; }

        public Material mat;
        public override Material material => mat;

        public override void Enable()
        {
            if (Prefab == null)
                return;
            base.Enable();
        }

        public override void RefreshMesh()
        {
            if (Prefab == null)
                return;
            Disable();
            base.RefreshMesh();
            Shader shader = Shader.Find(TextureShader);
            if (shader == null)
            {
                Debug.LogError("Skill Anime Setting: Texture view shader not found.");
                return;
            }
            if (mat == null)
                mat = CoreUtils.CreateEngineMaterial(shader);
            gameObject = Instantiate(Prefab, Position + Offset, Quaternion.Euler(eularAngle));
            gameObject.transform.localScale = Scale;
            gameObject.hideFlags = HideFlags.DontSave;
            renderers = gameObject.GetComponentsInChildren<Renderer>();
            renderers = RendererWhere(renderers, x => x.enabled);
            if (renderers.Length > 0)
            {
                LayerMask layer = LayerMask.NameToLayer(IgnoreMainRendererLayer);
                Renderer renderer = renderers[0];
                renderer.gameObject.layer = layer;
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    Renderer _renderer = renderers[i];
                    _renderer.gameObject.layer = layer;
                    bounds.Encapsulate(_renderer.bounds);
                }
            }
            Vector2Int pixelSize = this.pixelSize;
            if (pixelSize.x == 0 || pixelSize.y == 0)
            {
                Camera cam = Camera.main;
                pixelSize = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            }
            texture = RenderTexture.GetTemporary(pixelSize.x, pixelSize.y, 0, RenderTextureFormat.ARGB32);
            mat.SetTexture(_MainTex, texture);
            cmd = new CommandBuffer();
        }

        public override void Disable()
        {
            base.Disable();
            if (gameObject != null)
                DestroyImmediate(gameObject);
            if (texture != null)
            {
                RenderTexture.ReleaseTemporary(texture);
                texture = null;
            }
            if (cmd != null)
            {
                cmd.Release();
                cmd = null;
            }
        }

        public override void UpdateFrame()
        {
            if (cmd == null || texture == null || renderers == null || renderers.Length == 0)
                return;
            cmd.Clear();
            cmd.SetRenderTarget(texture);
            cmd.ClearRenderTarget(true, true, Color.clear);

            Vector2 min = bounds.center - bounds.extents;
            min -= Bias * .5f;
            Vector2 max = bounds.center + bounds.extents;
            max += Bias * .5f;
            Matrix4x4 P = Matrix4x4.Ortho(min.x, max.x, min.y, max.y, float.MinValue, float.MaxValue);
            cmd.SetViewport(new Rect(Vector2.zero, pixelSize));
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, P);
            for (int n = 0; n < renderers.Length; n++)
            {
                Renderer renderer = renderers[n];
                cmd.DrawRenderer(renderer, renderer.sharedMaterial);
            }
            Graphics.ExecuteCommandBuffer(cmd);
        }

        Renderer[] RendererWhere(Renderer[] renderers, Func<Renderer, bool> condition)
        {
            List<Renderer> _renderers = new List<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if(condition == null || condition.Invoke(renderer))
                    _renderers.Add(renderer);
            }
            return _renderers.ToArray();
        }
    }
}