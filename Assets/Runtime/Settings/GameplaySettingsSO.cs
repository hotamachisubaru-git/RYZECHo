using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo
{
    /// <summary>
    /// ゲームバランス関連の定数をScriptableObjectに外部化。
    /// インスペクタからゲームバランス調整を可能にする。
    /// </summary>
    [CreateAssetMenu(fileName = "GameplaySettings", menuName = "RYZECHo/Settings/Gameplay Settings")]
    public sealed class GameplaySettingsSO : ScriptableObject
    {
        #region FOV Settings

        [Header("FOV Settings")]
        [Tooltip("標準FOV（度）")]
        public float StandardFovDegrees = 100f;

        [Tooltip("ワイドFOV（度）")]
        public float WideFovDegrees = 120f;

        [Tooltip("スナイパーFOV（度）")]
        public float SniperFovDegrees = 80f;

        #endregion

        #region Audio Ripple Settings

        [Header("Audio Ripple Settings")]
        [Tooltip("サウンドリップルの最大到達距離")]
        public float SoundMaxDistance = 25f;

        [Tooltip("リップルの持続時間（秒）")]
        public float RippleDurationSeconds = 0.3f;

        [Tooltip("共有ビジョンの持続時間（秒）")]
        public float SharedVisionDurationSeconds = 1.4f;

        [Tooltip("アイドル時の呼吸露出までの時間（秒）")]
        public float IdleBreathExposeSeconds = 10f;

        [Tooltip("呼吸リップルの間隔（秒）")]
        public float BreathingRippleIntervalSeconds = 5.0f;

        [Tooltip("呼吸リップルのフェードアウト持続時間（秒）")]
        public float BreathingRippleFadeOutSeconds = 3.0f;

        #endregion

        #region Economy Settings

        [Header("Economy Settings")]
        [Tooltip("開始資金")]
        public int InitialMoney = 1000;

        [Tooltip("勝利報酬")]
        public int WinReward = 2200;

        [Tooltip("敗退報酬")]
        public int LossReward = 1200;

        [Tooltip("キル報酬")]
        public int KillReward = 400;

        [Tooltip("チームボスキルボーナス")]
        public int BossKillBonusForTeam = 200;

        [Tooltip("ボス排除報酬")]
        public int BossEliminatedReward = 800;

        #endregion

        #region Boss & Ultimate Settings

        [Header("Boss & Ultimate Settings")]
        [Tooltip("ボス投資ソフトキャップ")]
        public int BossInvestmentSoftCap = 300;

        [Tooltip("ボスペイアウト倍率")]
        public int BossPayoutMultiplier = 2;

        [Tooltip("最大ウルティメットポイント")]
        public int MaxUltPoints = 6;

        #endregion

        #region Team & Round Settings

        [Header("Team & Round Settings")]
        [Tooltip("チーム人数")]
        public int TeamSize = 4;

        [Tooltip("勝利に必要なラウンド数")]
        public int RoundsToWin = 7;

        [Tooltip("開始ビルドポイント")]
        public int InitialBuildPoints = 12;

        [Tooltip("最大ビルドポイント")]
        public int MaxBuildPoints = 12;

        [Tooltip("サイド交換時のビルドポイント回復量")]
        public int SideSwapBuildPointRefill = 12;

        #endregion
    }
}
