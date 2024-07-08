using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreenDrawer
{
    public class AssetBase : ScriptableObject
    {
        public static int _MainTex = Shader.PropertyToID("_MainTex");

        public Mesh mesh { get; private set; }
        public virtual Material material { get; }

        public AnchorType anchor;
        public RuntimeVal<Vector4> tilingOffset = new RuntimeVal<Vector4>(new Vector4(1, 1, 0, 0));
        public Vector2Int pixelSize;
        public List<int> Pass = new List<int>() { 0 };

        public virtual void Enable()
        {
            RefreshMesh();
        }

        public virtual void Disable()
        {
            if (mesh != null)
                DestroyImmediate(mesh);
        }

        public virtual void UpdateFrame()
        {

        }

        public virtual void RefreshMesh()
        {
            if (mesh != null)
                DestroyImmediate(mesh);
            Vector2 vert = Vector2.one;
            if (pixelSize.x != 0 && pixelSize.y != 0)
            {
                float s = Camera.main == null ? 1 : Camera.main.aspect;
                float p = (float)pixelSize.x / pixelSize.y;
                vert.x = p / s;
            }

            float topV = 1.0f;
            float bottomV = 0.0f;
            mesh = new Mesh { name = "ScreenDrawer Quad" };

            Vector3 offset = Vector3.zero;
            switch (anchor)
            {
                case AnchorType.Top:
                    offset = Vector3.down;
                    break;
                case AnchorType.Botton:
                    offset = Vector3.up;
                    break;
                case AnchorType.Left:
                    offset = Vector3.right;
                    break;
                case AnchorType.Right:
                    offset = Vector3.left;
                    break;
                case AnchorType.TopLeft:
                    offset = new Vector3(1, -1, 0);
                    break;
                case AnchorType.TopRight:
                    offset = new Vector3(-1, -1, 0);
                    break;
                case AnchorType.BottonLeft:
                    offset = new Vector3(1, 1, 0);
                    break;
                case AnchorType.BottonRight:
                    offset = new Vector3(-1, 1, 0);
                    break;
            }
            offset *= vert;
            mesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-vert.x, -vert.y, 0) + offset,
                    new Vector3(-vert.x,  vert.y, 0) + offset,
                    new Vector3(vert.x, -vert.y, 0) + offset,
                    new Vector3(vert.x,  vert.y, 0) + offset
                });

            mesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0, bottomV),
                    new Vector2(0, topV),
                    new Vector2(1, bottomV),
                    new Vector2(1, topV)
                });

            mesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
            mesh.UploadMeshData(true);
        }
    }

    public enum AnchorType { Center, Top, Botton, Left, Right, TopLeft, TopRight, BottonLeft, BottonRight }
}