using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UserCreateUtil
{
    [MenuItem("GameObject/Instance/Billboard LensFlare", priority = 1)]
    private static void CreateBillBoardLensFlare()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Renderer/DefaultMaterial/Default_BillboardLensFlare.mat");
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Collider collider = obj.GetComponent<Collider>();
        Object.DestroyImmediate(collider);
        obj.transform.position = SceneView.lastActiveSceneView.pivot;
        obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
        obj.AddComponent<BillboardLensFlareInstance>();
        obj.name = "Billboard LensFlare";
        Undo.RegisterCreatedObjectUndo(obj, "Create Billboard LensFlare");
    }
}