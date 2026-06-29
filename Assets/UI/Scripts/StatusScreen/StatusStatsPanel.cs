using System;
using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;

namespace RYZECHo
{
    /// <summary>
    /// ステータス統計パネル: エージェント名, 体力, シールド, 武器, スキル, ウルティ
    /// </summary>
    public sealed class StatusStatsPanel : MonoBehaviour
    {
        #region Private Fields

        [Header("Status Stats Panel Configuration")]
        [Tooltip("フォントサイズ - タイトル")]
        public int titleFontSize = 16;

        [Tooltip("フォントサイズ - 通常テキスト")]
        public int bodyFontSize = 12;

        [Tooltip("フォントサイズ - 小さなテキスト")]
        public int smallFontSize = 10;

        // UI Elements
        private Text _titleText;
        private Text _agentNameText;
        private Text _agentRoleText;
        private Image _healthBarBackground;
        private Image _healthBarFill;
        private Text _healthText;
        private Image _shieldBarBackground;
        private Image _shieldBarFill;
        private Text _shieldText;
        private Text _weaponNameText;
        private Text _sidearmNameText;
        private Text _skillOneText;
        private Text _skillTwoText;
        private Text _ultimateText;

        // Colors
        private static readonly Color PanelBgColor = new(0.02f, 0.03f, 0.06f, 0.9f);
        private static readonly Color PanelBorderColor = new(0.15f, 0.3f, 0.5f, 0.7f);
        private static readonly Color HealthBarColor = new(0.24f, 0.79f, 0.44f, 1f);
        private static readonly Color HealthBarBgColor = new(0.1f, 0.2f, 0.15f, 1f);
        private static readonly Color ShieldBarColor = new(0.24f, 0.55f, 0.97f, 1f);
        private static readonly Color ShieldBarBgColor = new(0.1f, 0.15f, 0.25f, 1f);
        private static readonly Color TitleColor = new(0.9f, 0.94f, 0.97f, 1f);
        private static readonly Color LabelColor = new(0.7f, 0.75f, 0.8f, 1f);
        private static readonly Color ValueColor = new(0.94f, 0.97f, 1f, 1f);
        private static readonly Color AccentColor = new(0.24f, 0.55f, 0.97f, 1f);

        // Layout
        private const float PaddingX = 16f;
        private const float PaddingY = 12f;
        private const float BarHeight = 14f;
        private const float SectionGap = 8f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CreateStatsContent();
        }

        #endregion

        #region Panel Creation

        private void CreateStatsContent()
        {
            var panelWidth = GetComponent<RectTransform>().sizeDelta.x;
            var panelHeight = GetComponent<RectTransform>().sizeDelta.y;
            var contentY = panelHeight - PaddingY;
            var contentX = PaddingX;
            var contentWidth = panelWidth - PaddingX * 2;

            // Title: "ステータス"
            _titleText = CreateText("StatusTitle", "ステータス", titleFontSize, TextAnchor.MiddleCenter, TitleColor);
            SetTextPosition(_titleText, contentX + contentWidth / 2f, contentY, contentWidth, 24);

            contentY -= 32f;

            // Agent name
            _agentNameText = CreateText("AgentName", "", bodyFontSize, TextAnchor.MiddleLeft, AccentColor);
            SetTextPosition(_agentNameText, contentX, contentY, contentWidth, 20);

            contentY -= 24f;

            // Agent role
            _agentRoleText = CreateText("AgentRole", "", smallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_agentRoleText, contentX, contentY, contentWidth, 16);

            contentY -= 20f;

            // Divider
            AddDivider(contentX, contentY, contentWidth);

            contentY -= 12f;

            // Health bar
            contentY = CreateBarWithText("体力", "Health", contentX, contentY, contentWidth, BarHeight,
                ref _healthBarBackground, ref _healthBarFill, ref _healthText, HealthBarBgColor, HealthBarColor);

            contentY -= SectionGap;

            // Shield bar
            contentY = CreateBarWithText("シールド", "Shield", contentX, contentY, contentWidth, BarHeight,
                ref _shieldBarBackground, ref _shieldBarFill, ref _shieldText, ShieldBarBgColor, ShieldBarColor);

            contentY -= SectionGap;

            // Divider
            AddDivider(contentX, contentY, contentWidth);

            contentY -= 12f;

            // Weapon section
            _weaponNameText = CreateText("WeaponName", "", bodyFontSize, TextAnchor.MiddleLeft, TitleColor);
            SetTextPosition(_weaponNameText, contentX, contentY, contentWidth, 18);
            contentY -= 20f;

            _sidearmNameText = CreateText("SidearmName", "", smallFontSize, TextAnchor.MiddleLeft, LabelColor);
            SetTextPosition(_sidearmNameText, contentX, contentY, contentWidth, 16);
            contentY -= 18f;

            // Divider
            AddDivider(contentX, contentY, contentWidth);

            contentY -= 12f;

            // Skills section
            _skillOneText = CreateText("SkillOne", "", bodyFontSize, TextAnchor.MiddleLeft, ValueColor);
            SetTextPosition(_skillOneText, contentX, contentY, contentWidth, 18);
            contentY -= 20f;

            _skillTwoText = CreateText("SkillTwo", "", bodyFontSize, TextAnchor.MiddleLeft, ValueColor);
            SetTextPosition(_skillTwoText, contentX, contentY, contentWidth, 18);
            contentY -= 20f;

            _ultimateText = CreateText("Ultimate", "", bodyFontSize, TextAnchor.MiddleLeft, AccentColor);
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
            image.color = PanelBorderColor;
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
            // Label
            var labelGO = new GameObject($"{name}_Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(transform, false);
            var labelText = labelGO.GetComponent<Text>();
            labelText.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", smallFontSize);
            labelText.fontSize = smallFontSize;
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

            // Background bar
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

            // Fill bar
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

            // Value text
            var textGO = new GameObject($"{name}_Value", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(transform, false);
            var textComp = textGO.GetComponent<Text>();
            textComp.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", smallFontSize);
            textComp.fontSize = smallFontSize;
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

        #endregion

        #region Public Update Methods

        /// <summary>
        /// ステータス情報を一括更新する。
        /// </summary>
        public void UpdateStatus(GameModel gameModel)
        {
            if (gameModel == null || !gameObject.activeInHierarchy) return;

            // Agent info
            var agent = gameModel.GetSelectedAgent();
            _agentNameText.text = gameModel.GetAgentLabel(agent);

            var role = gameModel.GetPlayerTeamRole();
            _agentRoleText.text = role == TeamRole.Attack ? "アタッカー" : "ディフェンダー";

            // Health & Shield
            var health = gameModel.PlayerHealth;
            var maxHealth = gameModel.PlayerMaxHealth;
            var shield = gameModel.PlayerShield;
            var maxShield = gameModel.PlayerMaxShield;

            _healthText.text = $"{Mathf.CeilToInt(health)}/{maxHealth}";
            _shieldText.text = $"{Mathf.CeilToInt(shield)}/{maxShield}";

            UpdateBarFill(_healthBarFill, health / maxHealth);
            UpdateBarFill(_shieldBarFill, shield / maxShield);

            // Weapon info
            var primaryWeapon = gameModel.GetPlayerPrimaryWeapon();
            var sidearmWeapon = gameModel.GetPlayerSidearmWeapon();
            _weaponNameText.text = $"武器: {GetWeaponDisplayName(primaryWeapon)}";
            _sidearmNameText.text = $"サブ: {GetWeaponDisplayName(sidearmWeapon)}";

            // Skills
            var skillOneCd = gameModel.GetAgentSkillOneCooldown();
            var skillTwoCd = gameModel.GetAgentSkillTwoCooldown();
            var ultPoints = gameModel.GetUltPoints();
            var maxUltPoints = gameModel.GetMaxUltPoints();

            _skillOneText.text = $"スキル1: {GetCooldownText(skillOneCd)}";
            _skillTwoText.text = $"スキル2: {GetCooldownText(skillTwoCd)}";
            _ultimateText.text = $"ウルティ: {ultPoints}/{maxUltPoints}";
        }

        #endregion

        #region Helpers

        private void UpdateBarFill(Image fillImage, float ratio)
        {
            if (fillImage == null) return;
            ratio = Mathf.Clamp01(ratio);
            var rect = fillImage.rectTransform;
            rect.sizeDelta = new Vector2(rect.parent.GetComponent<RectTransform>().sizeDelta.x * ratio, rect.sizeDelta.y);
        }

        private string GetCooldownText(float cooldown)
        {
            if (cooldown <= 0f) return "利用可能";
            return $"残り {cooldown:F1}秒";
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

        #endregion
    }
}
