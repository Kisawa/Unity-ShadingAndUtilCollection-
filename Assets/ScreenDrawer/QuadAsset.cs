using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreenDrawer
{
    [CreateAssetMenu(menuName = "Screen Draw/Asset/Quad Asset")]
    public class QuadAsset : AssetBase
    {
        public Material mat;

        public override Material material => mat;
    }
}