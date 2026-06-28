using System;
using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;

namespace RYZECHo
{
    /// <summary>
    /// プログレス/スコアパネル: 所持金, ラウンド勝利, キル/デス/アシスト, ラウンド
    /// </summary>
    public sealed class StatusProgressPanel : MonoBehaviour
    {
        #region Private Fields

        [Header("Status Progress Panel Configuration")]
        [Tooltip("フォントサイズ - 通常テキスト")]
        public int bodyFontSize = 12;

        [Tooltip("フォントサイズ - 小さなテキスト")]
        public int smallFontSize = 10;

        // UI Elements
        private Text _creditsText;
        private Text _roundWinsText;
        private Text _killsText;
        private Text _deathsText;
        private Text _assistsText;
        private Text _objectiveScoreText;

        // Colors
        private static readonly Color PanelBorderColor = new(0.15f, 0.3f, 0.5f, 0.7f);
        private static readonly Color LabelColor = new(0.7f, 0.75f, 0.8f, 1f);
        private static readonly Color ValueColor = new(0.94f, 0.97f, 1f, 1f);
        private static readonly Color CreditsColor = new(0.93f, 0.79f, 0.44f, 1f);

        // Layout
        private const float PaddingX = 16f;
        private const float PaddingY = 12f;
        private const float SectionGap = 8f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CreateProgressContent();
        }

        #endregion

        #region Panel Creation

        private void CreateProgressContent()
        {
            var panelWidth = GetComponent<RectTransform>().sizeDelta.x;
            var panelHeight = GetComponent<RectTransform>().sizeDelta.y;
            var contentY = panelHeight - PaddingY;
            var contentX = PaddingX;
            var contentWidth = panelWidth - PaddingX * 2;

            // Divider
            AddDivider(contentX, contentY, contentWidth);
            contentY -= 12f;

            // Economy section
            _creditsText = CreateText("Credits", "", bodyFontSize, TextAnchor.MiddleLeft, CreditsColor);
            SetTextPosition(_creditsText, contentX, contentY, contentWidth, 18);
            contentY -= 20f;

            // Score section
            _roundWinsText = CreateText("RoundWins", "", smallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_roundWinsText, contentX, contentY, contentWidth, 16);
            contentY -= 16f;

            _killsText = CreateText("Kills", "", smallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_killsText, contentX, contentY, contentWidth, 16);
            contentY -= 16f;

            _deathsText = CreateText("Deaths", "", smallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_deathsText, contentX, contentY, contentWidth, 16);
            contentY -= 16f;

            _assistsText = CreateText("Assists", "", smallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_assistsText, contentX, contentY, contentWidth, 16);
            contentY -= 16f;

            _objectiveScoreText = CreateText("ObjectiveScore", "", smallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_objectiveScoreText, contentX, contentY, contentWidth, 16);
        }

        private Text CreateText(string name, string text, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(Text));
            var textComponent = go.GetComponent<Text>();
            textComponent.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", fontSize);
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = color;
            textComponent.text = text;
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            return textComponent;
        }

        private void SetTextPosition(Text text, float x, float y, float width, float height)
        {
            var rect = text.rectTransform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void AddDivider(float x, float y, float width)
        {
            var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>();
            image.color = PanelBorderColor;
            image.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, 1);
        }

        #endregion

        #region Public Update Methods

        /// <summary>
        /// プログレス/スコア情報を一括更新する。
        /// </summary>
        public void UpdateProgress(GameModel gameModel)
        {
            if (gameModel == null || !gameObject.activeInHierarchy) return;

            // Economy
            var credits = gameModel.GetCredits();
            _creditsText.text = $"所持金: {credits} 円";

            // Score
            _roundWinsText.text = $"ラウンド勝利: {gameModel.PlayerRoundWins}/{gameModel.EnemyRoundWins}";
            _killsText.text = $"キル: {gameModel.MatchTeamEliminations}";
            _deathsText.text = $"デス: {gameModel.MatchPlayerDeaths}";
            _objectiveScoreText.text = $"ラウンド: {gameModel.CurrentRound}";
        }

        #endregion
    }
}
