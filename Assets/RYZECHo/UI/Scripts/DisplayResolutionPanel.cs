using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;
using RYZECHo.UI.ViewModels;

namespace RYZECHo.UI
{
    /// <summary>
    /// FOV/マウス感度/クロスヘアサイズのスライダーを管理するサブパネル
    /// </summary>
    public class DisplayResolutionPanel : MonoBehaviour
    {
        [Header("UI Configuration")]
        public int sliderHeight = 24;
        public int bodyFontSize = 13;
        public int headerFontSize = 16;

        [Header("Colors")]
        public Color accentColor = new Color(0.24f, 0.55f, 0.97f);
        public Color sliderFillColor = new Color(0.2f, 0.4f, 0.7f);
        public Color sliderBgColor = new Color(0.1f, 0.15f, 0.2f);

        [Header("Content Area")]
        [SerializeField] private float _contentLeft;
        [SerializeField] private float _contentWidth;
        [SerializeField] private float _contentTop;

        private Slider _fovSlider;
        private Slider _sensitivitySlider;
        private Slider _crosshairSizeSlider;
        private Text _fovValueText;
        private Text _sensitivityValueText;
        private Text _crosshairValueText;
        private Transform _panelTransform;
        private DisplaySettingsViewModel _viewModel;

        public float Fov => _viewModel?.Fov ?? 100f;
        public float Sensitivity => _viewModel?.Sensitivity ?? 1.0f;
        public float CrosshairSize => _viewModel?.CrosshairSize ?? 1.0f;

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
            _viewModel.OnFovChanged += v => UpdateSliderDisplay("fov", v, ref _fovSlider, ref _fovValueText);
            _viewModel.OnSensitivityChanged += v => UpdateSliderDisplay("sensitivity", v, ref _sensitivitySlider, ref _sensitivityValueText);
            _viewModel.OnCrosshairSizeChanged += v => UpdateSliderDisplay("crosshair", v, ref _crosshairSizeSlider, ref _crosshairValueText);
        }

        private void UnbindViewModelEvents()
        {
            if (_viewModel == null) return;
            _viewModel.OnFovChanged -= null;
            _viewModel.OnSensitivityChanged -= null;
            _viewModel.OnCrosshairSizeChanged -= null;
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
            CreateSliderRow("FOV", "fov", 60f, 120f, _viewModel.Fov, 5f, ref _fovSlider, ref _fovValueText, startY - 40f, 0);
            CreateSliderRow("マウス感度", "sensitivity", 0.1f, 3f, _viewModel.Sensitivity, 0.1f, ref _sensitivitySlider, ref _sensitivityValueText, startY - 80f, 1);
            CreateSliderRow("クロスヘアサイズ", "crosshair", 0.5f, 2f, _viewModel.CrosshairSize, 0.1f, ref _crosshairSizeSlider, ref _crosshairValueText, startY - 120f, 2);
        }

        private void CreateSliderRow(string label, string key, float min, float max, float defaultValue, float step,
            ref Slider slider, ref Text valueText, float yPos, int index)
        {
            var labelWidth = 100f;
            var sliderWidth = _contentWidth - labelWidth - 80f;

            var labelGO = new GameObject($"Slider_{key}_Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(_panelTransform, false);
            var labelText = labelGO.GetComponent<Text>();
            labelText.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", bodyFontSize);
            labelText.fontSize = bodyFontSize;
            labelText.alignment = TextAnchor.MiddleRight;
            labelText.color = new Color(0.6f, 0.65f, 0.7f);
            labelText.text = label;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(0, 1);
            labelRect.pivot = new Vector2(1, 1f);
            labelRect.anchoredPosition = new Vector2(_contentLeft + labelWidth, yPos);
            labelRect.sizeDelta = new Vector2(labelWidth, sliderHeight);

            var sliderGO = new GameObject($"Slider_{key}", typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderGO.transform.SetParent(_panelTransform, false);
            slider = sliderGO.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultValue;
            slider.fillRect.GetComponent<Image>().color = sliderFillColor;
            slider.handleRect?.GetComponent<Image>()?.SetColor(accentColor);
            slider.targetGraphic?.GetComponent<Image>()?.SetColor(accentColor);
            slider.wholeNumbers = false;

            var bgGO = new GameObject("SliderBg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgImage = bgGO.GetComponent<Image>();
            bgImage.color = sliderBgColor;
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = new Vector2(1, 1);
            bgRect.sizeDelta = new Vector2(0, 0);

            var handleGO = new GameObject("SliderHandle", typeof(RectTransform), typeof(Image));
            handleGO.transform.SetParent(sliderGO.transform, false);
            var handleImage = handleGO.GetComponent<Image>();
            handleImage.color = accentColor;
            var handleRect = handleGO.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16, sliderHeight);
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            var sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 1);
            sliderRect.anchorMax = new Vector2(0, 1);
            sliderRect.pivot = new Vector2(0, 1f);
            sliderRect.anchoredPosition = new Vector2(_contentLeft + labelWidth + 8, yPos);
            sliderRect.sizeDelta = new Vector2(sliderWidth, sliderHeight);

            var valueGO = new GameObject($"Slider_{key}_Value", typeof(RectTransform), typeof(Text));
            valueGO.transform.SetParent(_panelTransform, false);
            valueText = valueGO.GetComponent<Text>();
            valueText.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", bodyFontSize);
            valueText.fontSize = bodyFontSize;
            valueText.alignment = TextAnchor.MiddleLeft;
            valueText.color = accentColor;
            valueText.text = FormatSliderValue(key, defaultValue);
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var valueRect = valueGO.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 1);
            valueRect.anchorMax = new Vector2(0, 1);
            valueRect.pivot = new Vector2(0, 1f);
            valueRect.anchoredPosition = new Vector2(_contentLeft + labelWidth + sliderWidth + 12, yPos);
            valueRect.sizeDelta = new Vector2(60, sliderHeight);

            var capturedValueText = valueText;
            slider.onValueChanged.AddListener(v => OnSliderValueChanged(key, v, capturedValueText));
        }

        private void OnSliderValueChanged(string key, float value, Text valueText)
        {
            if (_viewModel == null) return;
            switch (key)
            {
                case "fov":
                    _viewModel.Fov = value;
                    valueText.text = Mathf.RoundToInt(value).ToString();
                    ApplyVisualSetting("Fov", value);
                    break;
                case "sensitivity":
                    _viewModel.Sensitivity = value;
                    valueText.text = value.ToString("F1");
                    ApplyVisualSetting("Sensitivity", value);
                    break;
                case "crosshair":
                    _viewModel.CrosshairSize = value;
                    valueText.text = value.ToString("F1");
                    ApplyVisualSetting("CrosshairSize", value);
                    break;
            }
        }

        private void UpdateSliderDisplay(string key, float value, ref Slider slider, ref Text valueText)
        {
            if (slider != null) slider.value = value;
            if (valueText != null) valueText.text = FormatSliderValue(key, value);
        }

        private void ApplyVisualSetting(string setting, float value)
        {
            var camera = Camera.main;
            if (camera != null && setting == "Fov") camera.fieldOfView = value;
        }

        private string FormatSliderValue(string key, float value)
        {
            return key switch
            {
                "fov" => Mathf.RoundToInt(value).ToString(),
                "sensitivity" or "crosshair" => value.ToString("F1"),
                _ => value.ToString()
            };
        }

    }
}
