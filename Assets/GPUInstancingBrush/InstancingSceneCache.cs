using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancingUtil 
{
    public class InstancingSceneCache : MonoBehaviour
    {
        public List<PrefabInstanceParent> parentDic = new List<PrefabInstanceParent>();
        public List<GameObject> instances = new List<GameObject>();

        [System.Serializable]
        public class PrefabInstanceParent
        {
            public GameObject prefab;
            public Transform parent;

            public PrefabInstanceParent(GameObject prefab, Transform parent)
            {
                this.prefab = prefab;
                this.parent = parent;
            }
        }

        public void AddParent(GameObject prefab, Transform parent)
        {
            if (prefab == null || parent == null)
                return;
            for (int i = 0; i < parentDic.Count; i++)
            {
                PrefabInstanceParent prefabInstanceParent = parentDic[i];
                if (prefabInstanceParent.prefab == prefab)
                {
                    prefabInstanceParent.parent = parent;
                    return;
                }
            }
            parentDic.Add(new PrefabInstanceParent(prefab, parent));
        }

        public bool TryGetParent(GameObject prefab, out Transform parent)
        {
            for (int i = 0; i < parentDic.Count; i++)
            {
                PrefabInstanceParent prefabInstanceParent = parentDic[i];
                if (prefabInstanceParent == null)
                    continue;
                if (prefabInstanceParent.prefab == prefab)
                {
                    parent = prefabInstanceParent.parent;
                    return true;
                }
            }
            parent = null;
            return false;
        }
    }
}