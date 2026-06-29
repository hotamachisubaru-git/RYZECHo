namespace RYZECHo.TacticalProto
{
    /// <summary>
    /// タクティカルシューター向けゲームルール定数。
    /// 既存のGameRulesから流用した値を再定義。
    /// </summary>
    public static class GameConstants
    {
        // --- ラウンド / マッチ ---
        public const int RoundsToWin = 7;
        public const int RegulationSideSwitchRound = 4;
        public const int OvertimeTriggerScore = 6;
        public const int TeamSize = 4;

        // --- クレジット / 経済 ---
        public const int StartingCredits = 1000;
        public const int WinRewardCredits = 2200;
        public const int LossRewardCredits = 1200;
        public const int KillRewardCredits = 400;
        public const int ObjectiveRewardCredits = 350;
        public const int BossKillDividendCredits = 200;
        public const int BossEliminationBonusCredits = 800;
        public const int AgentSkillPurchaseCost = 400;

        // --- ビルドポイント ---
        public const int InitialBuildPoints = 12;
        public const int MaxBuildPoints = 12;
        public const int SideSwapBuildPointRefill = 12;

        // --- キャラクター ---
        public const int MaxUltPoints = 6;
        public const float DefaultFovDegrees = 100f;
        public const float SniperFovDegrees = 80f;

        // --- タイミング ---
        public const float SoundCueLifetimeSeconds = 0.3f;
        public const float SharedVisionDurationSeconds = 1.4f;
        public const float IdleBreathExposeSeconds = 10f;
        public const float BreathingRippleIntervalSeconds = 5.0f;
        public const float BreathingRippleFadeOutSeconds = 3.0f;
        public const float RoundDurationSeconds = 100f;

        // --- ボム ---
        public const float BombPlantSeconds = 3f;
        public const float BombFuseSeconds = 35f;
        public const float BombDefuseSeconds = 8f;
        public const float BombSiteRadius = 28f;

        // --- マップ ---
        public const int GridColumns = 18;
        public const int GridRows = 12;
        public const int CellSize = 56;

        // --- 武器バランス ---
        public const float MaxShieldRegenDelay = 5.0f;
        public const float ShieldRegenRate = 1.0f;
        public const float MinHealthFractionForShield = 0.3f;
    }
}
