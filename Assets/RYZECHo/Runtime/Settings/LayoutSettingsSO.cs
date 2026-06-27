using UnityEngine;

namespace RYZECHo
{
    /// <summary>
    /// レイアウト・カメラ関連の定数をScriptableObjectに外部化。
    /// レベルデザイン時にインスペクタから即座に調整可能。
    /// </summary>
    [CreateAssetMenu(fileName = "LayoutSettings", menuName = "RYZECHo/Settings/Layout Settings")]
    public sealed class LayoutSettingsSO : ScriptableObject
    {
        #region Client Layout

        [Header("Client Layout")]
        [Tooltip("デフォルトクライアント幅（ピクセル）")]
        public int DefaultClientWidth = 1440;

        [Tooltip("デフォルトクライアント高さ（ピクセル）")]
        public int DefaultClientHeight = 960;

        #endregion

        #region World Layout

        [Header("World Layout")]
        [Tooltip("ワールドマージン")]
        public int WorldMargin = 24;

        [Tooltip("トップバー高さ")]
        public int TopBarHeight = 56;

        [Tooltip("サイドパネルギャップ")]
        public int SidePanelGap = 20;

        [Tooltip("サイドパネル幅")]
        public int SidePanelWidth = 280;

        [Tooltip("ボトムHUD高さ")]
        public int BottomHudHeight = 132;

        [Tooltip("グリッド列数")]
        public int GridColumns = 18;

        [Tooltip("グリッド行数")]
        public int GridRows = 12;

        [Tooltip("セルサイズ")]
        public int CellSize = 56;

        [Header("World Perspective")]
        [Tooltip("ワールド透視スケールX")]
        public float WorldPerspectiveScaleX = 0.84f;

        [Tooltip("ワールド透視スケールY")]
        public float WorldPerspectiveScaleY = 0.78f;

        [Tooltip("ワールド透視シアーX")]
        public float WorldPerspectiveShearX = 0.22f;

        [Tooltip("ワールド透視トップインセット")]
        public float WorldPerspectiveTopInset = 10f;

        #endregion

        #region Hunt Camera

        [Header("Hunt Camera")]
        [Tooltip("ハンティングカメラズーム")]
        public float HuntCameraZoom = 3.15f;

        [Tooltip("ハンティングカメラ可視範囲X")]
        public float HuntVisibleWorldFractionX = 0.55f;

        [Tooltip("ハンティングカメラ可視範囲Y")]
        public float HuntVisibleWorldFractionY = 0.62f;

        [Tooltip("ハンティングカメラターゲットX")]
        public float HuntCameraTargetX = 0.5f;

        [Tooltip("ハンティングカメラターゲットY")]
        public float HuntCameraTargetY = 0.54f;

        #endregion
    }
}
