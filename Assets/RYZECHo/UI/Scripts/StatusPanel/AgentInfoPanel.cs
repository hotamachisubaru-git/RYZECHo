using UnityEngine;
using UnityEngine.UI;

namespace RYZECHo
{
    /// <summary>
    /// エージェント情報パネル: 名前/ロール/スキル表示
    /// </summary>
    public sealed class AgentInfoPanel : MonoBehaviour
    {
        #region Private Fields

        private Text _agentNameText;
        private Text _agentRoleText;

        // Colors
        private static readonly Color AccentColor = new(0.24f, 0.55f, 0.97f, 1f);
        private static readonly Color LabelColor = new(0.7f, 0.75f, 0.8f, 1f);
        private static readonly Color TitleColor = new(0.9f, 0.94f, 0.97f, 1f);

        // Layout
        private const float PaddingX = 16f;
        private const float BodyFontSize = 12;
        private const float SmallFontSize = 10;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CreateAgentInfoContent();
        }

        #endregion

        #region Panel Creation

        private void CreateAgentInfoContent()
        {
            var contentX = PaddingX;
            var contentWidth = GetComponent<RectTransform>().sizeDelta.x - PaddingX * 2;
            var contentY = GetComponent<RectTransform>().sizeDelta.y - PaddingY();

            // Agent name
            _agentNameText = CreateText("AgentName", "", BodyFontSize, TextAnchor.MiddleLeft, AccentColor);
            SetTextPosition(_agentNameText, contentX, contentY, contentWidth, 20);

            contentY -= 24f;

            // Agent role
            _agentRoleText = CreateText("AgentRole", "", SmallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_agentRoleText, contentX, contentY, contentWidth, 16);
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
            textComponent.enableWordWrapping = false;
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

        private float PaddingY() => 12f;

        #endregion

        #region Public Update Methods

        /// <summary>
        /// エージェント名を更新する。
        /// </summary>
        public void UpdateAgentName(string name)
        {
            if (_agentNameText != null)
                _agentNameText.text = name;
        }

        /// <summary>
        /// エージェントのロールを更新する。
        /// </summary>
        public void UpdateAgentRole(TeamRole role)
        {
            if (_agentRoleText != null)
                _agentRoleText.text = role == TeamRole.Attack ? "アタッカー" : "ディフェンダー";
        }

        #endregion
    }
}
