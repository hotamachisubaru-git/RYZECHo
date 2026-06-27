using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using System;
using System.Collections.Generic;

namespace RYZECHo
{
    /// <summary>
    /// ゲームシーンをプログラムmaticに自動セットアップするスクリプト。
    /// Canvas、Camera、AudioMixer、UIScreenManagerを生成・初期化する。
    /// Unityエディタを使わずにゲームシーンを構築するために使用。
    /// </summary>
    public class GameSceneSetup : MonoBehaviour
    {
        [Header("Config File")]
        [Tooltip("ゲームシーン設定ファイル")]
        [SerializeField] private TextAsset gameSceneConfig;

        [Header("Camera Settings")]
        [Tooltip("メインカメラのクリアフラグ")]
        [SerializeField] private CameraClearFlags mainCameraClearFlags = CameraClearFlags.Skybox;

        [Tooltip("メインカメラの背景色")]
        [SerializeField] private Color mainCameraBackgroundColor = new Color(0.08f, 0.08f, 0.12f, 1.0f);

        [Tooltip("HUDカメラのクリアフラグ")]
        [SerializeField] private CameraClearFlags hudCameraClearFlags = CameraClearFlags.DepthOnly;

        [Header("Screen Prefabs")]
        [Tooltip("画面Prefab配列（TitleScreen, MainMenu, DifficultySelect, SetupScreen, GameOver）")]
        [SerializeField] private GameObject[] screenPrefabs;

        [Header("Audio")]
        [Tooltip("AudioMixerの名称")]
        [SerializeField] private string audioMixerName = "GameMixer";

        [Header("Performance")]
        [Tooltip("ターゲットFPS")]
        [SerializeField] private int targetFps = 60;

        [Tooltip("FixedDeltaTime")]
        [SerializeField] private float fixedDeltaTime = 0.016666f;

        // 生成されたオブジェクトの参照
        private GameObject _mainCamera;
        private GameObject _hudCamera;
        private GameObject _canvas;
        private GameObject _audioMixerGO;
        private UIScreenManager _screenManager;
        private GameHUDController _hudController;
        private RyzechoGameController _gameController;
        private HuntFovRenderer _huntFovRenderer;
        private Transform _playerView;

        // 設定データ
        private GameSceneConfig _config;

        public Camera MainCamera => _mainCamera?.GetComponent<Camera>();
        public Camera HudCamera => _hudCamera?.GetComponent<Camera>();
        public Canvas MainCanvas => _canvas?.GetComponent<Canvas>();
        public UIScreenManager ScreenManager => _screenManager;
        public GameHUDController HUDController => _hudController;
        public RyzechoGameController GameController => _gameController;

        /// <summary>
        /// シーンセットアップを実行（非同期）
        /// </summary>
        public void SetupAsync(Action onComplete = null)
        {
            StartCoroutine(SetUpCoroutine(onComplete));
        }

        private System.Collections.IEnumerator SetUpCoroutine(Action onComplete)
        {
            // 設定ファイルの読み込み
            LoadConfig();

            // パフォーマンス設定を適用
            ApplyPerformanceSettings();

            // 1. Camera設定
            yield return SetupCameras();

            // 2. Canvas生成
            yield return SetupCanvas();

            // 3. AudioMixer設定
            yield return SetupAudioMixer();

            // 4. UIScreenManagerと画面の初期化
            yield return SetupScreenManager();

            // 5. GameHUDController生成
            yield return SetupHUDController();

            // 6. RyzechoGameController生成
            yield return SetupGameController();

            onComplete?.Invoke();
        }

        /// <summary>
        /// 設定ファイルをJSONから読み込む
        /// </summary>
        private void LoadConfig()
        {
            if (gameSceneConfig != null)
            {
                _config = JsonUtility.FromJson<GameSceneConfig>(gameSceneConfig.text);
            }

            // 設定ファイルがない場合はデフォルト値を使用
            _config ??= new GameSceneConfig();
        }

        /// <summary>
        /// パフォーマンス設定を適用
        /// </summary>
        private void ApplyPerformanceSettings()
        {
            Application.targetFrameRate = targetFps;
            Time.fixedDeltaTime = fixedDeltaTime;
            QualitySettings.vSyncCount = 0; // VSync無効化（カスタムフレームレート制御のため）
        }

        /// <summary>
        /// カメラを設定（URP対応）
        /// </summary>
        private System.Collections.IEnumerator SetupCameras()
        {
            var cameraConfig = _config?.camera;
            var mainConfig = cameraConfig?.main;
            var hudConfig = cameraConfig?.hud;
            var viewport = cameraConfig?.viewport;
            var urpConfig = cameraConfig?.urp;

            // メインカメラ
            _mainCamera = new GameObject("Main Camera");
            _mainCamera.tag = "MainCamera";
            var mainCam = _mainCamera.AddComponent<Camera>();

            mainCam.clearFlags = mainConfig?.clearFlags ?? mainCameraClearFlags;
            mainCam.backgroundColor = mainConfig?.backgroundColor ?? mainCameraBackgroundColor;
            mainCam.cullingMask = mainConfig?.cullingMask ?? -1;
            mainCam.depth = mainConfig?.depth ?? -1;
            mainCam.renderPathType = mainConfig?.renderPathType ?? RenderPath.Automatic;
            mainCam.useOcclusionCulling = true;
            mainCam.allowHDR = false;
            mainCam.allowMSAA = false; // URP使用時はMSAA無効

            // URP対応
            if (urpConfig?.useUrp == true)
            {
                var urpCam = _mainCamera.AddComponent<UniversalAdditionalCameraData>();
                urpCam.requiresColorOption = CameraOverrideOption.Off;
                urpCam.requiresDepthOption = CameraOverrideOption.Off;
                urpCam.SetRendererListType(RenderType.Default, true);
            }

            // ビューポート設定
            if (viewport != null)
            {
                mainCam.rect = new Rect(viewport.x, viewport.y, viewport.width, viewport.height);
            }

            // URP Render Pipeline Assetを適用
            if (urpConfig != null)
            {
                var rpAsset = Resources.Load<RenderPipelineAsset>(urpConfig.renderPipelineAsset);
                if (rpAsset != null)
                {
                    GraphicsSettings.defaultRenderPipeline = rpAsset;
                }
            }

            yield return null;

            // HUDカメラ
            _hudCamera = new GameObject("HUD Camera");
            var hudCam = _hudCamera.AddComponent<Camera>();

            hudCam.clearFlags = hudConfig?.clearFlags ?? hudCameraClearFlags;
            hudCam.backgroundColor = hudConfig?.backgroundColor ?? new Color(0, 0, 0, 0);
            hudCam.depth = hudConfig?.depth ?? 10;
            hudCam.renderPathType = hudConfig?.renderPathType ?? RenderPath.Automatic;
            hudCam.orthographic = true;
            hudCam.orthographicSize = 5f;
            hudCam.nearClipPlane = 0.1f;
            hudCam.farClipPlane = 100f;
            hudCam.allowHDR = false;
            hudCam.allowMSAA = false;
            hudCam.enabled = false; // Canvasが自動でHUDカメラを使用するため

            // ビューポート設定
            if (viewport != null)
            {
                hudCam.rect = new Rect(viewport.x, viewport.y, viewport.width, viewport.height);
            }

            yield return null;

            // PlayerView（プレイヤー表現用）
            _playerView = CreatePlayerView();

            // HuntFovRendererの設定
            _huntFovRenderer = _mainCamera.AddComponent<HuntFovRenderer>();
            if (_playerView != null)
            {
                _huntFovRenderer.SetTarget(_playerView);
            }
        }

        /// <summary>
        /// プレイヤービューオブジェクトを生成
        /// </summary>
        private Transform CreatePlayerView()
        {
            var playerObject = new GameObject("Player View");
            playerObject.transform.SetParent(transform, false);

            // ディスクリンSpriteを生成
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

        /// <summary>
        /// Canvasを生成（Screen Space - Overlay）
        /// </summary>
        private System.Collections.IEnumerator SetupCanvas()
        {
            var uiConfig = _config?.ui;
            var canvasConfig = uiConfig?.canvas;
            var layoutConfig = uiConfig?.layout;

            // Canvas生成
            _canvas = new GameObject("GameCanvas");
            var canvas = _canvas.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = canvasConfig?.pixelPerfect ?? false;
            canvas.sortingGridSize = canvasConfig?.sortingGridSize ?? 16;

            // CanvasScaler追加
            var scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleWithScreenSize;

            var referenceResolution = new Vector2(
                layoutConfig?.clientWidth ?? GameLayout.DefaultClientWidth,
                layoutConfig?.clientHeight ?? GameLayout.DefaultClientHeight);
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // GraphicRenderer追加
            var graphicRenderer = _canvas.AddComponent<GraphicRaycaster>();
            graphicRenderer.referencePixelsPerUnit = 100f;

            yield return null;
        }

        /// <summary>
        /// AudioMixerを生成・設定
        /// </summary>
        private System.Collections.IEnumerator SetupAudioMixer()
        {
            var audioConfig = _config?.game?.audio;

            // AudioMixer生成
            var mixer = AudioMixer.CreateMixer(audioMixerName);
            if (mixer != null)
            {
                // AudioMixerのグループとバスを初期化
                mixer.CreateMixerGroup("Master");
                mixer.CreateMixerGroup("SFX");
                mixer.CreateMixerGroup("Music");

                // AudioMixerGOを作成してAudioMixerComponentをアタッチ
                _audioMixerGO = new GameObject("AudioMixer");
                var audioMixerComponent = _audioMixerGO.AddComponent<AudioMixerComponent>();
                audioMixerComponent.mixer = mixer;
            }

            // MasterVolumeの設定
            if (audioConfig != null)
            {
                SetMixerFloat(mixer, "MasterVolume", audioConfig.masterVolume);
                SetMixerFloat(mixer, "SFXVolume", audioConfig.sfxVolume);
                SetMixerFloat(mixer, "MusicVolume", audioConfig.musicVolume);
            }

            yield return null;
        }

        private void SetMixerFloat(AudioMixer mixer, string paramName, float value)
        {
            if (mixer == null) return;
            var dbValue = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
            mixer.SetFloat(paramName, dbValue);
        }

        /// <summary>
        /// UIScreenManagerと画面を初期化
        /// </summary>
        private System.Collections.IEnumerator SetupScreenManager()
        {
            var uiConfig = _config?.ui;
            var screensConfig = uiConfig?.screens;

            // ScreenManagerを生成
            var screenManagerGO = new GameObject("UIScreenManager");
            screenManagerGO.transform.SetParent(_canvas?.transform, false);
            _screenManager = screenManagerGO.AddComponent<UIScreenManager>();

            // 画面Prefabをセット
            _screenManager.screenPrefabs = screenPrefabs;

            // 初期画面をタイトルスクリーンに設定
            if (screensConfig?.titleScreen?.autoShow == true)
            {
                // InitializeScreens()がAwakeで呼ばれるため、ShowScreenは後から呼ぶ
            }

            yield return null;
        }

        /// <summary>
        /// GameHUDControllerを生成
        /// </summary>
        private System.Collections.IEnumerator SetupHUDController()
        {
            var hudConfig = _config?.ui?.screens?.gameHUD;
            if (hudConfig?.enabled != true)
                yield break;

            // HUDをCanvas配下に生成
            var hudGO = new GameObject("GameHUD");
            hudGO.transform.SetParent(_canvas?.transform, false);

            _hudController = hudGO.AddComponent<GameHUDController>();

            yield return null;
        }

        /// <summary>
        /// RyzechoGameControllerを生成
        /// </summary>
        private System.Collections.IEnumerator SetupGameController()
        {
            // GameControllerをシーンに生成
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
            _gameController.gameCamera = MainCamera;
            _gameController.playerView = _playerView;
            _gameController.huntFovRenderer = _huntFovRenderer;

            // ScreenManagerの参照をセット
            _gameController.screenManager = _screenManager;
            _gameController.hudController = _hudController;

            // ゲームモデルの初期化
            _gameController.InitializeGame();

            yield return null;
        }

        /// <summary>
        /// シーンをクリーンアップ
        /// </summary>
        public void Cleanup()
        {
            if (_mainCamera != null) Destroy(_mainCamera);
            if (_hudCamera != null) Destroy(_hudCamera);
            if (_canvas != null) Destroy(_canvas);
            if (_audioMixerGO != null) Destroy(_audioMixerGO);
            if (_gameController != null) Destroy(_gameController.gameObject);

            _mainCamera = null;
            _hudCamera = null;
            _canvas = null;
            _audioMixerGO = null;
            _screenManager = null;
            _hudController = null;
            _gameController = null;
        }

        /// <summary>
        /// ゲームシーン設定のJSONシリアライズ用データクラス
        /// </summary>
        [Serializable]
        private class GameSceneConfig
        {
            public CameraConfig camera;
            public UIConfig ui;
            public GameConfig game;
            public WorldConfig world;
            public PerformanceConfig performance;
        }

        [Serializable]
        private class CameraConfig
        {
            public MainCameraConfig main;
            public CameraConfigData hud;
            public ViewportConfig viewport;
            public URPConfig urp;
        }

        [Serializable]
        private class MainCameraConfig
        {
            public CameraClearFlags clearFlags;
            public Color backgroundColor;
            public int cullingMask;
            public int depth;
            public RenderPathType renderPathType;
            public bool supportedRenderingFeatures;
        }

        [Serializable]
        private class CameraConfigData
        {
            public CameraClearFlags clearFlags;
            public Color backgroundColor;
            public int depth;
            public RenderPathType renderPathType;
        }

        [Serializable]
        private class ViewportConfig
        {
            public float x, y, width, height;
        }

        [Serializable]
        private class URPConfig
        {
            public bool useUrp;
            public string renderPipelineAsset;
            public string defaultMaterial;
        }

        [Serializable]
        private class UIConfig
        {
            public CanvasConfig canvas;
            public LayoutConfig layout;
            public ScreensConfig screens;
        }

        [Serializable]
        private class CanvasConfig
        {
            public RenderMode renderMode;
            public string screenSpaceCamera;
            public bool pixelPerfect;
            public int sortingGridSize;
            public int defaultSortOrder;
        }

        [Serializable]
        private class LayoutConfig
        {
            public int clientWidth;
            public int clientHeight;
            public int topBarHeight;
            public int sidePanelGap;
            public int sidePanelWidth;
            public int bottomHudHeight;
            public int margin;
        }

        [Serializable]
        private class ScreensConfig
        {
            public ScreenConfigData titleScreen;
            public ScreenConfigData mainMenu;
            public ScreenConfigData difficultySelect;
            public ScreenConfigData setupScreen;
            public ScreenConfigData gameOver;
            public ScreenConfigData gameHUD;
            public ScreenConfigData pauseOverlay;
        }

        [Serializable]
        private class ScreenConfigData
        {
            public bool enabled;
            public bool autoShow;
            public string name;
        }

        [Serializable]
        private class GameConfig
        {
            public InitialConfig initial;
            public RoundFlowConfig roundFlow;
            public EconomyConfig economy;
            public BossConfig boss;
            public UltimateConfig ultimate;
            public AudioConfig audio;
            public InputConfig input;
        }

        [Serializable]
        private class InitialConfig
        {
            public int roundsToWin;
            public int startingCredits;
            public int initialBuildPoints;
            public int teamSize;
            public string startingPhase;
        }

        [Serializable]
        private class RoundFlowConfig
        {
            public int regulationSideSwitchRound;
            public int overtimeTriggerScore;
            public float roundDurationSeconds;
            public float bombPlantSeconds;
            public float bombFuseSeconds;
            public float bombDefuseSeconds;
        }

        [Serializable]
        private class EconomyConfig
        {
            public int winRewardCredits;
            public int lossRewardCredits;
            public int killRewardCredits;
            public int objectiveRewardCredits;
            public int bossKillDividendCredits;
            public int bossEliminationBonusCredits;
        }

        [Serializable]
        private class BossConfig
        {
            public int maxBossSelectionsPerActor;
            public int optimalBossInvestment;
            public int bossPayoutMultiplier;
        }

        [Serializable]
        private class UltimateConfig
        {
            public int maxUltPoints;
        }

        [Serializable]
        private class AudioConfig
        {
            public float masterVolume;
            public float sfxVolume;
            public float musicVolume;
            public string audioMixerName;
        }

        [Serializable]
        private class InputConfig
        {
            public float mouseSensitivity;
            public float lookSpeed;
            public float scrollSensitivity;
        }

        [Serializable]
        private class WorldConfig
        {
            public float[] origin;
            public float cellSize;
            public int gridColumns;
            public int gridRows;
            public PerspectiveConfig perspective;
            public HuntCameraConfig huntCamera;
        }

        [Serializable]
        private class PerspectiveConfig
        {
            public float scaleX, scaleY, shearX, topInset;
        }

        [Serializable]
        private class HuntCameraConfig
        {
            public float zoom;
            public float visibleWorldFractionX, visibleWorldFractionY;
            public float targetX, targetY;
        }

        [Serializable]
        private class PerformanceConfig
        {
            public int targetFps;
            public float fixedDeltaTime;
            public int maxParticleCount;
            public bool useGPUInstancing;
            public bool enableObjectPooling;
        }
    }

    /// <summary>
    /// AudioMixerを管理するコンポーネント。
    /// AudioMixerAssetをアタッチしてミキシングを制御。
    /// </summary>
    public class AudioMixerComponent : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private float masterVolume = 1.0f;
        [SerializeField] private float sfxVolume = 1.0f;
        [SerializeField] private float musicVolume = 1.0f;

        public AudioMixer Mixer => mixer;

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            var db = Mathf.Log10(Mathf.Max(masterVolume, 0.0001f)) * 20f;
            mixer?.SetFloat("MasterVolume", db);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            var db = Mathf.Log10(Mathf.Max(sfxVolume, 0.0001f)) * 20f;
            mixer?.SetFloat("SFXVolume", db);
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            var db = Mathf.Log10(Mathf.Max(musicVolume, 0.0001f)) * 20f;
            mixer?.SetFloat("MusicVolume", db);
        }
    }
}
