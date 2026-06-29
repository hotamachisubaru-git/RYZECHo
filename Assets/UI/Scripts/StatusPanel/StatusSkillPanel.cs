using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;

namespace RYZECHo.UI.StatusPanel
{
    /// <summary>
    /// スキル・ウルティ表示パネル。
    /// </summary>
    public sealed class StatusSkillPanel : MonoBehaviour
    {
        private Text _skillOneText;
        private Text _skillTwoText;
        private Text _ultimateText;

        private static readonly Color ValueColor = new(0.94f, 0.97f, 1f, 1f);
        private static readonly Color AccentColor = new(0.24f, 0.55f, 0.97f, 1f);

        private const float PaddingX = 16f;
        private const float PaddingY = 12f;
        private const float SectionGap = 8f;

        private void Awake()
        {
            CreateSkillContent();
        }

        private void CreateSkillContent()
        {
            var contentY = GetComponent<RectTransform>().sizeDelta.y - PaddingY;
            var contentX = PaddingX;
            var contentWidth = GetComponent<RectTransform>().sizeDelta.x - PaddingX * 2;

            AddDivider(contentX, contentY, contentWidth);
            contentY -= 12f;

            _skillOneText = CreateText("SkillOne", "スキル1: 利用可能", 12, TextAnchor.MiddleLeft, ValueColor);
            SetTextPosition(_skillOneText, contentX, contentY, contentWidth, 18);
            contentY -= 20f;

            _skillTwoText = CreateText("SkillTwo", "スキル2: 利用可能", 12, TextAnchor.MiddleLeft, ValueColor);
            SetTextPosition(_skillTwoText, contentX, contentY, contentWidth, 18);
            contentY -= 20f;

            _ultimateText = CreateText("Ultimate", "ウルティ: 0/0", 12, TextAnchor.MiddleLeft, AccentColor);
            SetTextPosition(_ultimateText, contentX, contentY, contentWidth, 18);
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
            image.color = new Color(0.15f, 0.3f, 0.5f, 0.7f);
            image.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, 1);
        }

        public void UpdateSkillOne(float cooldown)
        {
            if (_skillOneText != null)
                _skillOneText.text = $"スキル1: {GetCooldownText(cooldown)}";
        }

        public void UpdateSkillTwo(float cooldown)
        {
            if (_skillTwoText != null)
                _skillTwoText.text = $"スキル2: {GetCooldownText(cooldown)}";
        }

        public void UpdateUltimate(int current, int max)
        {
            if (_ultimateText != null)
                _ultimateText.text = $"ウルティ: {current}/{max}";
        }

        private string GetCooldownText(float cooldown)
        {
            if (cooldown <= 0f) return "利用可能";
            return $"残り {cooldown:F1}秒";
        }
    }
}
