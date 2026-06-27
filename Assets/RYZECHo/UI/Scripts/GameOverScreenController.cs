using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RYZECHo.UI
{
    /// <summary>
    /// ゲームオーバー画面のコントローラー。
    /// 勝利/敗北の結果を表示し、タイトル画面へ戻るかリトライするかを選択できる。
    /// </summary>
    public class GameOverScreenController : UIScreen
    {
        [Header("Game Over Elements")]
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text turnsText;
        [SerializeField] private Button titleButton;
        [SerializeField] private Button retryButton;

        private bool _isVictory;
        private int _score;
        private int _turnsPlayed;

        private void Awake()
        {
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();
            // UI要素のキャッシュ
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            if (ScreenRoot == null) return;

            if (resultText == null)
                resultText = FindTextInChildren("ResultText");
            if (scoreText == null)
                scoreText = FindTextInChildren("ScoreText");
            if (turnsText == null)
                turnsText = FindTextInChildren("TurnsText");
            if (titleButton == null)
                titleButton = FindButton("TitleButton");
            if (retryButton == null)
                retryButton = FindButton("RetryButton");
        }

        private TMP_Text FindTextInChildren(string name)
        {
            var obj = ScreenRoot.transform.Find(name);
            return obj?.GetComponent<TMP_Text>();
        }

        private Button FindButton(string name)
        {
            var obj = ScreenRoot.transform.Find(name);
            return obj?.GetComponent<Button>();
        }

        public override void OnShow()
        {
            base.OnShow();
            CacheUIElements();

            // 結果テキストを更新
            UpdateResultDisplay();

            // ボタンイベントを登録
            if (titleButton != null)
                AddButtonListener(titleButton, OnTitle);
            if (retryButton != null)
                AddButtonListener(retryButton, OnRetry);

            // デフォルトフォントを適用
            ApplyDefaultFont();
        }

        private void ApplyDefaultFont()
        {
            if (defaultFont == null) return;
            if (resultText != null) resultText.font = defaultFont;
            if (scoreText != null) scoreText.font = defaultFont;
            if (turnsText != null) turnsText.font = defaultFont;
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        /// <summary>
        /// ゲーム結果を設定
        /// </summary>
        public void SetResult(bool isVictory, int score, int turnsPlayed)
        {
            _isVictory = isVictory;
            _score = score;
            _turnsPlayed = turnsPlayed;
        }

        private void UpdateResultDisplay()
        {
            // 結果テキスト (VICTORY/DEFEAT)
            if (resultText != null)
            {
                resultText.text = _isVictory ? "VICTORY" : "DEFEAT";
                resultText.color = _isVictory ? new Color(0.4f, 0.95f, 0.5f) : new Color(0.9f, 0.25f, 0.25f);
            }

            // スコア/ターン情報
            if (scoreText != null)
            {
                scoreText.text = $"Score: {_score}";
            }

            if (turnsText != null)
            {
                turnsText.text = $"Turns: {_turnsPlayed}";
            }
        }

        private void OnTitle()
        {
            // タイトル画面に戻る
            UIScreenManager.Instance?.ResetGame();
        }

        private void OnRetry()
        {
            // ゲームリトライ
            UIScreenManager.Instance?.StartGame();
        }

        public override void OnStartGame() { }
        public override void OnOpenSettings() { }
        public override void OnExitGame() { }
        public override void OnRetry() { OnRetry(); }
    }
}
