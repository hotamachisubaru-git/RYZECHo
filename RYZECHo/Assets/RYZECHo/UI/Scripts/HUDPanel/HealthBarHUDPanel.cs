using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;
using TMPro;

namespace RYZECHo.UI
{
    /// <summary>
    /// HP/シールドバーを表示するHUDパネル。
    /// </summary>
    public class HealthBarHUDPanel : MonoBehaviour
    {
        [Header("HP Bar")]
        [Tooltip("プレイヤーHPバーの背景Image")]
        public Image hpBarBackground;

        [Tooltip("プレイヤーHPバーの充填Image")]
        public Image hpBarFill;

        [Tooltip("プレイヤーHPバーのシールドImage")]
        public Image hpBarShield;

        [Tooltip("プレイヤーHPテキスト")]
        public TextMeshProUGUI hpText;

        [Tooltip("プレイヤーシールドテキスト")]
        public TextMeshProUGUI shieldText;

        private void Awake()
        {
            if (hpBarFill != null)
            {
                hpBarFill.color = Color.white;
            }
        }

        /// <summary>
        /// HP/シールド状態を適用。
        /// </summary>
        public void ApplyHealthState(float playerHealth, float playerMaxHealth,
            float playerShield, float playerMaxShield, bool isPlayerAlive)
        {
            // HPバー更新
            if (hpBarFill != null)
            {
                hpBarFill.fillAmount = playerMaxHealth > 0f
                    ? Mathf.Clamp01(playerHealth / playerMaxHealth)
                    : 0f;
                hpBarFill.gameObject.SetActive(isPlayerAlive);
            }

            // シールドバー更新
            if (hpBarShield != null)
            {
                hpBarShield.fillAmount = playerMaxShield > 0f
                    ? Mathf.Clamp01(playerShield / playerMaxShield)
                    : 0f;
                hpBarShield.gameObject.SetActive(playerShield > 0.01f && isPlayerAlive);
            }

            // HPテキスト更新
            if (hpText != null)
            {
                hpText.text = isPlayerAlive
                    ? $"{Mathf.CeilToInt(playerHealth)} / {Mathf.CeilToInt(playerMaxHealth)}"
                    : "";
                hpText.gameObject.SetActive(isPlayerAlive);
            }

            // シールドテキスト更新
            if (shieldText != null)
            {
                shieldText.text = playerShield > 0.01f ? $"シールド: {Mathf.CeilToInt(playerShield)}" : "";
                shieldText.gameObject.SetActive(playerShield > 0.01f && isPlayerAlive);
            }

            // 色を状態に応じて更新
            UpdateHealthBarColor(playerHealth, playerMaxHealth);

            // プレイヤー死亡状態
            if (hpBarBackground != null)
            {
                hpBarBackground.gameObject.SetActive(isPlayerAlive);
            }
        }

        /// <summary>
        /// HPバーのカラーを状態に応じて更新。
        /// </summary>
        private void UpdateHealthBarColor(float health, float maxHealth)
        {
            if (hpBarFill == null) return;

            float ratio = maxHealth > 0f ? health / maxHealth : 0f;
            Color color;

            if (ratio > 0.6f)
            {
                // 緑系 (元気)
                color = Color.Lerp(new Color(0.4f, 0.86f, 0.65f), new Color(0.3f, 0.95f, 0.5f), ratio);
            }
            else if (ratio > 0.3f)
            {
                // 黄色系 (要注意)
                color = Color.Lerp(new Color(0.9f, 0.7f, 0.1f), new Color(0.85f, 0.55f, 0.1f), (ratio - 0.3f) / 0.3f);
            }
            else
            {
                // 赤系 (危険)
                color = new Color(0.9f, 0.25f, 0.15f);
            }

            hpBarFill.color = color;
        }
    }
}
