namespace RYZECHo
{
    /// <summary>
    /// ゲームバランス定数へのアクセスを提供する。
    /// 値は GameplaySettingsSO のデフォルト値と一致。
    /// </summary>
    internal static class GameSettings
    {
        #region FOV

        public const float StandardFovDegrees = 100f;
        public const float WideFovDegrees = 120f;
        public const float SniperFovDegrees = 80f;

        #endregion

        #region Audio Ripple

        public const float SoundMaxDistance = 25f;
        public const float RippleDurationSeconds = 0.3f;
        public const float SharedVisionDurationSeconds = 1.4f;
        public const float IdleBreathExposeSeconds = 10f;
        public const float BreathingRippleIntervalSeconds = 5.0f;
        public const float BreathingRippleFadeOutSeconds = 3.0f;

        #endregion

        #region Economy

        public const int InitialMoney = 1000;
        public const int WinReward = 2200;
        public const int LossReward = 1200;
        public const int KillReward = 400;
        public const int BossKillBonusForTeam = 200;
        public const int BossEliminatedReward = 800;

        #endregion

        #region Boss & Ultimate

        public const int BossInvestmentSoftCap = 300;
        public const int BossPayoutMultiplier = 2;
        public const int MaxUltPoints = 6;

        #endregion

        #region Team & Round

        public const int TeamSize = 4;
        public const int RoundsToWin = 7;
        public const int InitialBuildPoints = 12;
        public const int MaxBuildPoints = 12;
        public const int SideSwapBuildPointRefill = 12;

        #endregion
    }
}
