namespace RYZECHo;

internal static class GameLayout
{
    public const int DefaultClientWidth = 1440;
    public const int DefaultClientHeight = 960;
    public const int WorldMargin = 24;
    public const int TopBarHeight = 56;
    public const int SidePanelGap = 20;
    public const int SidePanelWidth = 280;
    public const int BottomHudHeight = 132;
    public const int GridColumns = 18;
    public const int GridRows = 12;
    public const int CellSize = 56;
    public const float WorldPerspectiveScaleX = 0.84f;
    public const float WorldPerspectiveScaleY = 0.78f;
    public const float WorldPerspectiveShearX = 0.22f;
    public const float WorldPerspectiveTopInset = 10f;
    public const float HuntCameraZoom = 3.15f;
    public const float HuntVisibleWorldFractionX = 0.55f;
    public const float HuntVisibleWorldFractionY = 0.62f;
    public const float HuntCameraTargetX = 0.5f;
    public const float HuntCameraTargetY = 0.54f;
}

internal static class GameRules
{
    public const int RoundsToWin = GameSettings.RoundsToWin;
    public const int RegulationSideSwitchRound = 4;
    public const int OvertimeTriggerScore = 6;
    public const int TeamSize = GameSettings.TeamSize;
    public const int StartingCredits = GameSettings.InitialMoney;
    public const int WinRewardCredits = GameSettings.WinReward;
    public const int LossRewardCredits = GameSettings.LossReward;
    public const int KillRewardCredits = GameSettings.KillReward;
    public const int ObjectiveRewardCredits = 350;
    public const int BossKillDividendCredits = GameSettings.BossKillBonusForTeam;
    public const int BossEliminationBonusCredits = GameSettings.BossEliminatedReward;
    public const int MaxBossSelectionsPerActor = 2;
    public const int OptimalBossInvestment = GameSettings.BossInvestmentSoftCap;
    public const int BossPayoutMultiplier = GameSettings.BossPayoutMultiplier;
    public const int MaxUltPoints = GameSettings.MaxUltPoints;
    public const int InitialBuildPoints = GameSettings.InitialBuildPoints;
    public const int MaxBuildPoints = GameSettings.MaxBuildPoints;
    public const int SideSwapBuildPointRefill = GameSettings.SideSwapBuildPointRefill;
    public const float DefaultFovDegrees = GameSettings.StandardFovDegrees;
    public const float SniperFovDegrees = GameSettings.SniperFovDegrees;
    public const float SoundCueLifetimeSeconds = GameSettings.RippleDurationSeconds;
    public const float SharedVisionDurationSeconds = GameSettings.SharedVisionDurationSeconds;
    public const float IdleBreathExposeSeconds = GameSettings.IdleBreathExposeSeconds;
    public const float BreathingRippleIntervalSeconds = GameSettings.BreathingRippleIntervalSeconds;
    public const float RoundDurationSeconds = 100f;
    public const float BombPlantSeconds = 3f;
    public const float BombFuseSeconds = 35f;
    public const float BombDefuseSeconds = 8f;
    public const float BombSiteRadius = 28f;
}
