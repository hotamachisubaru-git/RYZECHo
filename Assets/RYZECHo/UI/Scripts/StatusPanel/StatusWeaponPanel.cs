using UnityEngine;
using UnityEngine.UI;

namespace RYZECHo.UI.StatusPanel
{
    /// <summary>
    /// 武器・サブ武器表示パネル。
    /// </summary>
    public sealed class StatusWeaponPanel : MonoBehaviour
    {
        private Text _weaponNameText;
        private Text _sidearmNameText;

        private static readonly Color LabelColor = new(0.7f, 0.75f, 0.8f, 1f);
        private static readonly Color ValueColor = new(0.94f, 0.97f, 1f, 1f);

        private const float PaddingX = 16f;
        private const float PaddingY = 12f;
        private const float SmallFontSize = 10;

        private void Awake()
        {
            CreateWeaponContent();
        }

        private void CreateWeaponContent()
        {
            var contentY = GetComponent<RectTransform>().sizeDelta.y - PaddingY;
            var contentX = PaddingX;
            var contentWidth = GetComponent<RectTransform>().sizeDelta.x - PaddingX * 2;

            AddDivider(contentX, contentY, contentWidth);
            contentY -= 12f;

            _weaponNameText = CreateText("WeaponName", "武器: -", 12, TextAnchor.MiddleLeft, ValueColor);
            SetTextPosition(_weaponNameText, contentX, contentY, contentWidth, 18);
            contentY -= 20f;

            _sidearmNameText = CreateText("SidearmName", "サブ: -", SmallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_sidearmNameText, contentX, contentY, contentWidth, 16);
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

        public void UpdateWeaponName(WeaponType weapon)
        {
            if (_weaponNameText != null)
                _weaponNameText.text = $"武器: {GetWeaponDisplayName(weapon)}";
        }

        public void UpdateSidearmName(WeaponType weapon)
        {
            if (_sidearmNameText != null)
                _sidearmNameText.text = $"サブ: {GetWeaponDisplayName(weapon)}";
        }

        private string GetWeaponDisplayName(WeaponType weapon)
        {
            return weapon switch
            {
                WeaponType.Blitz => "Blitz",
                WeaponType.Monster => "Monster",
                WeaponType.Melt => "Melt",
                WeaponType.Fairy => "Fairy",
                WeaponType.Giant => "Giant",
                WeaponType.Juggernaut => "Juggernaut",
                WeaponType.Violet => "Violet",
                WeaponType.Changer => "Changer",
                WeaponType.Howl => "Howl",
                WeaponType.Pulse => "Pulse",
                WeaponType.Shard => "Shard",
                _ => weapon.ToString(),
            };
        }
    }
}
