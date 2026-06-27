using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RYZECHo.UI
{
    /// <summary>
    /// フェーズ表示とフェーズフラッシュエフェクトを管理するHUDパネル。
    /// </summary>
    public class PhaseHUDPanel : MonoBehaviour
    {
        [Header("Phase Display")]
        [Tooltip("フェーズ表示テキスト (HUNT, CONSTRUCT, BET等)")]
        public TextMeshProUGUI phaseText;

        [Header("Phase Flash")]
        [Tooltip("フェーズ切り替え時のフラッシュエフェクト")]
        public Image phaseFlashOverlay;

        private float _phaseFlashTimer;

        private void Update()
        {
            // フェーズフラッシュの更新
            if (_phaseFlashTimer > 0f)
            {
                _phaseFlashTimer -= Time.deltaTime;
                phaseFlashOverlay.gameObject.SetActive(_phaseFlashTimer > 0f);
                if (_phaseFlashTimer <= 0f)
                {
                    phaseFlashOverlay.color = Color.clear;
                }
            }
        }

        /// <summary>
        /// フェーズ表示とフラッシュを適用。
        /// </summary>
        public void ApplyPhaseState(string phaseLabel, bool showPhaseFlash)
        {
            // フェーズ表示更新
            if (phaseText != null)
            {
                phaseText.text = phaseLabel;
            }

            // フェーズフラッシュ
            if (showPhaseFlash && _phaseFlashTimer <= 0f)
            {
                phaseFlashOverlay.color = new Color(1f, 0.9f, 0.7f, 0.6f);
                _phaseFlashTimer = 0.9f;
            }
        }
    }
}
