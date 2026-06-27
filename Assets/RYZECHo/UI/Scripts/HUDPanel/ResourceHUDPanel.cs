using TMPro;
using UnityEngine;

namespace RYZECHo.UI
{
    /// <summary>
    /// リソース表示(Credits, BP, ULT)を管理するHUDパネル。
    /// フェーズに応じて表示するリソースを切り替える。
    /// </summary>
    public class ResourceHUDPanel : MonoBehaviour
    {
        [Header("Resource Display")]
        [Tooltip("プレイヤークレジット表示")]
        public TextMeshProUGUI creditsText;

        [Tooltip("ビルドポイント表示テキスト (BP)")]
        public TextMeshProUGUI buildPointsText;

        [Tooltip("ウルティメットポイント表示")]
        public TextMeshProUGUI ultPointsText;

        /// <summary>
        /// リソース表示をフェーズに応じて適用。
        /// </summary>
        public void ApplyResourceState(GamePhase phase, int credits, int buildPoints, int ultPoints)
        {
            // クレジット — 常に表示
            if (creditsText != null)
            {
                creditsText.text = $"{credits:c}";
                creditsText.gameObject.SetActive(true);
            }

            // AP/BP (BuildPoints) — Constructフェーズでのみ表示
            if (phase == GamePhase.Construct)
            {
                if (buildPointsText != null)
                {
                    buildPointsText.text = $"BP: {buildPoints}";
                    buildPointsText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (buildPointsText != null)
                {
                    buildPointsText.gameObject.SetActive(false);
                }
            }

            // ULT — Huntフェーズでのみ表示
            if (phase == GamePhase.Hunt)
            {
                if (ultPointsText != null)
                {
                    ultPointsText.text = $"ULT: {ultPoints}/6";
                    ultPointsText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (ultPointsText != null)
                {
                    ultPointsText.gameObject.SetActive(false);
                }
            }
        }
    }
}
