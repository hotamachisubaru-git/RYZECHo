using UnityEngine;
using UnityEngine.UI;

namespace RYZECHo.UI.StatusScreen
{
    /// <summary>
    /// 所持金表示パネル。
    /// </summary>
    public sealed class StatusEconomyPanel : MonoBehaviour
    {
        private Text _creditsText;

        private static readonly Color CreditsColor = new(0.93f, 0.79f, 0.44f, 1f);
        private const float PaddingX = 16f;
        private const float PaddingY = 12f;

        private void Awake()
        {
            CreateEconomyContent();
        }

        private void CreateEconomyContent()
        {
            var contentY = GetComponent<RectTransform>().sizeDelta.y - PaddingY;
            var contentX = PaddingX;
            var contentWidth = GetComponent<RectTransform>().sizeDelta.x - PaddingX * 2;

            AddDivider(contentX, contentY, contentWidth);
            contentY -= 12f;

            _creditsText = CreateText("Credits", "所持金: 0 円", 12, TextAnchor.MiddleLeft, CreditsColor);
            SetTextPosition(_creditsText, contentX, contentY, contentWidth, 18);
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

        public void UpdateCredits(int credits)
        {
            if (_creditsText != null)
                _creditsText.text = $"所持金: {credits} 円";
        }
    }
}
