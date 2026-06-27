using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RYZECHo.UI.StatusScreen
{
    /// <summary>
    /// ステータス画面のベースパネル作成を担当。
    /// Canvasからの配置、ボーダー、タイトル、エージェント名/ロール表示を管理。
    /// </summary>
    public sealed class StatusScreenBasePanel : MonoBehaviour
    {
        [Header("Configuration")]
        [Range(200, 500)] public int panelWidth = 320;
        [Range(200, 600)] public int panelHeight = 400;
        public int titleFontSize = 16;
        public int bodyFontSize = 12;
        public int smallFontSize = 10;

        private GameObject _panel;
        private Text _titleText;
        private Text _agentNameText;
        private Text _agentRoleText;
        private Vector2 _panelPosition;

        private static readonly Color PanelBgColor = new(0.02f, 0.03f, 0.06f, 0.9f);
        private static readonly Color PanelBorderColor = new(0.15f, 0.3f, 0.5f, 0.7f);
        private static readonly Color TitleColor = new(0.9f, 0.94f, 0.97f, 1f);
        private static readonly Color LabelColor = new(0.7f, 0.75f, 0.8f, 1f);
        private static readonly Color AccentColor = new(0.24f, 0.55f, 0.97f, 1f);
        private const float PaddingX = 16f;
        private const float PaddingY = 12f;

        public GameObject Panel => _panel;
        public RectTransform PanelRect => _panel?.GetComponent<RectTransform>();

        public void CreatePanel(Canvas canvas)
        {
            if (canvas == null)
            {
                Debug.LogError("[StatusScreen] No Canvas found in scene.");
                return;
            }

            _panel = new GameObject("StatusPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);
            _panel.transform.SetAsLastSibling();

            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

            var panelImage = _panel.GetComponent<Image>();
            panelImage.color = PanelBgColor;
            panelImage.raycastTarget = true;

            AddBorder(_panel, PanelBorderColor, 1.5f);
            _panelPosition = new Vector2(PaddingX, PaddingY);
            panelRect.anchoredPosition = _panelPosition;

            CreateBaseContent();
        }

        private void CreateBaseContent()
        {
            var contentY = panelHeight - PaddingY;
            var contentX = PaddingX;
            var contentWidth = panelWidth - PaddingX * 2;

            _titleText = CreateText("StatusTitle", "ステータス", titleFontSize, TextAnchor.MiddleCenter, TitleColor);
            SetTextPosition(_titleText, contentX + contentWidth / 2f, contentY, contentWidth, 24);
            contentY -= 32f;

            _agentNameText = CreateText("AgentName", "", bodyFontSize, TextAnchor.MiddleLeft, AccentColor);
            SetTextPosition(_agentNameText, contentX, contentY, contentWidth, 20);
            contentY -= 24f;

            _agentRoleText = CreateText("AgentRole", "", smallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_agentRoleText, contentX, contentY, contentWidth, 16);
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
            rect.SetParent(_panel.transform, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void AddBorder(GameObject target, Color color, float thickness)
        {
            var borderGO = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGO.transform.SetParent(target.transform, false);
            borderGO.transform.SetAsFirstSibling();
            var borderImage = borderGO.GetComponent<Image>();
            borderImage.color = color;
            borderImage.raycastTarget = false;
            var borderRect = borderGO.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(thickness, thickness);
        }

        public void UpdateAgentInfo(string name, string role)
        {
            if (_agentNameText != null) _agentNameText.text = name;
            if (_agentRoleText != null) _agentRoleText.text = role;
        }
    }
}
