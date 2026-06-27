using UnityEngine;
using UnityEngine.UI;
using RYZECHo.UI.ViewModels;

namespace RYZECHo.UI
{
    /// <summary>
    /// クロスヘア表示/体力バー/シールドバー/ミニマップ/ダメージ数のトグルを管理するサブパネル
    /// </summary>
    public class DisplayVREnablePanel : MonoBehaviour
    {
        [Header("UI Configuration")]
        public int sliderHeight = 24;
        public int bodyFontSize = 13;
        public int headerFontSize = 16;

        [Header("Colors")]
        public Color accentColor = new Color(0.24f, 0.55f, 0.97f);
        public Color textNormalColor = new Color(0.9f, 0.94f, 0.97f);

        [Header("Content Area")]
        [SerializeField] private float _contentLeft;
        [SerializeField] private float _contentWidth;
        [SerializeField] private float _contentTop;

        private Toggle _crosshairToggle;
        private Toggle _healthBarToggle;
        private Toggle _shieldBarToggle;
        private Toggle _minimapToggle;
        private Toggle _damageNumbersToggle;
        private Transform _panelTransform;
        private DisplaySettingsViewModel _viewModel;

        public bool CrosshairVisible => _viewModel?.CrosshairVisible ?? true;
        public bool HealthBarVisible => _viewModel?.HealthBarVisible ?? true;
        public bool ShieldBarVisible => _viewModel?.ShieldBarVisible ?? true;
        public bool MinimapVisible => _viewModel?.MinimapVisible ?? true;
        public bool DamageNumbersVisible => _viewModel?.DamageNumbersVisible ?? true;

        private void Awake()
        {
            _viewModel = new DisplaySettingsViewModel();
            BindViewModelEvents();
        }

        private void OnDestroy()
        {
            UnbindViewModelEvents();
            _viewModel?.Save();
            _viewModel?.Dispose();
        }

        private void BindViewModelEvents()
        {
            if (_viewModel == null) return;
            _viewModel.OnCrosshairVisibleChanged += v => UpdateToggleValue(ref _crosshairToggle, v);
            _viewModel.OnHealthBarVisibleChanged += v => UpdateToggleValue(ref _healthBarToggle, v);
            _viewModel.OnShieldBarVisibleChanged += v => UpdateToggleValue(ref _shieldBarToggle, v);
            _viewModel.OnMinimapVisibleChanged += v => UpdateToggleValue(ref _minimapToggle, v);
            _viewModel.OnDamageNumbersVisibleChanged += v => UpdateToggleValue(ref _damageNumbersToggle, v);
        }

        private void UnbindViewModelEvents()
        {
            if (_viewModel == null) return;
            _viewModel.OnCrosshairVisibleChanged -= null;
            _viewModel.OnHealthBarVisibleChanged -= null;
            _viewModel.OnShieldBarVisibleChanged -= null;
            _viewModel.OnMinimapVisibleChanged -= null;
            _viewModel.OnDamageNumbersVisibleChanged -= null;
        }

        public void Initialize(Transform parentTransform, float contentLeft, float contentWidth, float contentTop)
        {
            _panelTransform = parentTransform;
            _contentLeft = contentLeft;
            _contentWidth = contentWidth;
            _contentTop = contentTop;
            CreateUI();
        }

        private void CreateUI()
        {
            var startY = _contentTop - 16f;
            var toggleStartY = startY - 160f;
            CreateToggleRow("クロスヘア表示", _viewModel.CrosshairVisible, toggleStartY, "crosshair", ref _crosshairToggle);
            CreateToggleRow("体力バー表示", _viewModel.HealthBarVisible, toggleStartY - 40f, "healthbar", ref _healthBarToggle);
            CreateToggleRow("シールドバー表示", _viewModel.ShieldBarVisible, toggleStartY - 80f, "shieldbar", ref _shieldBarToggle);
            CreateToggleRow("ミニマップ表示", _viewModel.MinimapVisible, toggleStartY - 120f, "minimap", ref _minimapToggle);
            CreateToggleRow("ダメージ数表示", _viewModel.DamageNumbersVisible, toggleStartY - 160f, "damageNumbers", ref _damageNumbersToggle);
        }

        private void CreateToggleRow(string label, bool initialActive, float yPos, string key, ref Toggle toggle)
        {
            var go = new GameObject($"Toggle_{key}", typeof(RectTransform), typeof(Image), typeof(Toggle));
            go.transform.SetParent(_panelTransform, false);
            toggle = go.GetComponent<Toggle>();
            toggle.isOn = initialActive;

            var image = go.GetComponent<Image>();
            image.color = initialActive ? accentColor : new Color(0.1f, 0.15f, 0.25f, 0.5f);
            image.raycastTarget = true;

            var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.GetComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", bodyFontSize);
            text.fontSize = bodyFontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = textNormalColor;
            text.text = label;
            text.enableWordWrapping = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1f);
            rect.anchoredPosition = new Vector2(_contentLeft + 16, yPos);
            rect.sizeDelta = new Vector2(_contentWidth - 32, 28);

            toggle.onValueChanged.AddListener(v => OnToggleChanged(key, v));
        }

        private void OnToggleChanged(string key, bool value)
        {
            if (_viewModel == null) return;
            switch (key)
            {
                case "crosshair":
                    _viewModel.CrosshairVisible = value;
                    ApplyVisualSetting("CrosshairVisible", value ? 1f : 0f);
                    break;
                case "healthbar":
                    _viewModel.HealthBarVisible = value;
                    ApplyVisualSetting("HealthBarVisible", value ? 1f : 0f);
                    break;
                case "shieldbar":
                    _viewModel.ShieldBarVisible = value;
                    ApplyVisualSetting("ShieldBarVisible", value ? 1f : 0f);
                    break;
                case "minimap":
                    _viewModel.MinimapVisible = value;
                    ApplyVisualSetting("MinimapVisible", value ? 1f : 0f);
                    break;
                case "damageNumbers":
                    _viewModel.DamageNumbersVisible = value;
                    ApplyVisualSetting("DamageNumbersVisible", value ? 1f : 0f);
                    break;
            }
        }

        private void UpdateToggleValue(ref Toggle toggle, bool value)
        {
            if (toggle != null) toggle.isOn = value;
        }

        private void ApplyVisualSetting(string setting, float value) { }
    }
}
