using TMPro;
using UnityEngine;

namespace RYZECHo.UI
{
    /// <summary>
    /// ツール/武器表示を管理するHUDパネル。
    /// フェーズに応じてエージェント名、武器名、選択ツールを切り替える。
    /// </summary>
    public class ToolHUDPanel : MonoBehaviour
    {
        [Header("Status Display")]
        [Tooltip("プレイヤーエージェント名")]
        public TextMeshProUGUI agentNameText;

        [Tooltip("プレイヤー装備武器名")]
        public TextMeshProUGUI weaponNameText;

        /// <summary>
        /// ツール/武器表示をフェーズに応じて適用。
        /// </summary>
        public void ApplyToolState(GamePhase phase, string agentName, string weaponName,
            string selectedToolName, bool isPlayerAlive)
        {
            if (phase == GamePhase.Construct)
            {
                // ビルドツール表示
                if (weaponNameText != null)
                {
                    weaponNameText.text = $"ツール: {selectedToolName}";
                    weaponNameText.gameObject.SetActive(true);
                }
                if (agentNameText != null)
                {
                    agentNameText.gameObject.SetActive(false);
                }
            }
            else if (phase == GamePhase.Bet)
            {
                // 武器表示
                if (weaponNameText != null)
                {
                    weaponNameText.text = $"武器: {weaponName}";
                    weaponNameText.gameObject.SetActive(true);
                }
                if (agentNameText != null)
                {
                    agentNameText.gameObject.SetActive(false);
                }
            }
            else if (phase == GamePhase.Hunt)
            {
                // Huntフェーズは既存のagentNameText/weaponNameTextを使用
                if (agentNameText != null)
                {
                    agentNameText.text = agentName;
                    agentNameText.gameObject.SetActive(isPlayerAlive);
                }
                if (weaponNameText != null)
                {
                    weaponNameText.text = $"武器: {weaponName}";
                    weaponNameText.gameObject.SetActive(isPlayerAlive);
                }
            }
            else
            {
                // その他フェーズ: ツール表示非表示
                if (weaponNameText != null)
                {
                    weaponNameText.gameObject.SetActive(false);
                }
            }
        }
    }
}
