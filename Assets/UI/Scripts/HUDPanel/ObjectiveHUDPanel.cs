using TMPro;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo.UI
{
    /// <summary>
    /// 目標サイト表示とボム設置インジケーターを管理するHUDパネル。
    /// </summary>
    public class ObjectiveHUDPanel : MonoBehaviour
    {
        [Header("Objective Display")]
        [Tooltip("目標サイト表示テキスト")]
        public TextMeshProUGUI objectiveText;

        [Tooltip("ボム設置インジケーター")]
        public GameObject bombIndicator;

        /// <summary>
        /// 目標表示をフェーズに応じて適用。
        /// </summary>
        public void ApplyObjectiveState(GamePhase phase, ObjectiveSiteId attackFocusSite,
            bool bombPlanted, ObjectiveSiteId? armedBombSite)
        {
            if (phase == GamePhase.Hunt)
            {
                // Huntフェーズ: 攻撃目標サイト (A/B)
                // ボム設置状況
                if (objectiveText != null)
                {
                    if (bombPlanted && armedBombSite.HasValue)
                    {
                        // ボム設置中 - 爆弾設置サイトを強調表示
                        objectiveText.text = $"💣 ボム設置: {(armedBombSite.Value == ObjectiveSiteId.Alpha ? "Aサイト" : "Bサイト")}";
                        objectiveText.gameObject.SetActive(true);
                        objectiveText.color = new Color(1f, 0.4f, 0.4f, 1f); // 赤系
                    }
                    else if (attackFocusSite != default)
                    {
                        // 攻撃目標サイトを表示
                        objectiveText.text = $"🎯 攻撃目標: {(attackFocusSite == ObjectiveSiteId.Alpha ? "Aサイト" : "Bサイト")}";
                        objectiveText.gameObject.SetActive(true);
                        objectiveText.color = new Color(1f, 1f, 1f, 1f); // 白
                    }
                    else
                    {
                        objectiveText.text = "";
                        objectiveText.gameObject.SetActive(false);
                    }
                }

                // ボム設置インジケーター
                if (bombIndicator != null)
                {
                    bombIndicator.SetActive(bombPlanted || armedBombSite.HasValue);
                }
            }
            else if (phase == GamePhase.Construct)
            {
                // Constructフェーズ: 構築目標サイト
                if (objectiveText != null)
                {
                    if (attackFocusSite != default)
                    {
                        objectiveText.text = $"🔨 構築目標: {(attackFocusSite == ObjectiveSiteId.Alpha ? "Aサイト" : "Bサイト")}";
                        objectiveText.gameObject.SetActive(true);
                        objectiveText.color = new Color(0.6f, 0.8f, 1f, 1f); // 青系
                    }
                    else
                    {
                        objectiveText.text = "";
                        objectiveText.gameObject.SetActive(false);
                    }
                }
                if (bombIndicator != null)
                {
                    bombIndicator.SetActive(false);
                }
            }
            else
            {
                // その他のフェーズ: 目標表示非表示
                if (objectiveText != null)
                {
                    objectiveText.text = "";
                    objectiveText.gameObject.SetActive(false);
                }
                if (bombIndicator != null)
                {
                    bombIndicator.SetActive(false);
                }
            }
        }
    }
}
