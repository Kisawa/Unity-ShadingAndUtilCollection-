using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditorInternal;
using System.Linq;

namespace GPUInstancingUtil
{
    [EditorTool("GPUInstancing Brush Tool")]
    public class GPUInstancingBrush : EditorTool
    {
        public static GPUInstancingBrush Self;
        static readonly int _KernelGroupCount = 64;
        static int _InstancingBuffer = Shader.PropertyToID("_InstancingBuffer");
        static int Append_InstancingBuffer = Shader.PropertyToID("Append_InstancingBuffer");
        static int _Point = Shader.PropertyToID("_Point");
        static int _Normal = Shader.PropertyToID("_Normal");
        static int _BrushRange = Shader.PropertyToID("_BrushRange");
        static int _HitHeight = Shader.PropertyToID("_HitHeight");
        static int _VP = Shader.PropertyToID("_VP");
        static int _CameraOpaqueTexture = Shader.PropertyToID("_CameraOpaqueTexture");
        static int RW_InstancingBuffer = Shader.PropertyToID("RW_InstancingBuffer");

        BrushCache prefabBrushCache;
        BrushCache instanceDataBrushCache;

        public bool Preview;
        public BrushType brushType;
        public int BrushLayer;
        public int MaskLayer;
        public Color BrushViewColor = Color.blue;
        public float BrushRange = 2;
        public float Density = 15;
        public float HitHeight = 2;
        public float Bias = 0;

        public GameObject prefab;
        InstancingSceneCache _sceneCache;
        InstancingSceneCache sceneCache
        {
            get
            {
                if (_sceneCache == null)
                {
                    _sceneCache = FindObjectOfType<InstancingSceneCache>();
                    if (_sceneCache == null)
                    {
                        GameObject obj = new GameObject("Instance Brush Scene Cache");
                        _sceneCache = obj.AddComponent<InstancingSceneCache>();
                    }
                }
                return _sceneCache;
            }
        }

        /// <summary>
        /// Default value friendly Blender
        /// </summary>
        public Axis MeshOriginAxis = Axis.Forward;
        public float ForwardFaceNormal = 1;
        public float RandomSelfRotate = Mathf.PI;
        public float RandomScaleMin = .5f;
        public float RandomScaleMax = 1f;
        public Vector2 RandomValueRange;
        public InstancingData Data;
        public Mesh ViewMesh;
        public Material ViewMat;
        public bool InjectBackgroundColor;

        public ComputeBuffer instancingBuffer { get; private set; }
        public ComputeBuffer argsBuffer { get; private set; }

        ComputeShader calcShader;
        int kernel_Sub;
        int kernel_SampleBackground;
        bool cursorOnGUI;

        List<Vector3> brushRoute = new List<Vector3>();

        private void Awake()
        {
            calcShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/GPUInstancingBrush/Editor/GPUInstancingBrushCalcUtil.compute");
            kernel_Sub = calcShader.FindKernel("Sub");
            kernel_SampleBackground = calcShader.FindKernel("SampleBackground");
        }

        public override void OnActivated()
        {
            base.OnActivated();
            Self = this;
            Undo.undoRedoPerformed += undoRedoPerformed;
            SceneView.duringSceneGui += SceneHandle;
            RefreshInstancingView();
        }

        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            ClearBuffer();
            SceneView.duringSceneGui -= SceneHandle;
            Undo.undoRedoPerformed -= undoRedoPerformed;
        }

        void undoRedoPerformed()
        {
            if (Data != null)
            {
                Data.UseMesh = ViewMesh;
                Data.UseMat = ViewMat;
                EditorUtility.SetDirty(Data);
            }
            RefreshInstancingView();
        }

        public override void OnToolGUI(EditorWindow window)
        {
            base.OnToolGUI(window);
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Keyboard));
            GUIStyle txtStyle = GUI.skin.GetStyle("WhiteMiniLabel");

            float y = window.position.height - (brushType == BrushType.Instancing ? 435 : 335);
            Rect rect = new Rect(window.position.width - 270, y, 260, 405);
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("GPUInstancing Brush Tool", "Badge");
            switch (brushType)
            {
                case BrushType.Instancing:
                    if (Data != null)
                        GUILayout.Label($"{Data.InstancingList.Count}", "Badge", GUILayout.Width(50));
                    break;
                case BrushType.GameObject:
                    if (prefab != null)
                        GUILayout.Label($"{sceneCache.instances.Count}", "Badge", GUILayout.Width(50));
                    break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            string[] layers = GetLayers();
            EditorGUILayout.BeginVertical("Badge");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Brush Type", txtStyle, GUILayout.Width(120));
            BrushType _brushType = (BrushType)EditorGUILayout.EnumPopup(brushType);
            EditorGUILayout.EndHorizontal();

            bool _preview = Preview;
            if (brushType == BrushType.Instancing)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Instancing Preview", txtStyle, GUILayout.Width(120));
                _preview = EditorGUILayout.Toggle(_preview, GUILayout.Width(30));
                EditorGUILayout.LabelField("/V", GUI.skin.GetStyle("LODSliderTextSelected"));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Brush Layer", txtStyle, GUILayout.Width(120));
            int brushLayer = EditorGUILayout.MaskField(BrushLayer, layers);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mask Layer", txtStyle, GUILayout.Width(120));
            int maskLayer = EditorGUILayout.MaskField(MaskLayer, layers);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Brush View Color", txtStyle, GUILayout.Width(120));
            Color brushViewColor = EditorGUILayout.ColorField(BrushViewColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Brush Range", txtStyle, GUILayout.Width(120));
            float brushRange = EditorGUILayout.Slider(BrushRange, .1f, 10);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Density", txtStyle, GUILayout.Width(120));
            float density = EditorGUILayout.Slider(Density, .01f, 100);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hit Height", txtStyle, GUILayout.Width(120));
            float hitHeight = EditorGUILayout.Slider(HitHeight, .1f, 5);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Bias", txtStyle, GUILayout.Width(120));
            float bias = EditorGUILayout.Slider(Bias, -1, 1);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Forward Face Normal", txtStyle, GUILayout.Width(120));
            float forwardFaceNormal = EditorGUILayout.Slider(ForwardFaceNormal, 0, 1);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Random Self Rotate(Up)", txtStyle, GUILayout.Width(120));
            float randomSelfRotate = EditorGUILayout.Slider(RandomSelfRotate, 0, Mathf.PI);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Random Scale", txtStyle, GUILayout.Width(75));
            float randomScaleMin = RandomScaleMin, randomScaleMax = RandomScaleMax;
            randomScaleMin = EditorGUILayout.FloatField(randomScaleMin, GUILayout.Width(35));
            EditorGUILayout.MinMaxSlider(ref randomScaleMin, ref randomScaleMax, .01f, 3);
            randomScaleMax = EditorGUILayout.FloatField(randomScaleMax, GUILayout.Width(35));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Random Value Range", txtStyle, GUILayout.Width(120));
            Vector2 randomValueRange = RandomValueRange;
            randomValueRange = EditorGUILayout.Vector2Field("", randomValueRange, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "GPUInstancing Brush Setting Changed");
                Preview = _preview;
                brushType = _brushType;
                BrushLayer = brushLayer;
                MaskLayer = maskLayer;
                BrushViewColor = brushViewColor;
                BrushRange = brushRange;
                Density = density;
                HitHeight = hitHeight;
                Bias = bias;
                ForwardFaceNormal = forwardFaceNormal;
                RandomSelfRotate = randomSelfRotate;
                RandomScaleMin = randomScaleMin;
                RandomScaleMax = randomScaleMax;
                RandomValueRange = randomValueRange;
                RecordBrush();
                RefreshInstancingView();
            }

            switch (brushType)
            {
                case BrushType.Instancing:
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Mesh Origin Axis", txtStyle, GUILayout.Width(120));
                    Axis meshOriginAxis = (Axis)EditorGUILayout.EnumPopup(MeshOriginAxis);
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "GPUInstancing Brush Setting Changed");
                        MeshOriginAxis = meshOriginAxis;
                        RecordBrush();
                        RefreshInstancingView();
                    }

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("To Data", txtStyle, GUILayout.Width(120));
                    InstancingData data = EditorGUILayout.ObjectField(Data, typeof(InstancingData), false) as InstancingData;
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "GPUInstancing Brush Setting Changed");
                        Data = data;
                        if (Data != null)
                        {
                            if (Data.UseMesh == null)
                                Data.UseMesh = ViewMesh;
                            else
                                ViewMesh = Data.UseMesh;
                            if (Data.UseMat == null)
                                Data.UseMat = ViewMat;
                            else
                                ViewMat = Data.UseMat;
                            InjectBackgroundColor = Data.ColorRequired;
                            EditorUtility.SetDirty(Data);
                        }
                        instanceDataBrushCache = BrushCache.GetBrushCache(Data, this);
                        RefreshInstancingView();
                    }

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("View Mesh", txtStyle, GUILayout.Width(120));
                    Mesh viewMesh = EditorGUILayout.ObjectField(ViewMesh, typeof(Mesh), false) as Mesh;
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "GPUInstancing Brush Setting Changed");
                        ViewMesh = viewMesh;
                        if (Data != null)
                        {
                            Data.UseMesh = ViewMesh;
                            EditorUtility.SetDirty(Data);
                        }
                        RefreshInstancingView();
                    }

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("View Material", txtStyle, GUILayout.Width(120));
                    Material viewMat = EditorGUILayout.ObjectField(ViewMat, typeof(Material), false) as Material;
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "GPUInstancing Brush Setting Changed");
                        ViewMat = viewMat;
                        if (Data != null)
                        {
                            Data.UseMat = ViewMat;
                            EditorUtility.SetDirty(Data);
                        }
                        RefreshInstancingView();
                    }

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    GUIContent content = new GUIContent("Inject Background Color", "OpaqueTexture required");
                    EditorGUILayout.LabelField(content, txtStyle, GUILayout.Width(120));
                    bool _injectBackgroundColor = InjectBackgroundColor;
                    _injectBackgroundColor = EditorGUILayout.Toggle(_injectBackgroundColor, GUILayout.Width(30));
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "GPUInstancing Brush Setting Changed");
                        InjectBackgroundColor = _injectBackgroundColor;
                        if (Data != null)
                        {
                            Data.ColorRequired = _injectBackgroundColor;
                            EditorUtility.SetDirty(Data);
                        }
                        ClearData();
                    }
                    break;
                case BrushType.GameObject:
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Prefab Origin", txtStyle, GUILayout.Width(120));
                    GameObject _prefab = EditorGUILayout.ObjectField(prefab, typeof(GameObject), false) as GameObject;
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "GPUInstancing Brush Setting Changed");
                        prefab = _prefab;
                        prefabBrushCache = BrushCache.GetBrushCache(prefab, this);
                    }
                    break;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(150)))
            {
                RefreshInstances();
                RefreshInstancingView();
            }
            if (GUILayout.Button("Clear"))
                ClearData();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
            cursorOnGUI = rect.Contains(Event.current.mousePosition);
        }

        void SceneHandle(SceneView obj)
        {
            switch (brushType)
            {
                case BrushType.Instancing:
                    if (Data == null || ViewMesh == null || ViewMat == null)
                        return;
                    break;
                case BrushType.GameObject:
                    if (prefab == null)
                        return;
                    break;
            }

            HandleType type = EventMonitor();
            Vector2 pos = obj.camera.ScreenToViewportPoint(Event.current.mousePosition);
            pos.y = 1 - pos.y;
            Ray ray = obj.camera.ViewportPointToRay(pos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100, BrushLayer))
            {
                Handles.color = BrushViewColor;
                Handles.DrawLine(hit.point, hit.point + hit.normal);
                Handles.CircleHandleCap(0, hit.point, Quaternion.LookRotation(hit.normal), BrushRange, EventType.Repaint);
                int count = Mathf.CeilToInt(Density * BrushRange / 10);
                for (int i = 0; i < count; i++)
                    Handles.CircleHandleCap(0, hit.point, Quaternion.LookRotation(hit.normal), BrushRange / count * i, EventType.Repaint);
                switch (type)
                {
                    case HandleType.Add:
                        Add(hit);
                        brushRoute.Add(hit.point);
                        break;
                    case HandleType.Sub:
                        Sub(hit);
                        break;
                }
                SceneView.RepaintAll();
            }
        }

        HandleType EventMonitor()
        {
            if (cursorOnGUI)
                return HandleType.None;
            if (Event.current.isMouse)
            {
                if (Event.current.button == 0)
                {
                    if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                    {
                        if (Event.current.control && !Event.current.alt && !Event.current.shift)
                        {
                            BrushRange += Event.current.delta.x * .05f;
                            BrushRange = BrushRange < .1f ? .1f : BrushRange > 10 ? 10 : BrushRange;
                            RecordBrush();
                        }
                        if (!Event.current.control && !Event.current.alt && Event.current.shift)
                        {
                            Density += Event.current.delta.x * .05f;
                            Density = Density < .01f ? .01f : Density > 100 ? 100 : Density;
                            RecordBrush();
                        }
                        if (!Event.current.control && !Event.current.alt && !Event.current.shift)
                        {
                            if (Event.current.type == EventType.MouseDown)
                                brushRoute.Clear();
                            Event.current.Use();
                            return HandleType.Add;
                        }
                    }
                }
                if (Event.current.button == 1)
                {
                    if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                    {
                        if (!Event.current.control && !Event.current.alt && !Event.current.shift)
                        {
                            Event.current.Use();
                            return HandleType.Sub;
                        }
                    }
                }
            }
            if (Event.current.isKey)
            {
                if (Event.current.keyCode == KeyCode.V)
                {
                    if (Event.current.type == EventType.KeyDown)
                    {
                        Preview = !Preview;
                        RefreshInstancingView();
                    }
                }
            }
            return HandleType.None;
        }

        void Add(RaycastHit hit)
        {
            int count = Mathf.CeilToInt(Density * BrushRange);
            List<InstancingColor> instancing = new List<InstancingColor>();
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(-Mathf.PI, Mathf.PI);
                float distance = Random.Range(0, BrushRange);
                float randomRotateSelf = Random.Range(-RandomSelfRotate, RandomSelfRotate);
                float scale = Random.Range(RandomScaleMin, RandomScaleMax);
                Vector3 pos = new Vector3(Mathf.Sin(angle) * distance, 0, Mathf.Cos(angle) * distance) + hit.point;
                if (CheckBurshRoute(pos) && Physics.Raycast(pos + hit.normal * HitHeight, -hit.normal, out RaycastHit _hit, HitHeight * 2, BrushLayer))
                {
                    int j = 1 << _hit.collider.gameObject.layer;
                    if ((MaskLayer & j) == j)
                        continue;
                    Vector3 positionWS = _hit.point - hit.normal * Bias;
                    switch (brushType)
                    {
                        case BrushType.Instancing:
                            Vector3 axis;
                            Vector4 rotateSelf;
                            switch (MeshOriginAxis)
                            {
                                case Axis.Forward:
                                    axis = Vector3.forward;
                                    rotateSelf = new Vector4(0, 0, randomRotateSelf, 0);
                                    break;
                                case Axis.Back:
                                    axis = Vector3.back;
                                    rotateSelf = new Vector4(0, 0, randomRotateSelf, 0);
                                    break;
                                case Axis.Up:
                                    axis = Vector3.up;
                                    rotateSelf = new Vector4(0, randomRotateSelf, 0, 0);
                                    break;
                                case Axis.Down:
                                    axis = Vector3.down;
                                    rotateSelf = new Vector4(0, randomRotateSelf, 0, 0);
                                    break;
                                case Axis.Right:
                                    axis = Vector3.right;
                                    rotateSelf = new Vector4(randomRotateSelf, 0, 0, 0);
                                    break;
                                case Axis.Left:
                                    axis = Vector3.left;
                                    rotateSelf = new Vector4(randomRotateSelf, 0, 0, 0);
                                    break;
                                default:
                                    axis = Vector3.up;
                                    rotateSelf = new Vector4(0, randomRotateSelf, 0, 0);
                                    break;
                            }
                            Vector3 forward = Vector3.Lerp(Vector3.up, _hit.normal, ForwardFaceNormal).normalized;
                            Quaternion rotate = Quaternion.FromToRotation(axis, forward);
                            Vector4 rotateScale = new Vector4(rotate.eulerAngles.x * Mathf.Deg2Rad,
                                rotate.eulerAngles.y * Mathf.Deg2Rad,
                                rotate.eulerAngles.z * Mathf.Deg2Rad, scale);
                            Vector4 position = positionWS;
                            position.w = Random.Range(RandomValueRange.x, RandomValueRange.y);
                            instancing.Add(new InstancingColor(position, rotateScale + rotateSelf));
                            break;
                        case BrushType.GameObject:
                            if (!sceneCache.TryGetParent(prefab, out Transform parent) || parent == null)
                            {
                                GameObject parentObj = new GameObject($"Instances - {prefab.name}");
                                sceneCache.AddParent(prefab, parentObj.transform);
                                parent = parentObj.transform;
                                Undo.RegisterCreatedObjectUndo(parentObj, "Create instance parent");
                            }
                            GameObject _obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                            _obj.name = prefab.name;
                            _obj.transform.position = positionWS;
                            _obj.transform.localScale = Vector3.one * scale;
                            Vector3 eulerAngle = _obj.transform.localEulerAngles;
                            eulerAngle.y += randomRotateSelf * Mathf.Rad2Deg;
                            _obj.transform.localEulerAngles = eulerAngle;
                            Undo.RegisterCreatedObjectUndo(_obj, "Create instance");
                            Undo.RecordObject(this, "Create instance");
                            sceneCache.instances.Add(_obj);
                            break;
                    }
                }
            }
            if (Data != null)
            {
                if (InjectBackgroundColor)
                    Data.InstancingList.AddRange(InjectBackground(instancing));
                else
                    Data.InstancingList.AddRange(instancing);
                EditorUtility.SetDirty(Data);
            }
            RefreshInstancingView();
        }

        bool CheckBurshRoute(Vector3 pos)
        {
            for (int i = 0; i < brushRoute.Count; i++)
            {
                Vector3 routePos = brushRoute[i];
                if (Vector3.Distance(routePos, pos) <= BrushRange)
                    return false;
            }
            return true;
        }

        void Sub(RaycastHit hit)
        {
            switch (brushType)
            {
                case BrushType.Instancing:
                    if (Data.InstancingList.Count == 0)
                        return;
                    int threadCount = Mathf.FloorToInt(Data.InstancingList.Count / (float)_KernelGroupCount);
                    int calcCount = threadCount * _KernelGroupCount;
                    InstancingColor[] instancing = null;
                    if (threadCount > 0)
                    {
                        ComputeBuffer _instancing = new ComputeBuffer(calcCount, sizeof(float) * 11);
                        ComputeBuffer rw_instancing = new ComputeBuffer(calcCount, sizeof(float) * 11, ComputeBufferType.Append);
                        rw_instancing.SetCounterValue(0);
                        _instancing.SetData(Data.InstancingList, 0, 0, calcCount);
                        calcShader.SetBuffer(kernel_Sub, _InstancingBuffer, _instancing);
                        calcShader.SetBuffer(kernel_Sub, Append_InstancingBuffer, rw_instancing);
                        calcShader.SetVector(_Point, hit.point);
                        calcShader.SetVector(_Normal, hit.normal);
                        calcShader.SetFloat(_BrushRange, BrushRange);
                        calcShader.SetFloat(_HitHeight, HitHeight);
                        calcShader.Dispatch(kernel_Sub, threadCount, 1, 1);

                        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
                        ComputeBuffer.CopyCount(rw_instancing, countBuffer, 0);
                        int[] counter = new int[1];
                        countBuffer.GetData(counter);
                        instancing = new InstancingColor[counter[0]];
                        rw_instancing.GetData(instancing);
                        countBuffer.Dispose();
                        _instancing.Dispose();
                        rw_instancing.Dispose();
                    }
                    List<InstancingColor> instancingList = new List<InstancingColor>();
                    for (int i = calcCount; i < Data.InstancingList.Count; i++)
                    {
                        InstancingColor _instancing = Data.InstancingList[i];
                        Vector3 positionWS = _instancing.positionWS;
                        Vector3 dir = positionWS - hit.point;
                        Vector3 proj = dir - hit.normal * Vector3.Dot(dir, hit.normal);
                        Vector3 projDir = proj.normalized;
                        float height = (dir - projDir * Vector3.Dot(dir, projDir)).magnitude;
                        if (proj.magnitude > BrushRange || height > HitHeight)
                            instancingList.Add(_instancing);
                    }
                    if (instancing != null)
                        instancingList.AddRange(instancing);
                    Data.InstancingList = instancingList;
                    EditorUtility.SetDirty(Data);
                    RefreshInstancingView();
                    break;
                case BrushType.GameObject:
                    List<GameObject> newInstances = new List<GameObject>();
                    for (int i = 0; i < sceneCache.instances.Count; i++)
                    {
                        GameObject _obj = sceneCache.instances[i];
                        if (_obj == null)
                            continue;
                        if (!_obj.activeSelf)
                        {
                            newInstances.Add(_obj);
                            continue;
                        }
                        Vector3 dir = _obj.transform.position - hit.point;
                        Vector3 proj = dir - hit.normal * Vector3.Dot(dir, hit.normal);
                        Vector3 projDir = proj.normalized;
                        float height = (dir - projDir * Vector3.Dot(dir, projDir)).magnitude;
                        if (proj.magnitude > BrushRange || height > HitHeight || !_obj.name.StartsWith(prefab.name))
                            newInstances.Add(_obj);
                        else
                            Undo.DestroyObjectImmediate(_obj);
                    }
                    Undo.RecordObject(this, "Sub Instances");
                    sceneCache.instances = newInstances;
                    break;
            }
        }

        InstancingColor[] InjectBackground(List<InstancingColor> instancings)
        {
            calcShader.SetTextureFromGlobal(kernel_SampleBackground, _CameraOpaqueTexture, _CameraOpaqueTexture);
            ComputeBuffer rw_instancing = new ComputeBuffer(instancings.Count, sizeof(float) * 11);
            rw_instancing.SetData(instancings);
            calcShader.SetBuffer(kernel_SampleBackground, RW_InstancingBuffer, rw_instancing);
            Camera cam = SceneView.lastActiveSceneView.camera;
            Matrix4x4 P = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
            calcShader.SetMatrix(_VP, P * cam.worldToCameraMatrix);
            calcShader.Dispatch(kernel_SampleBackground, Mathf.CeilToInt(instancings.Count / (float)_KernelGroupCount), 1, 1);
            InstancingColor[] _instancings = new InstancingColor[instancings.Count];
            rw_instancing.GetData(_instancings);
            rw_instancing.Dispose();
            return _instancings;
        }

        void ClearData()
        {
            ClearInstances();
            ClearBuffer();
            if (Data != null)
            {
                Data.InstancingList.Clear();
                EditorUtility.SetDirty(Data);
            }
        }

        void RefreshInstancingView()
        {
            if (brushType == BrushType.GameObject)
                return;
            if (Data == null || ViewMesh == null || ViewMat == null)
                return;
            ClearBuffer();
            for (int i = 0; i < InstancingController.List.Count; i++)
            {
                InstancingController controller = InstancingController.List[i];
                if (controller.Data == Data)
                    controller.RefreshInstancingView();
            }
            if (!Preview || Data.InstancingList.Count == 0)
                return;
            if (InjectBackgroundColor)
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
            ViewMat.SetBuffer(_InstancingBuffer, instancingBuffer);

            argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = ViewMesh.GetIndexCount(0);
            args[1] = (uint)Data.InstancingList.Count;
            args[2] = ViewMesh.GetIndexStart(0);
            args[3] = ViewMesh.GetBaseVertex(0);
            argsBuffer.SetData(args);
        }

        void RefreshInstances()
        {
            for (int i = 0; i < sceneCache.instances.Count; i++)
            {
                GameObject obj = sceneCache.instances[i];
                if (obj == null)
                {
                    Undo.RecordObject(this, "Refresh instances");
                    sceneCache.instances.RemoveAt(i);
                    i--;
                }
            }
        }

        void ClearBuffer()
        {
            if (instancingBuffer != null)
                instancingBuffer.Release();
            instancingBuffer = null;
            if (argsBuffer != null)
                argsBuffer.Release();
            argsBuffer = null;
        }

        void ClearInstances()
        {
            for (int i = 0; i < sceneCache.instances.Count; i++)
            {
                GameObject obj = sceneCache.instances[i];
                if (obj != null)
                    Undo.DestroyObjectImmediate(obj);
            }
            Undo.RecordObject(this, "Clear Instances");
            sceneCache.instances.Clear();
        }

        string[] GetLayers()
        {
            List<string> layerNames = new List<string>(InternalEditorUtility.layers);
            layerNames.Insert(3, "");
            return layerNames.ToArray();
        }

        public void RecordBrush()
        {
            switch (brushType)
            {
                case BrushType.Instancing:
                    if (instanceDataBrushCache != null)
                        instanceDataBrushCache.RecordProp(this);
                    break;
                case BrushType.GameObject:
                    if (prefabBrushCache != null)
                        prefabBrushCache.RecordProp(this);
                    break;
            }
        }

        public void ApplyCache(BrushCache cache)
        {
            if (cache == null)
                return;
            BrushLayer = cache.BrushLayer;
            MaskLayer = cache.MaskLayer;
            BrushRange = cache.BrushRange;
            Density = cache.Density;
            HitHeight = cache.HitHeight;
            Bias = cache.Bias;
            MeshOriginAxis = cache.MeshOriginAxis;
            ForwardFaceNormal = cache.ForwardFaceNormal;
            RandomSelfRotate = cache.RandomSelfRotate;
            RandomScaleMin = cache.RandomScaleMin;
            RandomScaleMax = cache.RandomScaleMax;
            RandomValueRange = cache.RandomValueRange;
            EditorUtility.SetDirty(this);
        }

        public enum HandleType { None, Add, Sub }
        public enum Axis { Forward, Back, Up, Down, Right, Left }

        public enum BrushType { Instancing, GameObject }
    }
}