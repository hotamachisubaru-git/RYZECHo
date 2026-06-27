using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace RYZECHo.UI
{
    /// <summary>
    /// GameModelへのアクセスを提供するアダプター。
    /// GameModelはinternalクラスのため、リフレクションまたは公開API経由でアクセス。
    /// </summary>
    public static class GameModelAdapter
    {
        private static GameModel _model;

        public static GameModel Model
        {
            get => _model;
            set => _model = value;
        }

        // ==================== プレイヤー情報 ====================

        public static float GetPlayerHealth()
        {
            return _model?.GetPlayerHealth() ?? 100f;
        }

        public static float GetPlayerMaxHealth()
        {
            return _model?.GetPlayerMaxHealth() ?? 100f;
        }

        public static float GetPlayerShield()
        {
            return _model?.GetPlayerShield() ?? 60f;
        }

        public static float GetPlayerMaxShield()
        {
            return _model?.GetPlayerMaxShield() ?? 60f;
        }

        public static bool IsPlayerAlive()
        {
            return _model?.IsPlayerAlive() ?? true;
        }

        public static bool IsPlayerBoss()
        {
            return _model?.IsPlayerBoss() ?? false;
        }

        public static string GetAgentName()
        {
            return _model?.GetAgentName() ?? "ヴェール";
        }

        public static string GetWeaponName()
        {
            return _model?.GetWeaponName() ?? "Giant";
        }

        public static int GetUltPoints()
        {
            return _model != null && _model.GetPlayer() != null
                ? _model.GetUltPoints(_model.GetPlayer().Name)
                : 0;
        }

        // ==================== ゲーム状態 ====================

        public static int GetPlayerRoundWins()
        {
            return _model?.GetPlayerRoundWins() ?? 0;
        }

        public static int GetEnemyRoundWins()
        {
            return _model?.GetEnemyRoundWins() ?? 0;
        }

        public static int GetCurrentRound()
        {
            return _model?.GetCurrentRound() ?? 1;
        }

        public static GamePhase GetPhase()
        {
            return _model?.GetPhase() ?? GamePhase.Construct;
        }

        public static string GetPhaseLabel()
        {
            return _model?.GetPhaseLabel() ?? "HUNT";
        }

        public static int GetCredits()
        {
            return _model?.GetCredits() ?? 1000;
        }

        public static int GetBuildPoints()
        {
            return _model?.GetBuildPoints() ?? GameRules.InitialBuildPoints;
        }

        public static float GetRoundTimer()
        {
            return _model?.GetRoundTimer() ?? GameRules.RoundDurationSeconds;
        }

        public static bool IsPaused
        {
            get => _model?.IsPaused ?? false;
        }

        public static bool GetShowBriefing()
        {
            return _model?.GetShowBriefing() ?? false;
        }

        public static string GetResultMessage()
        {
            return _model?.GetResultMessage() ?? "";
        }

        public static string GetCombatResult()
        {
            return _model?.GetCombatResult() ?? "";
        }

        public static float GetCombatResultTimer()
        {
            return _model?.GetCombatResultTimer() ?? 2f;
        }

        public static bool GetShowPhaseFlash()
        {
            return _model?.GetShowPhaseFlash() ?? false;
        }

        // ==================== アクター情報 ====================

        public static Actor GetPlayer()
        {
            return _model?.GetPlayer() ?? new Actor();
        }

        public static Actor[] GetAllies()
        {
            return _model?.GetAllies() ?? new Actor[0];
        }

        public static Actor[] GetEnemies()
        {
            return _model?.GetEnemies() ?? new Actor[0];
        }

        public static string[] GetActivityFeedMessages()
        {
            return _model?.GetActivityFeedMessages() ?? new string[0];
        }

        // ==================== WeaponStats ====================

        public static string GetWeaponLabel(WeaponType weapon)
        {
            return _model?.GetWeaponLabel(weapon) ?? weapon.ToString();
        }

        public static string GetWeaponShortLabel(WeaponType weapon)
        {
            return _model?.GetWeaponShortLabel(weapon) ?? weapon.ToString();
        }

        // ==================== AgentKind ====================

        public static string GetAgentLabel(AgentKind agent)
        {
            return _model?.GetAgentLabel(agent) ?? agent.ToString();
        }

        // ==================== 選択ツール ====================

        public static BuildToolKind GetSelectedBuildTool()
        {
            return _model != null ? _model.GetSelectedBuildTool() : BuildToolKind.BlastDoor;
        }

        public static int GetCurrentPhase()
        {
            return _model?.GetCurrentPhase() ?? 0;
        }

        // ==================== 目標情報 ====================

        public static ObjectiveSiteId GetAttackFocusSite()
        {
            return _model?.GetAttackFocusSite() ?? ObjectiveSiteId.Alpha;
        }

        public static bool GetBombPlanted()
        {
            return _model?.GetBombPlanted() ?? false;
        }

        public static ObjectiveSiteId? GetArmedBombSite()
        {
            return _model?.GetArmedBombSite();
        }
    }
}
