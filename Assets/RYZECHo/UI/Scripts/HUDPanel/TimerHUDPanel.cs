using TMPro;
using UnityEngine;

namespace RYZECHo.UI
{
    /// <summary>
    /// ラウンドタイマー表示を管理するHUDパネル。
    /// フェーズに応じてタイマーフォーマットを切り替える。
    /// </summary>
    public class TimerHUDPanel : MonoBehaviour
    {
        [Header("Timer Display")]
        [Tooltip("ラウンドタイマー表示テキスト")]
        public TextMeshProUGUI timerText;

        /// <summary>
        /// タイマー表示をフェーズに応じて適用。
        /// </summary>
        public void ApplyTimerState(GamePhase phase, float roundTimer)
        {
            if (timerText == null) return;

            switch (phase)
            {
                case GamePhase.Hunt:
                    // Huntフェーズ: リアルタイマー (残り秒)
                    if (roundTimer > 0f)
                    {
                        int minutes = Mathf.FloorToInt(roundTimer / 60f);
                        int seconds = Mathf.FloorToInt(roundTimer) % 60;
                        timerText.text = $"{minutes:D2}:{seconds:D2}";
                        timerText.gameObject.SetActive(true);
                    }
                    else
                    {
                        timerText.gameObject.SetActive(false);
                    }
                    break;

                case GamePhase.Construct:
                    // Constructフェーズ: 残り秒
                    if (roundTimer > 0f)
                    {
                        int minutes = Mathf.FloorToInt(roundTimer / 60f);
                        int seconds = Mathf.FloorToInt(roundTimer) % 60;
                        timerText.text = $"CONSTRUCT {minutes:D2}:{seconds:D2}";
                        timerText.gameObject.SetActive(true);
                    }
                    else
                    {
                        timerText.gameObject.SetActive(false);
                    }
                    break;

                case GamePhase.Bet:
                    // Betフェーズ: フェーズ固有タイマー
                    if (roundTimer > 0f)
                    {
                        int minutes = Mathf.FloorToInt(roundTimer / 60f);
                        int seconds = Mathf.FloorToInt(roundTimer) % 60;
                        timerText.text = $"BET {minutes:D2}:{seconds:D2}";
                        timerText.gameObject.SetActive(true);
                    }
                    else
                    {
                        timerText.gameObject.SetActive(false);
                    }
                    break;

                default:
                    // RoundResult, Victory, Defeat等: タイマー非表示
                    timerText.gameObject.SetActive(false);
                    break;
            }
        }
    }
}
