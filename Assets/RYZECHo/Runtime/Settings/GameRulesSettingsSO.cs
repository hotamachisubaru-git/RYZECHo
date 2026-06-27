using UnityEngine;

namespace RYZECHo
{
    /// <summary>
    /// ゲームルール関連の定数をScriptableObjectに外部化。
    /// ラウンド時間、ボム時間、勝条件などのルール値をインスペクタから調整可能にする。
    /// </summary>
    [CreateAssetMenu(fileName = "GameRulesSettings", menuName = "RYZECHo/Settings/Game Rules Settings")]
    public sealed class GameRulesSettingsSO : ScriptableObject
    {
        #region Round & Win Rules

        [Header("Round & Win Rules")]
        [Tooltip("勝利に必要なラウンド数")]
        public int RoundsToWin = 7;

        [Tooltip("正規サイド切り替えラウンド")]
        public int RegulationSideSwitchRound = 4;

        [Tooltip("オーバータイム発動スコア")]
        public int OvertimeTriggerScore = 6;

        [Tooltip("ラウンド時間（秒）")]
        public float RoundDurationSeconds = 100f;

        #endregion

        #region Team & Credits

        [Header("Team & Credits")]
        [Tooltip("チーム人数")]
        public int TeamSize = 4;

        [Tooltip("開始クレジット")]
        public int StartingCredits = 1000;

        [Tooltip("勝利報酬クレジット")]
        public int WinRewardCredits = 2200;

        [Tooltip("敗退報酬クレジット")]
        public int LossRewardCredits = 1200;

        [Tooltip("キル報酬クレジット")]
        public int KillRewardCredits = 400;

        [Tooltip("オブジェクティブ報酬クレジット")]
        public int ObjectiveRewardCredits = 350;

        #endregion

        #region Boss Rules

        [Header("Boss Rules")]
        [Tooltip("チームボスキルボーナス")]
        public int BossKillDividendCredits = 200;

        [Tooltip("ボス排除ボーナスクレジット")]
        public int BossEliminationBonusCredits = 800;

        [Tooltip("アクターあたりの最大ボス選択数")]
        public int MaxBossSelectionsPerActor = 2;

        [Tooltip("最適ボス投資額")]
        public int OptimalBossInvestment = 300;

        [Tooltip("ボスペイアウト倍率")]
        public int BossPayoutMultiplier = 2;

        #endregion

        #region Ultimate & Build

        [Header("Ultimate & Build")]
        [Tooltip("最大ウルティメットポイント")]
        public int MaxUltPoints = 6;

        [Tooltip("開始ビルドポイント")]
        public int InitialBuildPoints = 12;

        [Tooltip("最大ビルドポイント")]
        public int MaxBuildPoints = 12;

        [Tooltip("サイド交換時のビルドポイント回復量")]
        public int SideSwapBuildPointRefill = 12;

        #endregion

        #region FOV & Audio Rules

        [Header("FOV & Audio Rules")]
        [Tooltip("標準FOV（度）")]
        public float DefaultFovDegrees = 100f;

        [Tooltip("スナイパーFOV（度）")]
        public float SniperFovDegrees = 80f;

        [Tooltip("サウンドキューの寿命（秒）")]
        public float SoundCueLifetimeSeconds = 0.3f;

        [Tooltip("共有ビジョンの持続時間（秒）")]
        public float SharedVisionDurationSeconds = 1.4f;

        [Tooltip("アイドル時の呼吸露出までの時間（秒）")]
        public float IdleBreathExposeSeconds = 10f;

        [Tooltip("呼吸リップルの間隔（秒）")]
        public float BreathingRippleIntervalSeconds = 5.0f;

        [Tooltip("呼吸リップルのフェードアウト持続時間（秒）")]
        public float BreathingRippleFadeOutSeconds = 3.0f;

        #endregion

        #region Bomb Rules

        [Header("Bomb Rules")]
        [Tooltip("ボム設置時間（秒）")]
        public float BombPlantSeconds = 3f;

        [Tooltip("ボム起爆時間（秒）")]
        public float BombFuseSeconds = 35f;

        [Tooltip("ボム解除時間（秒）")]
        public float BombDefuseSeconds = 8f;

        [Tooltip("ボムサイト半径")]
        public float BombSiteRadius = 28f;

        #endregion
    }
}
