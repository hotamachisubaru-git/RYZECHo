using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo
{
    /// <summary>
    /// ビジュアル関連の色定義をScriptableObjectに外部化。
    /// HuntFovRenderer / HuntFovLightBinder のSerializeField色を一元管理する。
    /// </summary>
    [CreateAssetMenu(fileName = "VisualSettings", menuName = "RYZECHo/Settings/Visual Settings")]
    public sealed class VisualSettingsSO : ScriptableObject
    {
        #region Hunt FOV Colors

        [Header("Hunt FOV Colors")]
        [Tooltip("ビジョンセンターの色")]
        public Color VisionCenterColor = new Color(0.58f, 0.92f, 1f, 0.15f);

        [Tooltip("ビジョンエッジの色")]
        public Color VisionEdgeColor = new Color(0.25f, 0.85f, 1f, 0.04f);

        [Tooltip("ダークネスの色")]
        public Color DarknessColor = new Color(0.01f, 0.02f, 0.035f, 0.82f);

        [Tooltip("ライトカラー")]
        public Color LightColor = new Color(0.55f, 0.88f, 1f, 1f);

        #endregion

        #region Tile & Entity Colors

        [Header("Tile & Entity Colors")]
        [Tooltip("タイル地面の色")]
        public Color TileGroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);

        [Tooltip("プレイヤーキャラクターの色")]
        public Color ColorPlayer = new Color(0.2f, 0.6f, 1f, 1f);

        [Tooltip("敵キャラクターの色")]
        public Color ColorEnemy = new Color(1f, 0.3f, 0.3f, 1f);

        [Tooltip("ボムサイトマーカーの色")]
        public Color BombSiteColor = new Color(1f, 0.8f, 0f, 0.6f);

        [Tooltip("グリッドラインの色")]
        public Color GridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        #endregion

        #region UI Colors

        [Header("UI Colors")]
        [Tooltip("UIテキストの基本色")]
        public Color UITextColor = Color.white;

        [Tooltip("UI強調色（勝利/アクティブ）")]
        public Color UIHighlightColor = new Color(0.2f, 0.9f, 0.4f, 1f);

        [Tooltip("UI警告色（敗北/危険）")]
        public Color UIWarningColor = new Color(1f, 0.3f, 0.2f, 1f);

        #endregion

        #region Effects

        [Header("Effects")]
        [Tooltip("リップルエフェクトの色")]
        public Color RippleEffectColor = new Color(0.5f, 0.8f, 1f, 0.3f);

        [Tooltip("呼吸エフェクトの色")]
        public Color BreathingEffectColor = new Color(0.7f, 0.95f, 1f, 0.2f);

        [Tooltip("キルエフェクトの基本色")]
        public Color KillEffectColor = new Color(1f, 0.5f, 0.1f, 1f);

        #endregion

        #region Material References

        [Header("Material References")]
        [Tooltip("オーバーレイマテリアル")]
        public Material OverlayMaterial;

        [Tooltip("リップルシェーダーマテリアル")]
        public Material RippleMaterial;

        #endregion
    }
}
