using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class EngineUtil
{
    [MenuItem("Tools/Screenshot %#&A")]
    static void Screenshot()
    {
        System.TimeSpan ts = System.DateTime.Now - new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
        string name = $"{System.Convert.ToInt64(ts.TotalSeconds)}.png";
        string path = $"Assets/Test/Screenshot/{name}";
        CheckFolder("Assets", "Test");
        CheckFolder("Assets/Test", "Screenshot");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"Screenshot success, please refresh with Ctrl-R: The path is in \"{path}\"");
    }

    [MenuItem("Tools/Split Texture", true)]
    static bool CheckSplitTextureEnable()
    {
        Object[] objs = Selection.objects;
        for (int i = 0; i < objs.Length; i++)
        {
            Texture tex = objs[i] as Texture;
            if (tex != null)
                return true;
        }
        return false;
    }

    [MenuItem("Tools/Split Texture")]
    static void SplitTexture()
    {
        Vector2Int cell = new Vector2Int(3, 3);
        ComputeShader commonCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Editor/EngineUtilCS.compute");
        int kernel_copyPixel = commonCS.FindKernel("CopyPixel");

        Object[] objs = Selection.objects;
        for (int i = 0; i < objs.Length; i++)
        {
            Texture tex = objs[i] as Texture;
            if (tex == null)
                continue;
            string path = AssetDatabase.GetAssetPath(tex);
            string folder = Path.GetDirectoryName(path);
            string subFolder = CheckFolder(folder, tex.name);
            int widthTol = 0, heightTol = 0;
            for (int x = 0; x < cell.x; x++)
            {
                int width = Mathf.CeilToInt((tex.width - widthTol) / (cell.x - x));
                for (int y = 0; y < cell.y; y++)
                {
                    int height = Mathf.CeilToInt((tex.height - heightTol) / (cell.y - y));
                    RenderTexture renderTexture = new RenderTexture(width, height, 0);
                    renderTexture.enableRandomWrite = true;
                    renderTexture.Create();
                    commonCS.SetTexture(kernel_copyPixel, "_OriginTex", tex);
                    commonCS.SetTexture(kernel_copyPixel, "RW_Result", renderTexture);
                    commonCS.SetVector("_Offset", new Vector2(widthTol, heightTol));
                    commonCS.Dispatch(kernel_copyPixel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
                    string _path = $"{subFolder}/{tex.name}_{x * cell.y + y}.png";
                    SaveToPNG(renderTexture, _path, TextureFormat.ARGB32);
                    heightTol += height;
                }
                widthTol += width;
                heightTol = 0;
            }
        }
    }

    [MenuItem("Tools/Composite Sequence", true)]
    static bool CheckCompositeSequenceEnable()
    {
        Texture[] texs = Selection.GetFiltered<Texture>(SelectionMode.DeepAssets);
        return texs.Length > 0;
    }

    [MenuItem("Tools/Composite Sequence", false)]
    static void CompositeSequence()
    {
        Texture[] texs = Selection.GetFiltered<Texture>(SelectionMode.DeepAssets);
        if (texs.Length == 0)
            return;
        string path = AssetDatabase.GetAssetPath(texs[0]);
        string[] strs = path.Split('/');
        CompositeSequence(strs[strs.Length - 2], texs);
    }

    public static Texture[] GetValidSequence(Texture[] textures)
    {
        List<Texture> validTextures = new List<Texture>();
        for (int i = 0; i < textures.Length; i++)
        {
            Texture tex = textures[i];
            if (TryGetSequenceIndex(tex, out int index))
                validTextures.Add(tex);
        }
        return validTextures.OrderBy(x => GetSequenceIndex(x)).ToArray();
    }

    public static string CompositeSequence(string name, Texture[] textures, int downSample = 1, int hor = -1, bool invVer = false)
    {
        downSample = Mathf.Max(1, downSample);
        ComputeShader commonCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Editor/CommonCS.compute");
        int kernel_writePixel = commonCS.FindKernel("WritePixel");

        Texture[] validTextures = GetValidSequence(textures);
        if (validTextures.Length == 0)
            return "";
        if (hor == -1)
            hor = Mathf.CeilToInt(Mathf.Sqrt(validTextures.Length));
        int ver = Mathf.CeilToInt((float)validTextures.Length / hor);
        int singleWidth = 0, singleHeight = 0;
        for (int i = 0; i < validTextures.Length; i++)
        {
            Texture tex = validTextures[i];
            singleWidth = Mathf.Max(tex.width, singleWidth);
            singleHeight = Mathf.Max(tex.height, singleHeight);
        }
        singleWidth = singleWidth / downSample;
        singleHeight = singleHeight / downSample;
        int width = singleWidth * hor, height = singleHeight * ver;

        RenderTexture renderTexture = new RenderTexture(width, height, 0);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        int count = validTextures.Length;
        for (int i = 0; i < count; i++)
        {
            int x = i % hor;
            int y = i / hor;
            int index = invVer ? (ver - y - 1) * hor + x : i;
            if (index >= validTextures.Length)
            {
                count++;
                continue;
            }
            Texture tex = validTextures[index];
            commonCS.SetTexture(kernel_writePixel, "_OriginTex", tex);
            commonCS.SetTexture(kernel_writePixel, "RW_Result", renderTexture);
            commonCS.SetVector("_Offset", new Vector2(singleWidth * x, singleHeight * y));
            Vector2 _downSample = new Vector2((float)tex.width / downSample / singleWidth, (float)tex.height / downSample / singleHeight) * downSample;
            commonCS.SetVector("_DownSample", _downSample);
            commonCS.Dispatch(kernel_writePixel, Mathf.CeilToInt(singleWidth / 8f), Mathf.CeilToInt(singleHeight / 8f), 1);
        }
        string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(validTextures[0]));
        string _path = $"{folder}/../{name}";
        string texPath = $"{_path}.png";
        SaveToARGB32(renderTexture, texPath);
        return texPath;
    }

    public static void SaveToARGB32(RenderTexture renderTexture, string path)
    {
        SaveToPNG(renderTexture, path, TextureFormat.ARGB32);
    }

    public static void SaveToRGB24(RenderTexture renderTexture, string path)
    {
        SaveToPNG(renderTexture, path, TextureFormat.RGB24);
    }

    static void SaveToPNG(RenderTexture renderTexture, string path, TextureFormat format)
    {
        if (renderTexture == null || string.IsNullOrWhiteSpace(path))
            return;
        RenderTexture pre = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, format, false);
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = pre;
        byte[] bytes = ImageConversion.EncodeToPNG(tex);
        Object.DestroyImmediate(tex);
        string _path = $"{Application.dataPath}/../{path}";
        if (!_path.EndsWith(".png"))
            _path += ".png";
        string directoryPath = System.IO.Path.GetDirectoryName(_path);
        System.IO.Directory.CreateDirectory(directoryPath);
        System.IO.File.WriteAllBytes(_path, bytes);
        Debug.Log($"Save png: {_path}");
    }

    static bool TryGetSequenceIndex(Texture tex, out int index)
    {
        if (tex == null)
        {
            index = -1;
            return false;
        }
        string[] nameStrs = tex.name.Split('_');
        string indexStr = nameStrs[nameStrs.Length - 1];
        return int.TryParse(indexStr, out index);
    }

    static int GetSequenceIndex(Texture tex)
    {
        if (tex == null)
            return -1;
        string[] nameStrs = tex.name.Split('_');
        string indexStr = nameStrs[nameStrs.Length - 1];
        return int.Parse(indexStr);
    }

    public static bool HasFolder(string path, string name)
    {
        string mergePath = $"{path}/{name}";
        return AssetDatabase.IsValidFolder(mergePath);
    }

    public static string CheckFolder(string path, string name)
    {
        string mergePath = $"{path}/{name}";
        if (!AssetDatabase.IsValidFolder(mergePath))
            AssetDatabase.CreateFolder(path, name);
        return mergePath;
    }

    public static string CheckName<T>(string folderPath, string name, string suf) where T : Object
    {
        string newName = name;
        int tryIndex = 1;
        while (true)
        {
            string path = $"{folderPath}/{newName}.{suf}";
            if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
                break;
            else
            {
                string end = tryIndex.ToString("D2");
                newName = $"{name}{end}";
                tryIndex++;
            }
        }
        return newName;
    }

    static void Move<T>(string old, string path) where T : Object
    {
        if (old == path)
            return;
        string newPath = path;
        int tryIndex = 0;
        while (true)
        {
            if (AssetDatabase.LoadAssetAtPath<T>(newPath) == null)
                break;
            else
            {
                int end = path.LastIndexOf(".");
                newPath = path.Insert(end, $" {tryIndex}");
                tryIndex++;
            }
        }
        AssetDatabase.MoveAsset(old, newPath);
    }
}