using TMPro;
using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;

namespace RYZECHo.UI
{
    /// <summary>
    /// スコア表示を管理するHUDパネル。
    /// </summary>
    public class ScoreHUDPanel : MonoBehaviour
    {
        [Header("Score Display")]
        [Tooltip("スコア表示テキスト (SCORE X - Y)")]
        public TextMeshProUGUI scoreText;

        [Tooltip("プレイヤー勝利数テキスト")]
        public TextMeshProUGUI playerScoreText;

        [Tooltip("敵勝利数テキスト")]
        public TextMeshProUGUI enemyScoreText;

        /// <summary>
        /// スコア状態を適用。
        /// </summary>
        public void ApplyScoreState(int playerRoundWins, int enemyRoundWins)
        {
            // スコア更新
            if (scoreText != null)
            {
                scoreText.text = $"SCORE {playerRoundWins} - {enemyRoundWins}";
            }
            if (playerScoreText != null)
            {
                playerScoreText.text = playerRoundWins.ToString();
            }
            if (enemyScoreText != null)
            {
                enemyScoreText.text = enemyRoundWins.ToString();
            }
        }
    }
}
