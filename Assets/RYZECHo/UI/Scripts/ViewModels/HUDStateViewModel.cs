using UnityEngine;

namespace RYZECHo.UI
{
    /// <summary>
    /// HUDの表示状態データ（ViewModel）。\n    /// GameHUDControllerから各HUDパネルへの状態伝播を担う。
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

        // ==================== Factory ====================

        /// <summary>
        /// GameModelから状態を構築して返す。
        /// </summary>
        public static HUDState FromGameModel(GameModel model)
        {
            if (model == null) return new HUDState();

            return new HUDState
            {
                // HP / シールド
                PlayerHealth = model.GetPlayerHealth(),
                PlayerMaxHealth = model.GetPlayerMaxHealth(),
                PlayerShield = model.GetPlayerShield(),
                PlayerMaxShield = model.GetPlayerMaxShield(),
                // スコア / ラウンド
                PlayerRoundWins = model.GetPlayerRoundWins(),
                EnemyRoundWins = model.GetEnemyRoundWins(),
                CurrentRound = model.GetCurrentRound(),
                // フェーズ / タイマー
                Phase = model.GetPhase(),
                PhaseLabel = model.GetPhaseLabel(),
                RoundTimer = model.GetRoundTimer(),
                // リソース
                BuildPoints = model.GetBuildPoints(),
                Credits = model.GetCredits(),
                UltPoints = model.GetUltPoints(model.GetPlayer().Name),
                // ステータス
                AgentName = model.GetAgentName(),
                WeaponName = model.GetWeaponName(),
                SelectedToolName = model.GetSelectedBuildTool().ToString(),
                // 戦闘 / 結果
                CombatResult = model.GetCombatResult(),
                CombatResultTimer = model.GetCombatResultTimer(),
                IsPlayerAlive = model.IsPlayerAlive(),
                IsPlayerBoss = model.IsPlayerBoss(),
                // 状態フラグ
                IsPaused = model.IsPaused,
                ShowBriefing = model.GetShowBriefing(),
                ResultMessage = model.GetResultMessage(),
                ShowPhaseFlash = model.GetShowPhaseFlash(),
                // 目標情報
                AttackFocusSite = model.GetAttackFocusSite(),
                BombPlanted = model.GetBombPlanted(),
                ArmedBombSite = model.GetArmedBombSite(),
            };
        }
    }
}
