using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using RYZECHo.UI;
using RYZECHo.Unity;

namespace RYZECHo
{
    /// <summary>
    /// 起動時にゲームシーンを完全に自動構築する。
    /// Awake で Camera → Canvas → AudioMixer → UIScreenManager → 各画面 → GameHUD → GameController を生成。
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GameSceneBootstrap : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string configFileName = "GameSceneConfig";
        [SerializeField] private string prefabJsonDir = "UI/Prefabs";
        [SerializeField] private int targetFps = 60;
        [SerializeField] private float fixedDeltaTime = 0.016666f;

        private GameObject _mainCamera;
        private GameObject _hudCamera;
        private GameObject _canvas;
        private AudioMixer _audioMixer;
        private UIScreenManager _screenManager;
        private GameHUDController _hudController;
        private RyzechoGameController _gameController;
        private Transform _playerView;
        private HuntFovRenderer _huntFovRenderer;

        private void Awake()
        {
            BuildScene();
        }

        private void BuildScene()
        {
            var config = LoadConfig();
            ApplyPerformanceSettings();
            _mainCamera = SetupMainCamera(config);
            _playerView = CreatePlayerView(_mainCamera);
            _huntFovRenderer = SetupHuntFov(_mainCamera);
            _canvas = SetupCanvas(config);
            _audioMixer = SetupAudioMixer(config);
            _screenManager = SetupScreenManager(config);
            _hudController = SetupHUDController(config);
            _gameController = SetupGameController(config);
            SetupURP(config);

            // ゲーム初期化（タイトル画面からスタート）
            _gameController.InitializeGame();

            Debug.Log("[Bootstrap] Scene build complete. Play to start!");
        }

        private GameSceneConfigData LoadConfig()
        {
            var path = Path.Combine(Application.dataPath, "..", "Assets/Scenes", configFileName + ".json");
            if (File.Exists(path))
            {
                return JsonUtility.FromJson<GameSceneConfigData>(File.ReadAllText(path));
            }
            Debug.LogWarning("[Bootstrap] Config not found, using defaults.");
            return new GameSceneConfigData();
        }

        private void ApplyPerformanceSettings()
        {
            Application.targetFrameRate = targetFps;
            Time.fixedDeltaTime = fixedDeltaTime;
            QualitySettings.vSyncCount = 0;
        }

        private GameObject SetupMainCamera(GameSceneConfigData config)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            var cam = go.AddComponent<Camera>();

            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            cam.cullingMask = -1;
            cam.depth = -1;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;

            // URP
            var urpData = go.AddComponent<UniversalAdditionalCameraData>();
            urpData.requiresColorOption = CameraOverrideOption.Off;
            urpData.requiresDepthOption = CameraOverrideOption.Off;
            urpData.renderType = CameraRenderType.Base;

            return go;
        }

        private Transform CreatePlayerView(GameObject parent)
        {
            var go = new GameObject("Player View");
            go.transform.SetParent(parent.transform, false);
            go.transform.position = new Vector3(0, 0, 1f);

            // ディスクスプライト生成
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "PlayerDisc";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var center = (size - 1) * 0.5f;
            var radius = size * 0.42f;
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(radius + 1.5f - dist);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.48f, 0.9f, 1f, 1f);
            sr.sortingOrder = 50;

            return go.transform;
        }

        private HuntFovRenderer SetupHuntFov(GameObject camera)
        {
            var renderer = camera.AddComponent<HuntFovRenderer>();
            if (_playerView != null)
                renderer.SetTarget(_playerView);
            return renderer;
        }

        private GameObject SetupCanvas(GameSceneConfigData config)
        {
            var go = new GameObject("GameCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 960);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            return go;
        }

        private AudioMixer SetupAudioMixer(GameSceneConfigData config)
        {
            const string mixerPath = "Assets/Settings/GameMixer.mixer";
            AudioMixer mixer = null;

#if UNITY_EDITOR
            // Compile fix: AudioMixer is an asset type, not a Component, so load it instead of AddComponent<T>().
            mixer = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
#endif

            var go = new GameObject("AudioMixer");
            var comp = go.AddComponent<AudioMixerComponent>();
            comp.mixer = mixer;

            if (mixer == null)
            {
                // Compile fix: missing mixer assets should not block bootstrap compilation or scene construction.
                Debug.LogWarning($"[Bootstrap] AudioMixer asset not found at {mixerPath}. AudioMixerComponent will use defaults.");
            }

            return mixer;
        }

        private UIScreenManager SetupScreenManager(GameSceneConfigData config)
        {
            var go = new GameObject("UIScreenManager");
            go.transform.SetParent(_canvas.transform, false);
            var manager = go.AddComponent<UIScreenManager>();

            // JSONから画面を生成
            var screens = LoadScreensFromJson(config.ui.screens);
            manager.screenPrefabs = screens.ToArray();

            // タイトル画面を表示
            if (config.ui.screens.titleScreen.enabled)
            {
                manager.ShowScreen(UIScreenManager.ScreenType.TitleScreen);
            }

            return manager;
        }

        private List<GameObject> LoadScreensFromJson(ScreensConfigData screensConfig)
        {
            var list = new List<GameObject>();
            var jsonDir = Path.Combine(Application.dataPath, "..", "Assets", prefabJsonDir);

            if (!Directory.Exists(jsonDir))
            {
                Debug.LogWarning($"[Bootstrap] Prefab JSON dir not found: {jsonDir}");
                return list;
            }

            foreach (var file in Directory.GetFiles(jsonDir, "*.prefab.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var screenName = name.Replace("UI", "").Replace("Screen", "").Replace("ScreenUI", "");

                if (!IsEnabled(screensConfig, screenName))
                {
                    Debug.Log($"[Bootstrap] '{screenName}' disabled in config, skipping.");
                    continue;
                }

                var json = File.ReadAllText(file);
                var screenGO = ParseScreen(json);
                if (screenGO != null)
                {
                    screenGO.transform.SetParent(_canvas.transform, false);
                    screenGO.transform.localPosition = Vector3.zero;
                    screenGO.transform.localScale = Vector3.one;
                    list.Add(screenGO);
                }
            }

            return list;
        }

        private bool IsEnabled(ScreensConfigData cfg, string name)
        {
            return name switch
            {
                "Title" => cfg.titleScreen?.enabled == true,
                "Main" => cfg.mainMenu?.enabled == true,
                "DifficultySelect" => cfg.difficultySelect?.enabled == true,
                "Setup" => cfg.setupScreen?.enabled == true,
                "GameOver" => cfg.gameOver?.enabled == true,
                _ => true
            };
        }

        private GameObject ParseScreen(string json)
        {
            var data = JsonUtility.FromJson<PrefabScreenJson>(json);
            if (data?.prefab == null) return null;

            var go = new GameObject(data.prefab.gameObject.name);
            go.AddComponent<UIScreen>();

            if (data.prefab.children != null)
            {
                foreach (var child in data.prefab.children)
                {
                    BuildChild(child, go.transform);
                }
            }

            return go;
        }

        private void BuildChild(PrefabChildJson child, Transform parent)
        {
            var go = new GameObject(child.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            if (child.components != null)
            {
                foreach (var comp in child.components)
                {
                    switch (comp.type)
                    {
                        case "RectTransform":
                            ApplyRectTransform(go, comp.properties);
                            break;
                        case "Image":
                            ApplyImage(go, comp.properties);
                            break;
                        case "Button":
                            go.AddComponent<Button>();
                            break;
                        case "TextMeshProUGUI":
                            ApplyTextMeshPro(go, comp.properties);
                            break;
                        case "VerticalLayoutGroup":
                            go.AddComponent<VerticalLayoutGroup>();
                            ApplyRectTransform(go, comp.properties);
                            break;
                        case "GridLayoutGroup":
                            go.AddComponent<GridLayoutGroup>();
                            ApplyRectTransform(go, comp.properties);
                            break;
                        case "CanvasScaler":
                            var scaler = go.AddComponent<CanvasScaler>();
                            if (comp.properties?.ContainsKey("uiScaleMode") == true)
                                scaler.uiScaleMode = (CanvasScaler.ScaleMode)(int)comp.properties["uiScaleMode"];
                            if (comp.properties?.ContainsKey("referenceResolution") == true)
                            {
                                var res = comp.properties["referenceResolution"] as float[];
                                if (res != null) scaler.referenceResolution = new Vector2(res[0], res[1]);
                            }
                            if (comp.properties?.ContainsKey("matchWidthOrHeight") == true)
                                scaler.matchWidthOrHeight = (float)comp.properties["matchWidthOrHeight"];
                            break;
                        case "GraphicRaycaster":
                            go.AddComponent<GraphicRaycaster>();
                            break;
                        case "InputField":
                            go.AddComponent<InputField>();
                            break;
                        case "Slider":
                            var slider = go.AddComponent<Slider>();
                            if (comp.properties?.ContainsKey("minValue") == true) slider.minValue = (float)comp.properties["minValue"];
                            if (comp.properties?.ContainsKey("maxValue") == true) slider.maxValue = (float)comp.properties["maxValue"];
                            if (comp.properties?.ContainsKey("value") == true) slider.value = (float)comp.properties["value"];
                            break;
                    }
                }
            }

            if (child.isActive.HasValue)
                go.SetActive(child.isActive.Value);

            if (child.children != null)
            {
                foreach (var grandChild in child.children)
                {
                    BuildChild(grandChild, go.transform);
                }
            }
        }

        private void ApplyRectTransform(GameObject go, Dictionary<string, object> props)
        {
            if (props == null) return;
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();

            if (props.TryGetValue("anchoredPosition", out var pos))
                rt.anchoredPosition = (float[])pos is float[] p ? new Vector2(p[0], p[1]) : Vector2.zero;
            if (props.TryGetValue("sizeDelta", out var size))
                rt.sizeDelta = (float[])size is float[] s ? new Vector2(s[0], s[1]) : Vector2.zero;
            if (props.TryGetValue("anchorMin", out var min))
                rt.anchorMin = (float[])min is float[] m ? new Vector2(m[0], m[1]) : Vector2.zero;
            if (props.TryGetValue("anchorMax", out var max))
                rt.anchorMax = (float[])max is float[] a ? new Vector2(a[0], a[1]) : Vector2.one;
            if (props.TryGetValue("pivot", out var pivot))
                rt.pivot = (float[])pivot is float[] pv ? new Vector2(pv[0], pv[1]) : new Vector2(0.5f, 0.5f);
        }

        private void ApplyImage(GameObject go, Dictionary<string, object> props)
        {
            if (props == null) return;
            var img = go.AddComponent<Image>();
            img.raycastTarget = props.TryGetValue("raycastTarget", out var rt) && (bool)rt;
            if (props.TryGetValue("color", out var c) && c is float[] color)
                img.color = new Color(color[0], color[1], color[2], color.Length > 3 ? color[3] : 1f);
            if (props.TryGetValue("fillMethod", out var fm))
                img.fillMethod = (Image.FillMethod)(int)fm;
        }

        private void ApplyTextMeshPro(GameObject go, Dictionary<string, object> props)
        {
            if (props == null) return;
            var text = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();

            if (props.TryGetValue("text", out var t)) text.text = (string)t;
            if (props.TryGetValue("fontSize", out var fs)) text.fontSize = (int)fs;
            if (props.TryGetValue("color", out var c) && c is float[] color)
                text.color = new Color(color[0], color[1], color[2], color.Length > 3 ? color[3] : 1f);
            if (props.TryGetValue("alignment", out var al)) text.alignment = (TextAlignmentOptions)(int)al;
            if (props.TryGetValue("fontWeight", out var fw)) text.fontWeight = (FontWeight)(int)fw;
        }

        private GameHUDController SetupHUDController(GameSceneConfigData config)
        {
            var hudConfig = config.ui.screens.gameHUD;
            if (!hudConfig?.enabled == true) return null;

            var go = new GameObject("GameHUD");
            go.transform.SetParent(_canvas.transform, false);
            return go.AddComponent<GameHUDController>();
        }

        private RyzechoGameController SetupGameController(GameSceneConfigData config)
        {
            var go = new GameObject("RyzechoGameController");
            var controller = go.AddComponent<RyzechoGameController>();

            var gameCfg = config.game;
            if (gameCfg != null)
            {
                controller.InitialRoundsToWin = gameCfg.initial.roundsToWin;
                controller.InitialStartingCredits = gameCfg.initial.startingCredits;
                controller.InitialBuildPoints = gameCfg.initial.initialBuildPoints;
                controller.TeamSize = gameCfg.initial.teamSize;
                controller.TargetFps = targetFps;
                controller.FixedDeltaTime = fixedDeltaTime;
            }

            controller.gameCamera = _mainCamera.GetComponent<Camera>();
            controller.playerView = _playerView;
            controller.huntFovRenderer = _huntFovRenderer;
            controller.screenManager = _screenManager;
            controller.hudController = _hudController;

            // ESC-0114: [SerializeField] fields are now public properties
            controller.followPlayerCamera = true;
            controller.cameraOffset = new Vector3(0, 0, -10);
            controller.cameraLerp = 18f;
            controller.orthographicSize = 6.5f;
            controller.worldOrigin = new Vector2(-9, -6);
            controller.cellSizeUnits = 1f;

            return controller;
        }

        private void SetupURP(GameSceneConfigData config)
        {
            var urpConfig = config.camera.urp;
            if (urpConfig != null && urpConfig.useUrp)
            {
                var rpAsset = Resources.Load<RenderPipelineAsset>(urpConfig.renderPipelineAsset);
                if (rpAsset != null)
                {
                    GraphicsSettings.defaultRenderPipeline = rpAsset;
                }
            }
        }

        // ==================== JSON Data Models ====================

        [Serializable]
        private class GameSceneConfigData
        {
            public CameraConfigData camera;
            public UIConfigData ui;
            public GameConfigData game;
        }

        [Serializable]
        public class CameraConfigData
        {
            public MainCameraConfigData main;
            public CameraHudData hud;
            public ViewportConfigData viewport;
            public URPConfigData urp;
        }

        [Serializable]
        public class CameraHudData
        {
            public CameraClearFlags clearFlags;
            public Color backgroundColor;
            public int depth;
        }

        [Serializable]
        public class MainCameraConfigData
        {
            public CameraClearFlags clearFlags;
            public Color backgroundColor;
            public int cullingMask;
            public int depth;
        }

        [Serializable]
        public class ViewportConfigData
        {
            public float x, y, width, height;
        }

        [Serializable]
        public class URPConfigData
        {
            public bool useUrp;
            public string renderPipelineAsset;
        }

        [Serializable]
        public class UIConfigData
        {
            public CanvasConfigData canvas;
            public LayoutConfigData layout;
            public ScreensConfigData screens;
        }

        [Serializable]
        public class CanvasConfigData
        {
            public RenderMode renderMode;
            public bool pixelPerfect;
            public int sortingGridSize;
        }

        [Serializable]
        public class LayoutConfigData
        {
            public int clientWidth;
            public int clientHeight;
        }

        [Serializable]
        public class ScreensConfigData
        {
            public ScreenConfigData titleScreen;
            public ScreenConfigData mainMenu;
            public ScreenConfigData difficultySelect;
            public ScreenConfigData setupScreen;
            public ScreenConfigData gameOver;
            public ScreenConfigData gameHUD;
        }

        [Serializable]
        public class ScreenConfigData
        {
            public bool enabled;
            public bool autoShow;
            public string name;
        }

        [Serializable]
        public class GameConfigData
        {
            public InitialConfigData initial;
        }

        [Serializable]
        public class InitialConfigData
        {
            public int roundsToWin;
            public int startingCredits;
            public int initialBuildPoints;
            public int teamSize;
        }

        [Serializable]
        public class PrefabScreenJson
        {
            public PrefabData prefab;
        }

        [Serializable]
        public class PrefabData
        {
            public string name;
            public PrefabGameObjectData gameObject;
            public PrefabChildJson[] children;
        }

        [Serializable]
        public class PrefabGameObjectData
        {
            public string name;
            public PrefabComponentData[] components;
        }

        [Serializable]
        public class PrefabChildJson
        {
            public string name;
            public PrefabComponentData[] components;
            public PrefabChildJson[] children;
            public bool? isActive;
        }

        [Serializable]
        public class PrefabComponentData
        {
            public string type;
            public Dictionary<string, object> properties;
        }
    }
}
