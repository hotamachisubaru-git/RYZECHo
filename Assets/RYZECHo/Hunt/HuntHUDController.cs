using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using RYZECHo.UI;
using RYZECHo.Unity;
using Color = UnityEngine.Color;

namespace RYZECHo.Hunt
{
    /// <summary>
    /// Huntフェーズ専用HUDコントローラー。
    /// Huntフェーズ中にプレイヤーの視界状態（FOV、HP、シールド、ステータス）をHUDで表示する。
    /// HuntFovRendererとの連携、GameHUDControllerとの統合更新、
    /// RyzechoGameControllerのOnPhaseChangedイベントと連携する。
    /// </summary>
    public class HuntHUDController : MonoBehaviour
    {
        [Header("FOV Mode Indicator")]
        [Tooltip("視界モード表示テキスト (Standard100/Wide120/Sniper80)")]
        [SerializeField] private TextMeshProUGUI fovModeText;

        [Tooltip("視界モード表示の背景Image")]
        [SerializeField] private Image fovModeBackground;

        [Tooltip("視界モードインジケーターのActive状態用Image")]
        [SerializeField] private Image fovIndicatorStandard;

        [Tooltip("WideモードインジケーターのActive状態用Image")]
        [SerializeField] private Image fovIndicatorWide;

        [Tooltip("SniperモードインジケーターのActive状態用Image")]
        [SerializeField] private Image fovIndicatorSniper;

        [Header("Player Stats")]
        [Tooltip("プレイヤーHPバーの背景Image")]
        [SerializeField] private Image hpBarBackground;

        [Tooltip("プレイヤーHPバーの充填Image")]
        [SerializeField] private Image hpBarFill;

        [Tooltip("プレイヤーHPバーのシールドImage")]
        [SerializeField] private Image hpBarShield;

        [Tooltip("プレイヤーHPテキスト")]
        [SerializeField] private TextMeshProUGUI hpText;

        [Tooltip("プレイヤーシールドテキスト")]
        [SerializeField] private TextMeshProUGUI shieldText;

        [Header("Score Display")]
        [Tooltip("スコア表示テキスト (SCORE X - Y)")]
        [SerializeField] private TextMeshProUGUI scoreText;

        [Tooltip("プレイヤー勝利数テキスト")]
        [SerializeField] private TextMeshProUGUI playerScoreText;

        [Tooltip("敵勝利数テキスト")]
        [SerializeField] private TextMeshProUGUI enemyScoreText;

        [Header("Credit Display")]
        [Tooltip("プレイヤークレジット表示")]
        [SerializeField] private TextMeshProUGUI creditsText;

        [Tooltip("クレジットアイコンImage")]
        [SerializeField] private Image creditIcon;

        [Header("Round Timer")]
        [Tooltip("ラウンドタイマーテキスト")]
        [SerializeField] private TextMeshProUGUI roundTimerText;

        [Tooltip("ラウンド番号テキスト (第Xラウンド)")]
        [SerializeField] private TextMeshProUGUI roundText;

        [Header("Enemy Detection")]
        [Tooltip("敵探测状況表示テキスト (アクティブな敵数)")]
        [SerializeField] private TextMeshProUGUI enemyCountText;

        [Tooltip("敵探测インジケーターの背景Image")]
        [SerializeField] private Image enemyCountBackground;

        [Header("Agent Skills")]
        [Tooltip("エージェントスキル冷却表示の親Transform")]
        [SerializeField] private Transform skillCooldownParent;

        [Header("Phase Label")]
        [Tooltip("フェーズ表示テキスト (HUNT, CONSTRUCT, BET等)")]
        [SerializeField] private TextMeshProUGUI phaseText;

        [Header("Agent Name")]
        [Tooltip("プレイヤーエージェント名")]
        [SerializeField] private TextMeshProUGUI agentNameText;

        [Header("Weapon Name")]
        [Tooltip("プレイヤー装備武器名")]
        [SerializeField] private TextMeshProUGUI weaponNameText;

        // ==================== 内部状態 ====================

        private bool _isActive;
        private HuntFovMode _currentFovMode;
        private float _currentFovDegrees;

        // HuntFovRendererの参照（外部からセットまたは検索）
        private HuntFovRenderer _huntFovRenderer;

        // GameHUDControllerの参照（統合更新用）
        private GameHUDController _gameHudController;

        // RyzechoGameControllerの参照
        private RyzechoGameController _gameController;

        // スキル冷却表示のキャッシュ
        private readonly Dictionary<string, SkillCooldownDisplay> _skillDisplays = new();

        // スキル冷却表示用Prefabパス
        private const string SkillCooldownPrefabPath = "Assets/RYZECHo/Hunt/Prefabs/SkillCooldownDisplay.prefab";

        private sealed class SkillCooldownDisplay
        {
            public GameObject gameObject { get; init; }
        }

        // ==================== Unityライフサイクル ====================

        private void Awake()
        {
            // HuntFovRendererを検索
            _huntFovRenderer = FindObjectOfType<HuntFovRenderer>();

            // GameHUDControllerを検索
            var hudControllers = FindObjectsOfType<GameHUDController>();
            if (hudControllers.Length > 0)
            {
                _gameHudController = hudControllers[0];
            }

            // RyzechoGameControllerを検索
            _gameController = FindObjectOfType<RyzechoGameController>();
            if (_gameController != null)
            {
                _gameController.OnPhaseChanged += OnPhaseChanged;
            }

            _isActive = false;
        }

        private void OnDestroy()
        {
            if (_gameController != null)
            {
                _gameController.OnPhaseChanged -= OnPhaseChanged;
            }

            // スキル表示のクリーンアップ
            foreach (var kvp in _skillDisplays)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _skillDisplays.Clear();
        }

        private void Update()
        {
            if (!_isActive) return;

            UpdateHUD();
        }

        // ==================== フェーズ遷移イベント ====================

        /// <summary>
        /// RyzechoGameControllerのOnPhaseChangedイベントハンドラー。
        /// Huntフェーズのときのみアクティブになる。
        /// </summary>
        private void OnPhaseChanged(GamePhase phase)
        {
            _isActive = (phase == GamePhase.Hunt);

            if (_isActive)
            {
            }
        }

        // ==================== HUD更新 ====================

        /// <summary>
        /// 毎フレームHUD状態を更新する。
        /// Huntフェーズ固有のHUD表示ロジックを適用する。
        /// </summary>
        private void UpdateHUD()
        {
            if (_gameController == null) return;

            UpdateFovModeIndicator();
            UpdatePlayerStats();
            UpdateScoreDisplay();
            UpdateCreditDisplay();
            UpdateRoundTimer();
            UpdateEnemyCount();
            UpdateSkillCooldowns();
            UpdatePhaseLabel();
            UpdateAgentInfo();
        }

        /// <summary>
        /// 視界モードインジケーターを更新する。
        /// HuntFovRendererのCurrentFovDegreesと連動する。
        /// </summary>
        private void UpdateFovModeIndicator()
        {
            float fovDegrees = 100f;

            // HuntFovRendererから現在のFOVを取得
            if (_huntFovRenderer != null)
            {
                fovDegrees = _huntFovRenderer.CurrentFovDegrees;
                _currentFovDegrees = fovDegrees;
                _currentFovMode = DetectFovMode(fovDegrees);
            }
            else if (_gameController != null)
            {
                fovDegrees = _gameController.PlayerFov;
                _currentFovDegrees = fovDegrees;
                _currentFovMode = DetectFovMode(fovDegrees);
            }

            // FOVモードテキストを更新
            if (fovModeText != null)
            {
                fovModeText.text = _currentFovMode.ToString();
            }

            // FOVインジケーターのActive状態を更新
            if (fovIndicatorStandard != null)
                fovIndicatorStandard.gameObject.SetActive(_currentFovMode == HuntFovMode.Standard100);

            if (fovIndicatorWide != null)
                fovIndicatorWide.gameObject.SetActive(_currentFovMode == HuntFovMode.Wide120);

            if (fovIndicatorSniper != null)
                fovIndicatorSniper.gameObject.SetActive(_currentFovMode == HuntFovMode.Sniper80);

            // 背景色をFOVモードに応じて変更
            if (fovModeBackground != null)
            {
                Color bgColor = GetFovModeColor(_currentFovMode);
                fovModeBackground.color = bgColor;
            }
        }

        /// <summary>
        /// FOV度数からHuntFovModeを推測する。
        /// </summary>
        private HuntFovMode DetectFovMode(float fovDegrees)
        {
            const float tolerance = 5f;

            if (Mathf.Abs(fovDegrees - 100f) < tolerance)
                return HuntFovMode.Standard100;

            if (Mathf.Abs(fovDegrees - 120f) < tolerance)
                return HuntFovMode.Wide120;

            if (Mathf.Abs(fovDegrees - 80f) < tolerance)
                return HuntFovMode.Sniper80;

            return HuntFovMode.Custom;
        }

        /// <summary>
        /// FOVモードに応じたインジケーターカラーを返す。
        /// </summary>
        private Color GetFovModeColor(HuntFovMode mode)
        {
            return mode switch
            {
                HuntFovMode.Standard100 => new Color(0.3f, 0.9f, 1f, 0.3f),   // ブルー
                HuntFovMode.Wide120 => new Color(0.2f, 1f, 0.4f, 0.3f),       // グリーン
                HuntFovMode.Sniper80 => new Color(1f, 0.3f, 0.3f, 0.3f),      // レッド
                _ => new Color(0.5f, 0.5f, 0.5f, 0.3f),                       // グレー
            };
        }

        /// <summary>
        /// プレイヤーHP/シールドバーを更新する。
        /// </summary>
        private void UpdatePlayerStats()
        {
            if (_gameController == null) return;

            float playerHealth = _gameController.PlayerHealth;
            float playerMaxHealth = _gameController.PlayerMaxHealth;
            float playerShield = _gameController.PlayerShield;
            float playerMaxShield = _gameController.PlayerMaxShield;

            // HPバー更新
            if (hpBarFill != null)
            {
                hpBarFill.fillAmount = playerMaxHealth > 0f
                    ? Mathf.Clamp01(playerHealth / playerMaxHealth)
                    : 0f;
            }

            // シールドバー更新
            if (hpBarShield != null)
            {
                hpBarShield.fillAmount = playerMaxShield > 0f
                    ? Mathf.Clamp01(playerShield / playerMaxShield)
                    : 0f;
                hpBarShield.gameObject.SetActive(playerShield > 0.01f);
            }

            // HPテキスト更新
            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt(playerHealth)} / {Mathf.CeilToInt(playerMaxHealth)}";
            }

            // シールドテキスト更新
            if (shieldText != null)
            {
                shieldText.text = playerShield > 0.01f ? $"シールド: {Mathf.CeilToInt(playerShield)}" : "";
                shieldText.gameObject.SetActive(playerShield > 0.01f);
            }

            // HPバーの色を状態に応じて更新
            UpdateHealthBarColor(playerHealth, playerMaxHealth);

            // プレイヤー死亡状態
            if (hpBarBackground != null)
            {
                hpBarBackground.gameObject.SetActive(playerHealth > 0f);
            }
            if (hpBarFill != null)
            {
                hpBarFill.gameObject.SetActive(playerHealth > 0f);
            }
            if (hpText != null)
            {
                hpText.gameObject.SetActive(playerHealth > 0f);
            }
        }

        /// <summary>
        /// HPバーのカラーを状態に応じて更新する。
        /// </summary>
        private void UpdateHealthBarColor(float health, float maxHealth)
        {
            if (hpBarFill == null) return;

            float ratio = maxHealth > 0f ? health / maxHealth : 0f;
            Color color;

            if (ratio > 0.6f)
            {
                // 緑系 (元気)
                color = Color.Lerp(new Color(0.4f, 0.86f, 0.65f), new Color(0.3f, 0.95f, 0.5f), ratio);
            }
            else if (ratio > 0.3f)
            {
                // 黄色系 (要注意)
                color = Color.Lerp(new Color(0.9f, 0.7f, 0.1f), new Color(0.85f, 0.55f, 0.1f), (ratio - 0.3f) / 0.3f);
            }
            else
            {
                // 赤系 (危険)
                color = new Color(0.9f, 0.25f, 0.15f);
            }

            hpBarFill.color = color;
        }

        /// <summary>
        /// スコア表示を更新する。
        /// </summary>
        private void UpdateScoreDisplay()
        {
            if (_gameController == null) return;

            int playerWins = _gameController.PlayerRoundWins;
            int enemyWins = _gameController.EnemyRoundWins;

            if (scoreText != null)
            {
                scoreText.text = $"SCORE {playerWins} - {enemyWins}";
            }
            if (playerScoreText != null)
            {
                playerScoreText.text = playerWins.ToString();
            }
            if (enemyScoreText != null)
            {
                enemyScoreText.text = enemyWins.ToString();
            }
        }

        /// <summary>
        /// クレジット表示を更新する。
        /// </summary>
        private void UpdateCreditDisplay()
        {
            if (_gameController == null) return;

            int credits = _gameController.Credits;

            if (creditsText != null)
            {
                creditsText.text = $"{credits:c}";
            }
            if (creditIcon != null)
            {
                creditIcon.gameObject.SetActive(credits > 0);
            }
        }

        /// <summary>
        /// ラウンドタイマーを更新する。
        /// </summary>
        private void UpdateRoundTimer()
        {
            if (_gameController == null) return;

            float timer = _gameController.RoundTimer;
            int currentRound = _gameController.CurrentRound;

            // タイマー表示 (mm:ss形式)
            if (roundTimerText != null)
            {
                int minutes = Mathf.FloorToInt(timer / 60f);
                int seconds = Mathf.FloorToInt(timer % 60f);
                roundTimerText.text = $"{minutes:D2}:{seconds:D2}";
            }

            // ラウンド番号表示
            if (roundText != null)
            {
                roundText.text = $"第{currentRound}ラウンド";
            }
        }

        /// <summary>
        /// 敵の探测状況（アクティブな敵数）を更新する。
        /// </summary>
        private void UpdateEnemyCount()
        {
            // HuntFovRendererから探测範囲内の敵数を取得
            // 現在はGameControllerから取得（必要に応じてGameModelにメソッドを追加）
            int activeEnemyCount = 0;

            if (_gameController != null)
            {
                // GameModelから敵アクター数を取得する仕組みが望ましいが、
                // 現在のAPIでは直接取得できないため、0とする。
                // 将来的にGameModelにGetActiveEnemyCount()を追加する。
                activeEnemyCount = 0;
            }

            if (enemyCountText != null)
            {
                enemyCountText.text = $"敵: {activeEnemyCount}";
            }
            if (enemyCountBackground != null)
            {
                enemyCountBackground.gameObject.SetActive(activeEnemyCount > 0);
            }
        }

        /// <summary>
        /// エージェントスキル冷却表示を更新する。
        /// </summary>
        private void UpdateSkillCooldowns()
        {
            if (skillCooldownParent == null || _gameController == null) return;

            // スキル情報をGameModelから取得（将来的に実装）
            // 現在はプレースホルダー
            // 各スキルの冷却状態をskillCooldownParentの子として表示
        }

        /// <summary>
        /// フェーズラベルを更新する。
        /// </summary>
        private void UpdatePhaseLabel()
        {
            if (_gameController == null) return;

            string phaseLabel = _gameController.PhaseLabel;

            if (phaseText != null)
            {
                phaseText.text = phaseLabel;
            }
        }

        /// <summary>
        /// エージェント情報（名前、武器）を更新する。
        /// </summary>
        private void UpdateAgentInfo()
        {
            if (_gameController == null) return;

            string agentName = _gameController.AgentName;
            string weaponName = _gameController.WeaponName;

            if (agentNameText != null)
            {
                agentNameText.text = agentName;
            }
            if (weaponNameText != null)
            {
                weaponNameText.text = weaponName;
            }
        }

        // ==================== 統合更新（GameHUDController連携） ====================

        /// <summary>
        /// GameHUDControllerのUpdateHUD()を呼び出して、
        /// HUD状態を統合更新する。
        /// HuntHUDControllerの表示とGameHUDControllerの表示を同期する。
        /// </summary>
        public void UpdateWithGameHud()
        {
            if (_gameHudController != null)
            {
                _gameHudController.UpdateHUD();
            }
        }

        /// <summary>
        /// HuntHUDControllerのアクティブ状態を取得/設定する。
        /// Huntフェーズのときのみtrueになる。
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            private set => _isActive = value;
        }

        /// <summary>
        /// 現在のFOVモードを取得する。
        /// </summary>
        public HuntFovMode CurrentFovMode => _currentFovMode;

        /// <summary>
        /// 現在のFOV度数を取得する。
        /// </summary>
        public float CurrentFovDegrees => _currentFovDegrees;

        /// <summary>
        /// HuntFovRendererの参照を取得する。
        /// </summary>
        public HuntFovRenderer HuntFovRenderer => _huntFovRenderer;

        /// <summary>
        /// GameHUDControllerの参照を取得する。
        /// </summary>
        public GameHUDController GameHudController => _gameHudController;
    }
}
