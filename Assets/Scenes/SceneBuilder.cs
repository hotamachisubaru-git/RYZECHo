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
    /// ゲームシーンをコードだけで完全に構築するビルダー。
    /// Awake で自動的に全コンポーネントを生成・初期化する。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SceneBuilder : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("GameSceneConfig.jsonのパス")]
        [SerializeField] private string configPath = "Assets/Scenes/GameSceneConfig.json";

        [Header("Performance")]
        [SerializeField] private int targetFps = 60;
        [SerializeField] private float fixedDeltaTime = 0.016666f;

        private GameSceneSetup.GameSceneConfig _config;
        private GameObject _mainCamera;
        private GameObject _hudCamera;
        private GameObject _canvas;
        private AudioMixer _audioMixer;
        private UIScreenManager _screenManager;
        private GameHUDController _hudController;
        private RyzechoGameController _gameController;
        private RYZECHo.Unity.HuntFovRenderer _huntFovRenderer;
        private Transform _playerView;

        private void Awake()
        {
            Build();
        }

        public void Build()
        {
            LoadConfig();
            ApplyPerformanceSettings();
            SetupCameras();
            SetupCanvas();
            SetupAudioMixer();
            SetupScreenManager();
            SetupHUDController();
            SetupGameController();
            SetupUniversalRenderPipeline();

            // ゲーム初期化（タイトル画面から始まる）
            _gameController.InitializeGame();

            Debug.Log("[SceneBuilder] Scene build complete.");
        }

        private void LoadConfig()
        {
            var fullPath = Path.Combine(Application.dataPath, "..", configPath);
            if (File.Exists(fullPath))
            {
                var json = File.ReadAllText(fullPath);
                _config = JsonUtility.FromJson<GameSceneSetup.GameSceneConfig>(json);
            }
            _config ??= new GameSceneSetup.GameSceneConfig();
        }

        private void ApplyPerformanceSettings()
        {
            Application.targetFrameRate = targetFps;
            Time.fixedDeltaTime = fixedDeltaTime;
            QualitySettings.vSyncCount = 0;
        }

        private void SetupCameras()
        {
            var cameraConfig = _config?.camera;
            var mainConfig = cameraConfig?.main;
            var hudConfig = cameraConfig?.hud;
            var viewport = cameraConfig?.viewport;

            // メインカメラ
            _mainCamera = new GameObject("Main Camera");
            _mainCamera.tag = "MainCamera";
            var mainCam = _mainCamera.AddComponent<Camera>();
            mainCam.clearFlags = mainConfig?.clearFlags ?? CameraClearFlags.Skybox;
            mainCam.backgroundColor = mainConfig?.backgroundColor ?? new Color(0.08f, 0.08f, 0.12f, 1f);
            mainCam.cullingMask = -1;
            mainCam.depth = mainConfig?.depth ?? -1;
            mainCam.allowHDR = false;
            mainCam.allowMSAA = false;
            if (viewport != null)
                mainCam.rect = new Rect(viewport.x, viewport.y, viewport.width, viewport.height);

            // PlayerView（プレイヤー表現用）
            _playerView = CreatePlayerView();

            // HuntFovRenderer
            _huntFovRenderer = _mainCamera.AddComponent<RYZECHo.Unity.HuntFovRenderer>();
            if (_playerView != null)
                _huntFovRenderer.SetTarget(_playerView);

            // HUDカメラ
            _hudCamera = new GameObject("HUD Camera");
            var hudCam = _hudCamera.AddComponent<Camera>();
            hudCam.clearFlags = hudConfig?.clearFlags ?? CameraClearFlags.Depth;
            hudCam.backgroundColor = hudConfig?.backgroundColor ?? new Color(0, 0, 0, 0);
            hudCam.depth = hudConfig?.depth ?? 10;
            hudCam.orthographic = true;
            hudCam.orthographicSize = 5f;
            hudCam.nearClipPlane = 0.1f;
            hudCam.farClipPlane = 100f;
            hudCam.allowHDR = false;
            hudCam.allowMSAA = false;
            hudCam.enabled = false;
            if (viewport != null)
                hudCam.rect = new Rect(viewport.x, viewport.y, viewport.width, viewport.height);

            // カメラ位置
            _mainCamera.transform.position = new Vector3(0, 0, -10);
            _hudCamera.transform.position = new Vector3(0, 0, -10);
        }

        private Transform CreatePlayerView()
        {
            var playerObject = new GameObject("Player View");
            playerObject.transform.position = new Vector3(0, 0, 1f);

            var spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateDiscSprite();
            spriteRenderer.color = new Color(0.48f, 0.9f, 1f, 1f);
            spriteRenderer.sortingOrder = 50;

            return playerObject.transform;
        }

        private Sprite CreateDiscSprite()
        {
            const int textureSize = 64;
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = "Generated Player Disc",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var center = (textureSize - 1) * 0.5f;
            var radius = textureSize * 0.42f;
            var pixels = new Color32[textureSize * textureSize];

            for (var y = 0; y < textureSize; y++)
            {
                for (var x = 0; x < textureSize; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var alpha = Mathf.Clamp01(radius + 1.5f - distance);
                    pixels[(y * textureSize) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
        }

        private void SetupCanvas()
        {
            var uiConfig = _config?.ui;
            var canvasConfig = uiConfig?.canvas;
            var layoutConfig = uiConfig?.layout;

            _canvas = new GameObject("GameCanvas");
            var canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = canvasConfig?.pixelPerfect ?? false;
            canvas.sortingOrder = canvasConfig?.defaultSortOrder ?? 100;

            var scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var refRes = new Vector2(
                layoutConfig?.clientWidth ?? GameLayout.DefaultClientWidth,
                layoutConfig?.clientHeight ?? GameLayout.DefaultClientHeight);
            scaler.referenceResolution = refRes;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _canvas.AddComponent<GraphicRaycaster>();
        }

        private void SetupAudioMixer()
        {
            var audioGO = new GameObject("AudioMixer");
            const string mixerPath = "Assets/Settings/GameMixer.mixer";

#if UNITY_EDITOR
            // 修正: AudioMixerはComponentではなくアセットなので、AddComponent<T>()ではなく読み込みで取得する。
            _audioMixer = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
#endif

            var audioMixerComponent = audioGO.AddComponent<AudioMixerComponent>();
            audioMixerComponent.mixer = _audioMixer;

            var audioConfig = _config?.game?.audio;
            if (audioConfig != null && _audioMixer != null)
            {
                _audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(audioConfig.masterVolume, 0.0001f)) * 20f);
                _audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(audioConfig.sfxVolume, 0.0001f)) * 20f);
                _audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(audioConfig.musicVolume, 0.0001f)) * 20f);
            }
            else if (audioConfig != null)
            {
                // 修正: mixerアセット未作成でも、生成シーン自体は有効なまま進める。
                Debug.LogWarning($"[SceneBuilder] AudioMixer asset not found at {mixerPath}. Volume settings were skipped.");
            }
        }

        private void SetupScreenManager()
        {
            var uiConfig = _config?.ui;
            var screensConfig = uiConfig?.screens;

            // ScreenManager生成
            var screenManagerGO = new GameObject("UIScreenManager");
            screenManagerGO.transform.SetParent(_canvas.transform, false);
            _screenManager = screenManagerGO.AddComponent<UIScreenManager>();

            // JSON Prefabから画面を動的に生成
            LoadScreensFromJson(screensConfig);

            // 初期画面をタイトルスクリーンに
            if (screensConfig?.titleScreen?.autoShow == true)
            {
                _screenManager.ShowScreen(UIScreenManager.ScreenType.TitleScreen);
            }
        }

        private void LoadScreensFromJson(GameSceneSetup.ScreensConfig screensConfig)
        {
            string[] screenNames = { "TitleScreen", "MainMenu", "DifficultySelect", "SetupScreen", "GameOverScreen" };
            UIScreenManager.ScreenType[] screenTypes =
            {
                UIScreenManager.ScreenType.TitleScreen,
                UIScreenManager.ScreenType.MainMenu,
                UIScreenManager.ScreenType.DifficultySelect,
                UIScreenManager.ScreenType.SetupScreen,
                UIScreenManager.ScreenType.GameOver
            };

            List<GameObject> prefabList = new();

            for (int i = 0; i < screenNames.Length; i++)
            {
                var config = GetScreenConfig(screensConfig, screenNames[i]);
                if (config == null || !config.enabled)
                {
                    Debug.Log($"[SceneBuilder] Screen '{screenNames[i]}' is disabled.");
                    continue;
                }

                var screenGO = CreateScreenFromJson(screenNames[i], screenTypes[i]);
                if (screenGO != null)
                {
                    screenGO.transform.SetParent(_canvas.transform, false);
                    screenGO.transform.localPosition = Vector3.zero;
                    screenGO.transform.localScale = Vector3.one;
                    screenGO.SetActive(config.autoShow);
                    prefabList.Add(screenGO);
                }
            }

            _screenManager.screenPrefabs = prefabList.ToArray();
        }

        private GameSceneSetup.ScreenConfigData GetScreenConfig(GameSceneSetup.ScreensConfig screensConfig, string name)
        {
            return name switch
            {
                "TitleScreen" => screensConfig?.titleScreen,
                "MainMenu" => screensConfig?.mainMenu,
                "DifficultySelect" => screensConfig?.difficultySelect,
                "SetupScreen" => screensConfig?.setupScreen,
                "GameOverScreen" => screensConfig?.gameOver,
                _ => null
            };
        }

        private GameObject CreateScreenFromJson(string prefabName, UIScreenManager.ScreenType screenType)
        {
            string jsonPath = Path.Combine(Application.dataPath, "..", "Assets/UI/Prefabs", prefabName + ".prefab.json");
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"[SceneBuilder] Prefab JSON not found: {jsonPath}");
                return CreateFallbackScreen(prefabName);
            }

            var json = File.ReadAllText(jsonPath);
            var prefabData = JsonUtility.FromJson<ScreenPrefabData>(json);

            var go = new GameObject(prefabName);
            go.AddComponent<UIScreen>();

            if (prefabData.prefab?.gameObject != null)
            {
                CreateGameObjectFromPrefabData(prefabData.prefab.gameObject, go.transform);
            }

            return go;
        }

        private GameObject CreateGameObjectFromPrefabData(PrefabGameObjectData data, Transform parent)
        {
            var go = new GameObject(data.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            if (data.isActive == false)
                go.SetActive(false);

            if (data.components != null)
            {
                foreach (var comp in data.components)
                {
                    if (comp.type == "RectTransform" && comp.properties != null)
                    {
                        var rt = go.AddComponent<RectTransform>();
                        if (comp.properties.TryGetValue("anchoredPosition", out var pos))
                            rt.anchoredPosition = pos.Count >= 2 ? new Vector2((float)pos[0], (float)pos[1]) : Vector2.zero;
                        if (comp.properties.TryGetValue("sizeDelta", out var size))
                            rt.sizeDelta = size.Count >= 2 ? new Vector2((float)size[0], (float)size[1]) : Vector2.zero;
                        if (comp.properties.TryGetValue("anchorMin", out var min))
                            rt.anchorMin = min.Count >= 2 ? new Vector2((float)min[0], (float)min[1]) : Vector2.zero;
                        if (comp.properties.TryGetValue("anchorMax", out var max))
                            rt.anchorMax = max.Count >= 2 ? new Vector2((float)max[0], (float)max[1]) : Vector2.one;
                        if (comp.properties.TryGetValue("pivot", out var pivot))
                            rt.pivot = pivot.Count >= 2 ? new Vector2((float)pivot[0], (float)pivot[1]) : new Vector2(0.5f, 0.5f);
                    }
                    else if (comp.type == "Image" && comp.properties != null)
                    {
                        var img = go.AddComponent<Image>();
                        if (comp.properties.TryGetValue("color", out var color))
                            img.color = color.Count >= 4 ? new Color((float)color[0], (float)color[1], (float)color[2], (float)color[3]) : Color.white;
                        if (comp.properties.TryGetValue("fillMethod", out var fillMethod))
                            img.fillMethod = (Image.FillMethod)(int)fillMethod[0];
                        if (comp.properties.TryGetValue("raycastTarget", out var raycast))
                            img.raycastTarget = raycast[0] != null && (bool)raycast[0];
                    }
                    else if (comp.type == "TextMeshProUGUI" && comp.properties != null)
                    {
                        var textComp = go.AddComponent<TextMeshProUGUI>();
                        if (comp.properties.TryGetValue("text", out var textVal))
                            textComp.text = textVal[0]?.ToString() ?? "";
                        if (comp.properties.TryGetValue("fontSize", out var fontSize))
                            textComp.fontSize = (float)fontSize[0];
                        if (comp.properties.TryGetValue("color", out var color))
                            textComp.color = color.Count >= 4 ? new Color((float)color[0], (float)color[1], (float)color[2], (float)color[3]) : Color.white;
                        if (comp.properties.TryGetValue("fontWeight", out var fw))
                            textComp.fontWeight = (FontWeight)fw[0];
                    }
                    else if (comp.type == "Button" && comp.properties != null)
                    {
                        go.AddComponent<Button>();
                    }
                    else if (comp.type == "Text" && comp.properties != null)
                    {
                        var textComp = go.AddComponent<Text>();
                        if (comp.properties.TryGetValue("text", out var textVal))
                            textComp.text = textVal[0]?.ToString() ?? "";
                        if (comp.properties.TryGetValue("fontSize", out var fontSize))
                            textComp.fontSize = (int)fontSize[0];
                        if (comp.properties.TryGetValue("color", out var color))
                            textComp.color = color.Count >= 4 ? new Color((float)color[0], (float)color[1], (float)color[2], (float)color[3]) : Color.white;
                    }
                }
            }

            if (data.children != null)
            {
                foreach (var childData in data.children)
                {
                    CreateGameObjectFromPrefabData(childData, go.transform);
                }
            }

            return go;
        }

        private GameObject CreateFallbackScreen(string name)
        {
            var go = new GameObject(name);
            var rootGO = new GameObject("ScreenRoot");
            rootGO.transform.SetParent(go.transform, false);
            var rt = rootGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rootGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 1f);

            var textGO = new GameObject("TitleText");
            textGO.transform.SetParent(rootGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, 100);
            textRect.sizeDelta = new Vector2(400, 60);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = name;
            text.fontSize = 36;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.8f, 0.8f, 1f, 1f);

            return go;
        }

        private void SetupHUDController()
        {
            var hudConfig = _config?.ui?.screens?.gameHUD;
            if (hudConfig == null || !hudConfig.enabled)
                return;

            var hudGO = new GameObject("GameHUD");
            hudGO.transform.SetParent(_canvas.transform, false);
            _hudController = hudGO.AddComponent<GameHUDController>();
        }

        private void SetupGameController()
        {
            // ScriptableObjectを生成
            var gameRulesSO = CreateScriptableObject<GameRulesSettingsSO>("GameRulesSettings");
            var layoutSO = CreateScriptableObject<LayoutSettingsSO>("LayoutSettings");
            var gameplaySO = CreateScriptableObject<GameplaySettingsSO>("GameplaySettings");
            var visualSO = CreateScriptableObject<VisualSettingsSO>("VisualSettings");
            var audioSO = CreateScriptableObject<AudioSettingsSO>("AudioSettings");

            // GameController生成
            var controllerGO = new GameObject("RyzechoGameController");
            _gameController = controllerGO.AddComponent<RyzechoGameController>();

            // 設定を適用
            var gameConfig = _config?.game;
            if (gameConfig != null)
            {
                _gameController.InitialRoundsToWin = gameConfig.initial?.roundsToWin ?? GameRules.RoundsToWin;
                _gameController.InitialStartingCredits = gameConfig.initial?.startingCredits ?? GameRules.StartingCredits;
                _gameController.InitialBuildPoints = gameConfig.initial?.initialBuildPoints ?? GameRules.InitialBuildPoints;
                _gameController.TeamSize = gameConfig.initial?.teamSize ?? GameRules.TeamSize;
                _gameController.TargetFps = targetFps;
                _gameController.FixedDeltaTime = fixedDeltaTime;
            }

            // 既存のCamera参照をセット
            _gameController.gameCamera = _mainCamera.GetComponent<Camera>();
            _gameController.playerView = _playerView;
            _gameController.huntFovRenderer = _huntFovRenderer;

            // ScreenManagerとHUDControllerをセット
            _gameController.screenManager = _screenManager;
            _gameController.hudController = _hudController;

            // ScriptableObjectを設定
            _gameController.GameplaySettings = gameplaySO;
            _gameController.GameRulesSettings = gameRulesSO;
            _gameController.LayoutSettings = layoutSO;
            _gameController.VisualSettings = visualSO;
            _gameController.AudioSettings = audioSO;

            // カメラ設定
            _gameController.followPlayerCamera = true;
            _gameController.cameraOffset = new Vector3(0f, 0f, -10f);
            _gameController.cameraLerp = 18f;
            _gameController.orthographicSize = 6.5f;

            // World mapping
            _gameController.worldOrigin = new Vector2(-9f, -6f);
            _gameController.cellSizeUnits = 1f;
        }

        private T CreateScriptableObject<T>(string name) where T : ScriptableObject
        {
            var path = $"Assets/Settings/{name}.asset";
            var so = ScriptableObject.CreateInstance<T>();
            so.name = name;
            // ESC-0114: AssetDatabase is UnityEditor, not UnityEngine
            UnityEditor.AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private void SetupUniversalRenderPipeline()
        {
            var rpAsset = Resources.Load<RenderPipelineAsset>("PC_RPAsset");
            if (rpAsset != null)
            {
                GraphicsSettings.defaultRenderPipeline = rpAsset;
            }
        }

        // ==================== JSON Prefab Data Models ====================

        [Serializable]
        private class ScreenPrefabData
        {
            public PrefabData prefab;
        }

        [Serializable]
        private class PrefabData
        {
            public string name;
            public string screenType;
            public PrefabGameObjectData gameObject;
        }

        [Serializable]
        private class PrefabGameObjectData
        {
            public string name;
            public string type;
            public List<PrefabComponentData> components;
            public List<PrefabGameObjectData> children;
            public bool isActive = true;
        }

        [Serializable]
        private class PrefabComponentData
        {
            public string type;
            public Dictionary<string, List<object>> properties;
        }
    }
}
