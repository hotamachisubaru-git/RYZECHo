using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RYZECHo.UI.StatusScreen
{
    /// <summary>
    /// スコア/統計情報表示パネル（ラウンド勝利、キル、デス、アシスト、オブジェクティブ）。
    /// </summary>
    public sealed class StatusScorePanel : MonoBehaviour
    {
        private Text _roundWinsText;
        private Text _killsText;
        private Text _deathsText;
        private Text _assistsText;
        private Text _objectiveScoreText;

        private static readonly Color LabelColor = new(0.7f, 0.75f, 0.8f, 1f);

        private const float PaddingX = 16f;
        private const float PaddingY = 12f;
        private const float SmallFontSize = 10;

        private void Awake()
        {
            CreateScoreContent();
        }

        private void CreateScoreContent()
        {
            var contentY = GetComponent<RectTransform>().sizeDelta.y - PaddingY;
            var contentX = PaddingX;
            var contentWidth = GetComponent<RectTransform>().sizeDelta.x - PaddingX * 2;

            AddDivider(contentX, contentY, contentWidth);
            contentY -= 12f;

            _roundWinsText = CreateText("RoundWins", "ラウンド勝利: 0/0", SmallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_roundWinsText, contentX, contentY, contentWidth, 16);
            contentY -= 16f;

            _killsText = CreateText("Kills", "キル: 0", SmallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_killsText, contentX, contentY, contentWidth, 16);
            contentY -= 16f;

            _deathsText = CreateText("Deaths", "デス: 0", SmallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_deathsText, contentX, contentY, contentWidth, 16);
            contentY -= 16f;

            _assistsText = CreateText("Assists", "アシスト: 0", SmallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_assistsText, contentX, contentY, contentWidth, 16);
            contentY -= 16f;

            _objectiveScoreText = CreateText("ObjectiveScore", "ラウンド: 0", SmallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_objectiveScoreText, contentX, contentY, contentWidth, 16);
        }

        private Text CreateText(string name, string text, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(Text));
            var tc = go.GetComponent<Text>();
            tc.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", fontSize);
            tc.fontSize = fontSize;
            tc.alignment = alignment;
            tc.color = color;
            tc.text = text;
            tc.enableWordWrapping = false;
            return tc;
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
            image.color = new Color(0.15f, 0.3f, 0.5f, 0.7f);
            image.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, 1);
        }

        public void UpdateScore(int playerWins, int enemyWins, int kills, int deaths, int assists, int objectiveScore)
        {
            if (_roundWinsText != null) _roundWinsText.text = $"ラウンド勝利: {playerWins}/{enemyWins}";
            if (_killsText != null) _killsText.text = $"キル: {kills}";
            if (_deathsText != null) _deathsText.text = $"デス: {deaths}";
            if (_assistsText != null) _assistsText.text = $"アシスト: {assists}";
            if (_objectiveScoreText != null) _objectiveScoreText.text = $"ラウンド: {objectiveScore}";
        }
    }
}
