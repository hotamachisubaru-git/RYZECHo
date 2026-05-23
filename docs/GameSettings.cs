namespace Ryzecho.Core
{
    public static class GameSettings
    {
        // 視界設定
        public const float FOV_STANDARD = 100f;
        public const float FOV_SNIPER = 80f;
        public const float SOUND_MAX_DISTANCE = 25f;
        public const float RIPPLE_DURATION = 0.3f;

        // 経済設定
        public const int INITIAL_MONEY = 1000;
        public const int WIN_REWARD = 2200;
        public const int LOSS_REWARD = 1200;
        public const int KILL_REWARD = 400;
        public const int BOSS_KILL_BONUS_FOR_TEAM = 200;
        public const int BOSS_ELIMINATED_REWARD = 800;

        // ボス投資設定
        public const int BOSS_INVESTMENT_SOFT_CAP = 300;
        public const int BOSS_PAYOUT_MULTIPLIER = 2;
        
        // 試合形式
        public const int TEAMS_SIZE = 4;
        public const int ROUNDS_TO_WIN = 7;
    }
}