using System;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;
using TMPro;
using UnityEngine.UI;

namespace RYZECHo.UI
{
    /// <summary>
    /// ゲーム中のHUD表示を管理するコントローラー。
    /// 各HUDパネル(HealthBar, Score, Phase, Resource, Timer, Tool, Objective)を統合して制御。
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class GameHUDController : MonoBehaviour
    {
        // ==================== HUD Panels ====================

        [Header("HUD Panels")]
        [Tooltip("HP/シールドバーパネル")]
        public HealthBarHUDPanel healthBarPanel;

        [Tooltip("スコア表示パネル")]
        public ScoreHUDPanel scorePanel;

        [Tooltip("フェーズ表示パネル")]
        public PhaseHUDPanel phasePanel;

        [Tooltip("リソース表示パネル")]
        public ResourceHUDPanel resourcePanel;

        [Tooltip("タイマー表示パネル")]
        public TimerHUDPanel timerPanel;

        [Tooltip("ツール/武器表示パネル")]
        public ToolHUDPanel toolPanel;

        [Tooltip("目標サイト表示パネル")]
        public ObjectiveHUDPanel objectivePanel;

        // ==================== Legacy Fields (for backward compatibility) ====================

        [Header("HP Bar")]
        [Tooltip("プレイヤーHPバーの背景Image")]
        public Image hpBarBackground;

        [Tooltip("プレイヤーHPバーの充填Image")]
        public Image hpBarFill;

        [Tooltip("プレイヤーHPバーのシールドImage")]
        public Image hpBarShield;

        [Tooltip("プレイヤーHPテキスト")]
        public TextMeshProUGUI hpText;

        [Tooltip("プレイヤーシールドテキスト")]
        public TextMeshProUGUI shieldText;

        [Header("Score Display")]
        [Tooltip("スコア表示テキスト (SCORE X - Y)")]
        public TextMeshProUGUI scoreText;

        [Tooltip("プレイヤー勝利数テキスト")]
        public TextMeshProUGUI playerScoreText;

        [Tooltip("敵勝利数テキスト")]
        public TextMeshProUGUI enemyScoreText;

        [Header("Turn / Round Display")]
        [Tooltip("ラウンド番号テキスト (第Xラウンド)")]
        public TextMeshProUGUI roundText;

        [Header("Phase Flash")]
        [Tooltip("フェーズ切り替え時のフラッシュエフェクト")]
        public Image phaseFlashOverlay;

        [Header("Status Display")]
        [Tooltip("プレイヤーエージェント名")]
        public TextMeshProUGUI agentNameText;

        [Tooltip("プレイヤー装備武器名")]
        public TextMeshProUGUI weaponNameText;

        [Header("Combat Result Display")]
        [Tooltip("戦闘結果テキスト (ダメージ、クリティカル等)")]
        public TextMeshProUGUI combatResultText;

        [Tooltip("戦闘結果テキストのフェードアウトタイマー")]
        public float combatResultFadeTime = 2.0f;

        [Header("Kill Feed")]
        [Tooltip("キルフィードの親Transform")]
        public Transform killFeedParent;

        [Header("Enemy Unit Display")]
        [Tooltip("敵ユニット表示の親Transform")]
        public Transform enemyUnitParent;

        [Header("Bottom HUD Panel")]
        [Tooltip("下部HUDパネル")]
        public GameObject bottomHudPanel;

        [Header("Activity Feed")]
        [Tooltip("アクティビティフィードの親Transform")]
        public Transform activityFeedParent;

        // ==================== Internal State ====================

        // 参照キャッシュ
        private Canvas _canvas;
        private GameModel _gameModel;
        private float _combatResultTimer;
        private string _currentCombatResult = "";
        private float _phaseFlashTimer;

        // 敵ユニット表示用キャッシュ
        private readonly Dictionary<string, EnemyUnitDisplay> _enemyDisplays = new();

        // アクションイベント（外部からHUD状態を通知するためのイベント）
        public static event System.Action<HUDState> OnHUDStateChanged;

        /// <summary>
        /// HUDの表示状態データ。
        /// </summary>
        public struct HUDState
        {
            // HP / シールド
            public float PlayerHealth;
            public float PlayerMaxHealth;
            public float PlayerShield;
            public float PlayerMaxShield;
            // スコア / ラウンド
            public int PlayerRoundWins;
            public int EnemyRoundWins;
            public int CurrentRound;
            // フェーズ / タイマー
            public GamePhase Phase;
            public string PhaseLabel;
            public float RoundTimer;
            // リソース (AP/クレジット/BP)
            public int BuildPoints;
            public int Credits;
            public int UltPoints;
            // ステータス
            public string AgentName;
            public string WeaponName;
            public string SelectedToolName;
            // 戦闘 / 結果
            public string CombatResult;
            public float CombatResultTimer;
            public bool IsPlayerAlive;
            public bool IsPlayerBoss;
            // 状態フラグ
            public bool IsPaused;
            public bool ShowBriefing;
            public string ResultMessage;
            public bool ShowPhaseFlash;
            // 目標情報
            public ObjectiveSiteId AttackFocusSite;
            public bool BombPlanted;
            public ObjectiveSiteId? ArmedBombSite;
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        private void Start()
        {
            // 敵ユニット表示オブジェクトの初期化
            InitializeEnemyDisplays();
            // アクティビティフィードの初期化
            InitializeActivityFeed();
        }

        private void Update()
        {
            // 戦闘結果テキストの更新
            if (_combatResultTimer > 0f)
            {
                _combatResultTimer -= Time.deltaTime;
                float alpha = Mathf.Clamp01(_combatResultTimer / combatResultFadeTime);
                combatResultText.color = new Color(combatResultText.color.r, combatResultText.color.g, combatResultText.color.b, alpha * 255f);
                if (_combatResultTimer <= 0f)
                {
                    combatResultText.text = "";
                }
            }
        }

        /// <summary>
        /// ゲームモデルをセット（外部から呼び出し）。
        /// </summary>
        public void SetGameModel(GameModel model)
        {
            _gameModel = model;
        }

        /// <summary>
        /// ゲームモデルの現在状態からHUDを更新。
        /// 各HUDパネルに状態を委譲する。
        /// </summary>
        public void UpdateHUD()
        {
            if (_gameModel == null) return;

            var state = new HUDState
            {
                // HP / シールド
                PlayerHealth = _gameModel.GetPlayerHealth(),
                PlayerMaxHealth = _gameModel.GetPlayerMaxHealth(),
                PlayerShield = _gameModel.GetPlayerShield(),
                PlayerMaxShield = _gameModel.GetPlayerMaxShield(),
                // スコア / ラウンド
                PlayerRoundWins = _gameModel.GetPlayerRoundWins(),
                EnemyRoundWins = _gameModel.GetEnemyRoundWins(),
                CurrentRound = _gameModel.GetCurrentRound(),
                // フェーズ / タイマー
                Phase = _gameModel.GetPhase(),
                PhaseLabel = _gameModel.GetPhaseLabel(),
                RoundTimer = _gameModel.GetRoundTimer(),
                // リソース (AP/クレジット/BP)
                BuildPoints = _gameModel.GetBuildPoints(),
                Credits = _gameModel.GetCredits(),
                UltPoints = _gameModel.GetUltPoints(),
                // ステータス
                AgentName = _gameModel.GetAgentName(),
                WeaponName = _gameModel.GetWeaponName(),
                SelectedToolName = _gameModel.GetSelectedBuildTool().ToString(),
                // 戦闘 / 結果
                CombatResult = _gameModel.GetCombatResult(),
                CombatResultTimer = _gameModel.GetCombatResultTimer(),
                IsPlayerAlive = _gameModel.IsPlayerAlive(),
                IsPlayerBoss = _gameModel.IsPlayerBoss(),
                // 状態フラグ
                IsPaused = _gameModel.IsPaused,
                ShowBriefing = _gameModel.GetShowBriefing(),
                ResultMessage = _gameModel.GetResultMessage(),
                ShowPhaseFlash = _gameModel.GetShowPhaseFlash(),
                // 目標情報
                AttackFocusSite = _gameModel.GetAttackFocusSite(),
                BombPlanted = _gameModel.GetBombPlanted(),
                ArmedBombSite = _gameModel.GetArmedBombSite(),
            };

            ApplyState(state);
            OnHUDStateChanged?.Invoke(state);
        }

        /// <summary>
        /// HUD状態を各パネルに適用。
        /// </summary>
        private void ApplyState(HUDState state)
        {
            // 1. HP/シールドバー
            if (healthBarPanel != null)
            {
                healthBarPanel.ApplyHealthState(
                    state.PlayerHealth, state.PlayerMaxHealth,
                    state.PlayerShield, state.PlayerMaxShield,
                    state.IsPlayerAlive);
            }

            // 2. スコア表示
            if (scorePanel != null)
            {
                scorePanel.ApplyScoreState(state.PlayerRoundWins, state.EnemyRoundWins);
            }

            // 3. フェーズ表示
            if (phasePanel != null)
            {
                phasePanel.ApplyPhaseState(state.PhaseLabel, state.ShowPhaseFlash);
            }

            // 4. リソース表示
            if (resourcePanel != null)
            {
                resourcePanel.ApplyResourceState(
                    state.Phase, state.Credits,
                    state.BuildPoints, state.UltPoints);
            }

            // 5. タイマー表示
            if (timerPanel != null)
            {
                timerPanel.ApplyTimerState(state.Phase, state.RoundTimer);
            }

            // 6. ツール/武器表示
            if (toolPanel != null)
            {
                toolPanel.ApplyToolState(
                    state.Phase, state.AgentName, state.WeaponName,
                    state.SelectedToolName, state.IsPlayerAlive);
            }

            // 7. 目標表示
            if (objectivePanel != null)
            {
                objectivePanel.ApplyObjectiveState(
                    state.Phase, state.AttackFocusSite,
                    state.BombPlanted, state.ArmedBombSite);
            }

            // 戦闘結果表示
            if (!string.IsNullOrEmpty(state.CombatResult) && _combatResultTimer <= 0f)
            {
                _currentCombatResult = state.CombatResult;
                _combatResultTimer = state.CombatResultTimer;
                combatResultText.text = state.CombatResult;
                combatResultText.color = new Color(255f, 255f, 255f, 255f);
            }

            // 下部HUDの表示/非表示
            if (bottomHudPanel != null)
            {
                bottomHudPanel.SetActive(state.Phase == GamePhase.Hunt && state.IsPlayerAlive);
            }
        }

        // ==================== Enemy Unit Display ====================

        /// <summary>
        /// 敵ユニット表示を初期化。
        /// </summary>
        private void InitializeEnemyDisplays()
        {
            if (enemyUnitParent == null) return;
        }

        /// <summary>
        /// 敵ユニット表示を更新。
        /// </summary>
        public void UpdateEnemyDisplays(Actor[] enemies, Actor[] allies)
        {
            if (enemyUnitParent == null) return;

            var allActors = new List<(Actor actor, bool isEnemy)>();
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive)
                    allActors.Add((enemy, true));
            }
            foreach (var ally in allies)
            {
                if (ally.IsAlive)
                    allActors.Add((ally, false));
            }

            foreach (var kvp in _enemyDisplays)
            {
                bool found = false;
                foreach (var (actor, _) in allActors)
                {
                    if (kvp.Key == actor.Name)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }

            foreach (var (actor, isEnemy) in allActors)
            {
                if (!_enemyDisplays.ContainsKey(actor.Name))
                {
                    var prefabPath = isEnemy
                        ? "Assets/RYZECHo/UI/Prefabs/EnemyUnitDisplay.prefab"
                        : "Assets/RYZECHo/UI/Prefabs/AllyUnitDisplay.prefab";

                    var go = new GameObject($"EnemyUnit_{actor.Name}");
                    go.transform.SetParent(enemyUnitParent, false);
                    var display = go.AddComponent<EnemyUnitDisplay>();
                    display.Initialize(actor, isEnemy);
                    _enemyDisplays[actor.Name] = display;
                }
                else
                {
                    _enemyDisplays[actor.Name].UpdateActor(actor, isEnemy);
                }
            }
        }

        // ==================== Activity Feed ====================

        /// <summary>
        /// アクティビティフィードを初期化。
        /// </summary>
        private void InitializeActivityFeed()
        {
            if (activityFeedParent == null) return;
        }

        /// <summary>
        /// アクティビティフィードを更新。
        /// </summary>
        public void UpdateActivityFeed(string[] messages)
        {
            if (activityFeedParent == null || messages == null) return;

            var existingChildren = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in activityFeedParent)
            {
                existingChildren.Add(child);
            }

            for (int i = 0; i < existingChildren.Count; i++)
            {
                if (i >= messages.Length)
                {
                    Destroy(existingChildren[i].gameObject);
                }
            }
        }

        /// <summary>
        /// ダメージフラッシュを適用。
        /// </summary>
        public void ApplyDamageFlash(Color color, float duration)
        {
            if (phaseFlashOverlay != null)
            {
                phaseFlashOverlay.color = color;
                _phaseFlashTimer = duration;
            }
        }

        /// <summary>
        /// クチコミ（キルフィード）エントリを追加。
        /// </summary>
        public void AddKillFeedEntry(string killerName, string victimName, bool isPlayerKiller)
        {
            if (killFeedParent == null) return;
        }
    }
}
