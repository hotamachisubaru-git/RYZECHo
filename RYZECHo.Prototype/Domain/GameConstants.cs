namespace RYZECHo.Prototype;

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
    public const int RoundsToWin = 7;
    public const int RegulationSideSwitchRound = 4;
    public const int OvertimeTriggerScore = 6;
    public const int TeamSize = 4;
    public const int StartingCredits = 1000;
    public const int WinRewardCredits = 2200;
    public const int LossRewardCredits = 1200;
    public const int KillRewardCredits = 400;
    public const int ObjectiveRewardCredits = 350;
    public const int BossKillDividendCredits = 200;
    public const int BossEliminationBonusCredits = 800;
    public const int MaxBossSelectionsPerActor = 2;
    public const int OptimalBossInvestment = 300;
    public const int MaxUltPoints = 7;
    public const float DefaultFovDegrees = 120f;
    public const float SniperFovDegrees = 100f;
    public const float SoundCueLifetimeSeconds = 0.3f;
    public const float SharedVisionDurationSeconds = 1.4f;
    public const float RoundDurationSeconds = 50f;
    public const float BombPlantSeconds = 3f;
    public const float BombFuseSeconds = 35f;
    public const float BombDefuseSeconds = 8f;
    public const float BombSiteRadius = 28f;
}
