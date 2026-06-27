using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;

namespace RYZECHo.UI.StatusPanel
{
    /// <summary>
    /// HP/シールドバー表示パネル。
    /// </summary>
    public sealed class StatusHealthPanel : MonoBehaviour
    {
        private Image _healthBarBackground;
        private Image _healthBarFill;
        private Text _healthText;
        private Image _shieldBarBackground;
        private Image _shieldBarFill;
        private Text _shieldText;

        private static readonly Color HealthBarColor = new(0.24f, 0.79f, 0.44f, 1f);
        private static readonly Color HealthBarBgColor = new(0.1f, 0.2f, 0.15f, 1f);
        private static readonly Color ShieldBarColor = new(0.24f, 0.55f, 0.97f, 1f);
        private static readonly Color ShieldBarBgColor = new(0.1f, 0.15f, 0.25f, 1f);
        private static readonly Color LabelColor = new(0.7f, 0.75f, 0.8f, 1f);
        private static readonly Color ValueColor = new(0.94f, 0.97f, 1f, 1f);

        private const float PaddingX = 16f;
        private const float PaddingY = 12f;
        private const float BarHeight = 14f;
        private const float SectionGap = 8f;
        private const int SmallFontSize = 10;

        private void Awake()
        {
            CreateHealthContent();
        }

        private void CreateHealthContent()
        {
            var contentY = GetComponent<RectTransform>().sizeDelta.y - PaddingY;
            var contentX = PaddingX;
            var contentWidth = GetComponent<RectTransform>().sizeDelta.x - PaddingX * 2;

            AddDivider(contentX, contentY, contentWidth);
            contentY -= 12f;

            contentY = CreateBarWithText("体力", "Health", contentX, contentY, contentWidth, BarHeight,
                ref _healthBarBackground, ref _healthBarFill, ref _healthText, HealthBarBgColor, HealthBarColor);
            contentY -= SectionGap;

            contentY = CreateBarWithText("シールド", "Shield", contentX, contentY, contentWidth, BarHeight,
                ref _shieldBarBackground, ref _shieldBarFill, ref _shieldText, ShieldBarBgColor, ShieldBarColor);
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

        private float CreateBarWithText(string label, string name, float x, float y, float width, float barHeight,
            ref Image bgRef, ref Image fillRef, ref Text textRef, Color bgColor, Color fillColor)
        {
            var labelGO = new GameObject($"{name}_Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(transform, false);
            var labelText = labelGO.GetComponent<Text>();
            labelText.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", SmallFontSize);
            labelText.fontSize = SmallFontSize;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = LabelColor;
            labelText.text = label;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(0, 1);
            labelRect.pivot = new Vector2(0, 1f);
            labelRect.anchoredPosition = new Vector2(x, y);
            labelRect.sizeDelta = new Vector2(width, 14);

            var bgGO = new GameObject($"{name}_Bg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(transform, false);
            var bgImage = bgGO.GetComponent<Image>();
            bgImage.color = bgColor;
            bgImage.raycastTarget = false;
            bgRef = bgImage;
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 1);
            bgRect.anchorMax = new Vector2(0, 1);
            bgRect.pivot = new Vector2(0, 1f);
            bgRect.anchoredPosition = new Vector2(x, y - 16);
            bgRect.sizeDelta = new Vector2(width, barHeight);

            var fillGO = new GameObject($"{name}_Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(bgGO.transform, false);
            var fillImage = fillGO.GetComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            fillRef = fillImage;
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.sizeDelta = new Vector2(0, 0);

            var textGO = new GameObject($"{name}_Value", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(transform, false);
            var textComp = textGO.GetComponent<Text>();
            textComp.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", SmallFontSize);
            textComp.fontSize = SmallFontSize;
            textComp.alignment = TextAnchor.MiddleRight;
            textComp.color = ValueColor;
            textComp.text = "100/100";
            textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            textRef = textComp;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(1, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(1, 1f);
            textRect.anchoredPosition = new Vector2(-x - width, y - 16);
            textRect.sizeDelta = new Vector2(80, barHeight);

            return y - 16 - barHeight - 4;
        }

        public void UpdateHealth(float current, float max)
        {
            if (_healthText != null)
                _healthText.text = $"{Mathf.CeilToInt(current)}/{max}";
            UpdateBarFill(_healthBarFill, current / max);
        }

        public void UpdateShield(float current, float max)
        {
            if (_shieldText != null)
                _shieldText.text = $"{Mathf.CeilToInt(current)}/{max}";
            UpdateBarFill(_shieldBarFill, current / max);
        }

        private void UpdateBarFill(Image fillImage, float ratio)
        {
            if (fillImage == null) return;
            ratio = Mathf.Clamp01(ratio);
            var rect = fillImage.rectTransform;
            rect.sizeDelta = new Vector2(rect.parent.GetComponent<RectTransform>().sizeDelta.x * ratio, rect.sizeDelta.y);
        }
    }
}
