using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections.Generic;

namespace RYZECHo
{
    /// <summary>
    /// メインゲームコントローラー。
    /// ゲームライフサイクル管理（初期化→ゲームループ→終了）、
    /// UI画面遷移の制御（UIScreenManager連携）、
    /// ゲーム状態とUIの同期、InputSystemとの連携、AudioMixerとの連携を管理する。
    /// </summary>
    public class RyzechoGameController : MonoBehaviour
    {
        // ==================== セットアップ用シリアライズフィールド ====================

        [Header("Scene References")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Transform playerView;
        [SerializeField] private HuntFovRenderer huntFovRenderer;

        [Header("Settings SO")]
        [SerializeField] private GameplaySettingsSO gameplaySettings;
        [SerializeField] private GameRulesSettingsSO gameRulesSettings;
        [SerializeField] private LayoutSettingsSO layoutSettings;
        [SerializeField] private VisualSettingsSO visualSettings;
        [SerializeField] private AudioSettingsSO audioSettings;

        [Header("World Mapping")]
        [SerializeField] private Vector2 worldOrigin = new(-9f, -6f);
        [SerializeField] private float cellSizeUnits = 1f;

        [Header("Camera")]
        [SerializeField] private bool followPlayerCamera = true;
        [SerializeField] private Vector3 cameraOffset = new(0f, 0f, -10f);
        [SerializeField] private float cameraLerp = 18f;
        [SerializeField] private float orthographicSize = 6.5f;

        [Header("Game Settings")]
        [SerializeField] private int initialRoundsToWin = GameRules.RoundsToWin;
        [SerializeField] private int initialStartingCredits = GameRules.StartingCredits;
        [SerializeField] private int initialBuildPoints = GameRules.InitialBuildPoints;
        [SerializeField] private int teamSize = GameRules.TeamSize;
        [SerializeField] private int targetFps = 60;
        [SerializeField] private float fixedDeltaTime = 0.016666f;

        // ==================== 外部参照 ====================

        /// <summary>UIScreenManagerの参照（外部からセット）</summary>
        public UIScreenManager screenManager;

        /// <summary>GameHUDControllerの参照（外部からセット）</summary>
        public GameHUDController hudController;

        // ==================== ゲーム状態 ====================

        private IGameModel _gameModel;
        private IGameModelFactory _factory;
        private IEventBus _eventBus;

        // ゲーム状態フラグ
        private bool _isGameActive;
        private bool _isPaused;
        private bool _isGameInitialized;
        private GamePhase _currentPhase;

        // ゲーム設定値
        public int InitialRoundsToWin { get => initialRoundsToWin; set => initialRoundsToWin = value; }
        public int InitialStartingCredits { get => initialStartingCredits; set => initialStartingCredits = value; }
        public int InitialBuildPoints { get => initialBuildPoints; set => initialBuildPoints = value; }
        public int TeamSize { get => teamSize; set => teamSize = value; }
        public int TargetFps { get => targetFps; set => targetFps = value; }
        public float FixedDeltaTime { get => fixedDeltaTime; set => fixedDeltaTime = value; }

        // 難易度とターン数
        private int _selectedDifficulty;
        private int _setupTurnCount;

        // プレイヤー表現用
        private Sprite _generatedPlayerSprite;

        // ==================== イベント ====================

        /// <summary>ゲーム開始時に発火</summary>
        public event Action OnGameStarted;

        /// <summary>ゲーム終了時に発火</summary>
        public event Action<bool, int> OnGameEnded;

        /// <summary>フェーズ変更時に発火</summary>
        public event Action<GamePhase> OnPhaseChanged;

        /// <summary>一時停止状態変更時に発火</summary>
        public event Action<bool> OnPausedChanged;

        /// <summary>オーディオキュー発火時に発火</summary>
        public event Action<RippleKind, PointF, float> OnAudioCue;

        // ==================== ゲームライフサイクル ====================

        /// <summary>
        /// ゲームを初期化（GameModel生成と設定）
        /// </summary>
        public void InitializeGame()
        {
            if (_isGameInitialized)
            {
                Debug.LogWarning("[RyzechoGameController] Already initialized.");
                return;
            }

            // パフォーマンス設定
            Application.targetFrameRate = targetFps;
            Time.fixedDeltaTime = fixedDeltaTime;
            QualitySettings.vSyncCount = 0;

            // ゲームモデルを生成
            _factory = GameModelFactory.Instance;
            _gameModel = _factory.Create(
                gameRules: gameRulesSettings,
                layoutSettings: layoutSettings,
                gameplaySettings: gameplaySettings);

            // EventBusをセット
            _eventBus = GameEventBusAdapter.Instance;
            SubscribeToEvents();

            // HUDにGameModelをセット
            if (hudController != null)
            {
                hudController.SetGameModel((GameModel)_gameModel);
            }

            // HuntFovRendererの設定
            if (huntFovRenderer != null && playerView != null)
            {
                huntFovRenderer.SetTarget(playerView);
            }

            // プレイヤー表現の初期化
            EnsurePlayerView();

            _isGameActive = false;
            _isPaused = false;
            _isGameInitialized = true;

            // 初期画面をタイトルスクリーンに
            if (screenManager != null)
            {
                screenManager.ShowScreen(UIScreenManager.ScreenType.TitleScreen);
            }

            Debug.Log("[RyzechoGameController] Game initialized.");
        }

        /// <summary>
        /// ゲームを開始（難易度・ターン数付き）
        /// </summary>
        public void StartGame(int difficulty = 0, int turnCount = 10)
        {
            if (!_isGameInitialized)
            {
                Debug.LogWarning("[RyzechoGameController] Game not initialized. Call InitializeGame() first.");
                return;
            }

            _selectedDifficulty = difficulty;
            _setupTurnCount = turnCount;

            // ゲームモデルに設定を適用
            ApplyGameSettings();

            // ゲームアクティブ状態を有効化
            _isGameActive = true;
            _isPaused = false;

            // ScreenManager経由でゲーム開始を通知
            if (screenManager != null)
            {
                screenManager.StartGame(difficulty, turnCount);
            }

            // ゲームHUDを表示
            if (screenManager != null)
            {
                screenManager.ShowScreen(UIScreenManager.ScreenType.SetupScreen);
            }

            OnGameStarted?.Invoke();

            Debug.Log($"[RyzechoGameController] Game started. Difficulty: {difficulty}, Turns: {turnCount}");
        }

        /// <summary>
        /// ゲームを終了（結果付き）
        /// </summary>
        public void EndGame(bool isVictory, int score)
        {
            if (!_isGameActive)
            {
                Debug.LogWarning("[RyzechoGameController] Game is not active.");
                return;
            }

            _isGameActive = false;

            // ScreenManager経由でゲーム終了を通知
            if (screenManager != null)
            {
                screenManager.EndGame(isVictory, score);
            }

            // ゲームオーバー画面を表示
            if (screenManager != null)
            {
                screenManager.ShowGameOver(isVictory, score, _setupTurnCount);
            }

            OnGameEnded?.Invoke(isVictory, score);

            Debug.Log($"[RyzechoGameController] Game ended. Victory: {isVictory}, Score: {score}");
        }

        /// <summary>
        /// ゲームをリセット（タイトル画面に戻る）
        /// </summary>
        public void ResetGame()
        {
            _isGameActive = false;
            _isPaused = false;

            // ScreenManager経由でゲームリセット
            if (screenManager != null)
            {
                screenManager.ResetGame();
            }

            // ゲームモデルを再生成
            if (_gameModel != null)
            {
                _gameModel = _factory.Create(
                    gameRules: gameRulesSettings,
                    layoutSettings: layoutSettings,
                    gameplaySettings: gameplaySettings);

                if (hudController != null)
                {
                    hudController.SetGameModel((GameModel)_gameModel);
                }
            }

            Debug.Log("[RyzechoGameController] Game reset to title screen.");
        }

        /// <summary>
        /// ゲームの一時停止を切り替え
        /// </summary>
        public void TogglePause()
        {
            _isPaused = !_isPaused;
            _gameModel.IsPaused = _isPaused;

            if (OnPausedChanged != null)
            {
                OnPausedChanged(_isPaused);
            }

            // PauseOverlay画面の表示/非表示
            if (screenManager != null)
            {
                if (_isPaused)
                {
                    screenManager.ShowScreen(UIScreenManager.ScreenType.MainMenu);
                }
                else
                {
                    // 前の画面に戻る（SetupScreenまたはGameHUD）
                    screenManager.ShowScreen(UIScreenManager.ScreenType.SetupScreen);
                }
            }

            Debug.Log($"[RyzechoGameController] Paused: {_isPaused}");
        }

        // ==================== ゲームループ ====================

        private void Update()
        {
            if (!_isGameInitialized) return;

            // ゲームがアクティブでない場合はUpdateしない
            if (!_isGameActive) return;

            // 一時停止中はUpdateしない
            if (_isPaused) return;

            // InputSystemからの入力をキャプチャ
            var snapshot = InputAdapter.Capture();

            // キーボード入力（特殊キー）
            HandleSpecialInputs();

            // ゲームモデルを更新
            _gameModel.Update(Time.deltaTime, snapshot);

            // プレイヤー表現を同期
            SyncPlayerView();

            // カメラを同期
            SyncCamera();

            // HUDを更新
            UpdateHUD();

            // フェーズ変更を検出
            CheckPhaseChange();
        }

        /// <summary>
        /// 特殊キー入力を処理（Tab, B, Escape, P, Enter）
        /// </summary>
        private void HandleSpecialInputs()
        {
            // Tab: ビルドツールサイクル
            if (InputAdapter.IsKeyDown(KeyCode.Tab))
            {
                _gameModel.CycleBuildTool();
            }

            // B: ブリーフィング表示切替
            if (InputAdapter.IsKeyDown(KeyCode.B))
            {
                _gameModel.ToggleBriefing();
            }

            // Escape: 一時停止
            if (InputAdapter.IsKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }

            // P: 一時停止（別キー）
            if (InputAdapter.IsKeyDown(KeyCode.P))
            {
                TogglePause();
            }

            // Enter: ゲーム開始/再開
            if (InputAdapter.IsKeyDown(KeyCode.Return) || InputAdapter.IsKeyDown(KeyCode.KeypadEnter))
            {
                if (!_isGameActive)
                {
                    StartGame(_selectedDifficulty, _setupTurnCount);
                }
                else if (_isPaused)
                {
                    TogglePause();
                }
            }

            // マウスクリック入力
            if (InputAdapter.IsMouseButtonDown(0))
            {
                _gameModel.HandleLeftClick(InputAdapter.CaptureMousePoint());
            }

            if (InputAdapter.IsMouseButtonDown(1))
            {
                _gameModel.HandleRightClick(InputAdapter.CaptureMousePoint());
            }
        }

        // ==================== プレイヤー表現同期 ====================

        private void EnsurePlayerView()
        {
            if (playerView != null) return;

            var playerObject = new GameObject("Player View");
            playerObject.transform.SetParent(transform, false);
            playerView = playerObject.transform;

            var spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateDiscSprite();
            spriteRenderer.color = new Color(0.48f, 0.9f, 1f, 1f);
            spriteRenderer.sortingOrder = 50;
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

        private void SyncPlayerView()
        {
            if (playerView == null) return;

            playerView.position = ModelToUnityPosition(_gameModel.PlayerModelPosition);
            playerView.rotation = Quaternion.Euler(0f, 0f, -_gameModel.PlayerFacingRadians * Mathf.Rad2Deg);

            if (huntFovRenderer != null)
            {
                huntFovRenderer.SetTarget(playerView);
                huntFovRenderer.SetVision(
                    _gameModel.PlayerFovDegrees,
                    _gameModel.PlayerVisionRange / _gameModel.ModelCellSize * cellSizeUnits);
            }
        }

        private void SyncCamera()
        {
            if (!followPlayerCamera || gameCamera == null || playerView == null) return;

            gameCamera.orthographic = true;
            gameCamera.orthographicSize = orthographicSize;

            var target = playerView.position + cameraOffset;
            gameCamera.transform.position = Vector3.Lerp(
                gameCamera.transform.position,
                target,
                1f - Mathf.Exp(-cameraLerp * Time.deltaTime));
        }

        private Vector3 ModelToUnityPosition(PointF modelPosition)
        {
            var cellSpace = _gameModel.ModelToCellSpace(modelPosition);
            return new Vector3(
                worldOrigin.x + (cellSpace.X * cellSizeUnits),
                worldOrigin.y + (cellSpace.Y * cellSizeUnits),
                0f);
        }

        // ==================== HUD同期 ====================

        private void UpdateHUD()
        {
            if (hudController == null) return;

            // HUD状態を更新
            hudController.UpdateHUD();

            // 敵ユニット表示を更新
            UpdateEnemyDisplays();

            // アクティビティフィードを更新
            UpdateActivityFeed();
        }

        private void UpdateEnemyDisplays()
        {
            if (hudController == null || _gameModel == null) return;

            // 敵・味方のアクター情報を取得してHUDに反映
            // GameModelから直接取得する方法が望ましいが、
            // 現在のAPIではGetPlayerHealth等のみが公開されている。
            // 拡張が必要な場合はIGameModelにメソッドを追加する。
        }

        private void UpdateActivityFeed()
        {
            if (hudController == null) return;

            // アクティビティフィードの更新は、
            // GameModelからのイベント通知でトリガーされる
        }

        // ==================== フェーズ変更検出 ====================

        private void CheckPhaseChange()
        {
            var phase = _gameModel.Phase;
            if (phase != _currentPhase)
            {
                _currentPhase = phase;

                // フェーズ変更イベントを通知
                OnPhaseChanged?.Invoke(phase);

                // フェーズに応じたUI画面遷移
                HandlePhaseChange(phase);
            }
        }

        private void HandlePhaseChange(GamePhase phase)
        {
            if (screenManager == null) return;

            switch (phase)
            {
                case GamePhase.Construct:
                    screenManager.ShowScreen(UIScreenManager.ScreenType.SetupScreen);
                    break;

                case GamePhase.Bet:
                    screenManager.ShowScreen(UIScreenManager.ScreenType.SetupScreen);
                    break;

                case GamePhase.Hunt:
                    screenManager.ShowScreen(UIScreenManager.ScreenType.SetupScreen);
                    break;

                case GamePhase.RoundResult:
                    // ラウンド結果画面を表示
                    break;

                case GamePhase.Victory:
                    EndGame(true, GetPlayerScore());
                    break;

                case GamePhase.Defeat:
                    EndGame(false, GetPlayerScore());
                    break;
            }
        }

        private int GetPlayerScore()
        {
            if (_gameModel == null) return 0;
            return _gameModel.GetPlayerRoundWins();
        }

        // ==================== ゲーム設定適用 ====================

        private void ApplyGameSettings()
        {
            // ゲームモデルに設定を適用
            // GameModelが設定値を受け取るインターフェースを持てばそこに渡す
        }

        // ==================== イベント購読 ====================

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;

            // ゲームイベントを購読
            _eventBus.Subscribe<GameEvent>(OnGameEvent);
            _eventBus.Subscribe<GameEvent.ActorDeath>(OnActorDeath);
            _eventBus.Subscribe<GameEvent.Damage>(OnDamage);
            _eventBus.Subscribe<GameEvent.StructureBuilt>(OnStructureBuilt);
            _eventBus.Subscribe<GameEvent.StructureDestroyed>(OnStructureDestroyed);
            _eventBus.Subscribe<GameEvent.PhaseChanged>(OnPhaseChangedEvent);
            _eventBus.Subscribe<GameEvent.AudioCue>(OnAudioCueEvent);

            // ゲームモデルのオーディオキューイベントも購読
            _gameModel.AudioCueEmitted += OnModelAudioCue;
        }

        private void OnGameEvent(GameEvent evt)
        {
            // 汎用ゲームイベント処理
            Debug.Log($"[RyzechoGameController] GameEvent: {evt.GetType().Name}");
        }

        private void OnActorDeath(GameEvent.ActorDeath evt)
        {
            Debug.Log($"[RyzechoGameController] ActorDeath: {evt.ActorName}");
        }

        private void OnDamage(GameEvent.Damage evt)
        {
            Debug.Log($"[RyzechoGameController] Damage: {evt.Amount}");
        }

        private void OnStructureBuilt(GameEvent.StructureBuilt evt)
        {
            Debug.Log($"[RyzechoGameController] StructureBuilt: {evt.Kind}");
        }

        private void OnStructureDestroyed(GameEvent.StructureDestroyed evt)
        {
            Debug.Log($"[RyzechoGameController] StructureDestroyed: {evt.Kind}");
        }

        private void OnPhaseChangedEvent(GameEvent.PhaseChanged evt)
        {
            Debug.Log($"[RyzechoGameController] PhaseChanged: {evt.OldPhase} -> {evt.NewPhase}");
        }

        private void OnAudioCueEvent(GameEvent.AudioCue evt)
        {
            Debug.Log($"[RyzechoGameController] AudioCue: {evt.Kind} at {evt.Position}");
        }

        private void OnModelAudioCue(RippleKind kind, PointF position, float volume)
        {
            OnAudioCue?.Invoke(kind, position, volume);

            // AudioMixer経由でオーディオキューを発行
            PlayAudioCue(kind, position, volume);
        }

        // ==================== オーディオ ====================

        /// <summary>
        /// オーディオキューを再生（AudioMixer連携）
        /// </summary>
        private void PlayAudioCue(RippleKind kind, PointF position, float volume)
        {
            // AudioMixer経由でオーディオを再生
            // 3Dオーディオの場合、positionから距離を計算してvolumeを調整
            // 2Dオーディオの場合、volumeを直接適用
            // 実際のUnity AudioMixerでの再生は、AudioSourceとAudioMixerSnapshotを使用
        }

        /// <summary>
        /// オーディオミキサーのボリュームを設定
        /// </summary>
        public void SetAudioVolume(AudioChannel channel, float volume)
        {
            // AudioMixerComponentがあればそちらに設定を伝播
            // 実装はGameSceneSetupのAudioMixerComponentに依存
        }

        public enum AudioChannel
        {
            Master,
            SFX,
            Music,
        }

        // ==================== ゲーム状態取得 ====================

        /// <summary>現在のフェーズ</summary>
        public GamePhase CurrentPhase => _gameModel?.Phase ?? GamePhase.Construct;

        /// <summary>ゲームがアクティブか</summary>
        public bool IsGameActive => _isGameActive;

        /// <summary>一時停止中か</summary>
        public bool IsPaused => _isPaused;

        /// <summary>ゲームが初期化済みか</summary>
        public bool IsInitialized => _isGameInitialized;

        /// <summary>プレイヤーの生存状態</summary>
        public bool PlayerIsAlive => _gameModel?.PlayerIsAlive ?? false;

        /// <summary>プレイヤーの位置</summary>
        public PointF PlayerPosition => _gameModel?.PlayerModelPosition ?? new PointF(0, 0);

        /// <summary>プレイヤーの向き</summary>
        public float PlayerFacing => _gameModel?.PlayerFacingRadians ?? 0f;

        /// <summary>プレイヤーのFOV</summary>
        public float PlayerFov => _gameModel?.PlayerFovDegrees ?? GameRules.DefaultFovDegrees;

        /// <summary>プレイヤーの視認範囲</summary>
        public float PlayerVisionRange => _gameModel?.PlayerVisionRange ?? 0f;

        /// <summary>プレイヤーのHP</summary>
        public float PlayerHealth => _gameModel?.GetPlayerHealth() ?? 0f;

        /// <summary>プレイヤーの最大HP</summary>
        public float PlayerMaxHealth => _gameModel?.GetPlayerMaxHealth() ?? 0f;

        /// <summary>プレイヤーのシールド</summary>
        public float PlayerShield => _gameModel?.GetPlayerShield() ?? 0f;

        /// <summary>プレイヤーの最大シールド</summary>
        public float PlayerMaxShield => _gameModel?.GetPlayerMaxShield() ?? 0f;

        /// <summary>プレイヤーのラウンド勝利数</summary>
        public int PlayerRoundWins => _gameModel?.GetPlayerRoundWins() ?? 0;

        /// <summary>敵のラウンド勝利数</summary>
        public int EnemyRoundWins => _gameModel?.GetEnemyRoundWins() ?? 0;

        /// <summary>現在のラウンド</summary>
        public int CurrentRound => _gameModel?.GetCurrentRound() ?? 0;

        /// <summary>フェーズラベル</summary>
        public string PhaseLabel => _gameModel?.GetPhaseLabel() ?? "";

        /// <summary>エージェント名</summary>
        public string AgentName => _gameModel?.GetAgentName() ?? "";

        /// <summary>武器名</summary>
        public string WeaponName => _gameModel?.GetWeaponName() ?? "";

        /// <summary>クレジット</summary>
        public int Credits => _gameModel?.GetCredits() ?? 0;

        /// <summary>ウルティメットポイント</summary>
        public int UltPoints => _gameModel?.GetUltPoints() ?? 0;

        /// <summary>ラウンドタイマー</summary>
        public float RoundTimer => _gameModel?.GetRoundTimer() ?? 0f;

        /// <summary>ブリーフィング表示中か</summary>
        public bool ShowBriefing => _gameModel?.GetShowBriefing() ?? false;

        /// <summary>結果メッセージ</summary>
        public string ResultMessage => _gameModel?.GetResultMessage() ?? "";

        // ==================== クリーンアップ ====================

        private void OnDestroy()
        {
            // イベント購読を解除
            if (_eventBus != null && _gameModel != null)
            {
                _eventBus.Unsubscribe<GameEvent>(OnGameEvent);
                _eventBus.Unsubscribe<GameEvent.ActorDeath>(OnActorDeath);
                _eventBus.Unsubscribe<GameEvent.Damage>(OnDamage);
                _eventBus.Unsubscribe<GameEvent.StructureBuilt>(OnStructureBuilt);
                _eventBus.Unsubscribe<GameEvent.StructureDestroyed>(OnStructureDestroyed);
                _eventBus.Unsubscribe<GameEvent.PhaseChanged>(OnPhaseChangedEvent);
                _eventBus.Unsubscribe<GameEvent.AudioCue>(OnAudioCueEvent);

                _gameModel.AudioCueEmitted -= OnModelAudioCue;
            }

            // プレイヤー表現のクリーンアップ
            if (_generatedPlayerSprite != null)
            {
                Destroy(_generatedPlayerSprite.texture);
                Destroy(_generatedPlayerSprite);
                _generatedPlayerSprite = null;
            }
        }
    }
}
