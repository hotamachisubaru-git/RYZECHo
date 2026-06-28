using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.Audio;
using UnityEngine.UI;
using RYZECHo.UI.ViewModels;

namespace RYZECHo.UI
{
    /// <summary>
    /// BGM/SE/ボイス/環境の音量スライダーを管理するサブパネル
    /// </summary>
    public class AudioMusicPanel : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioSettingsSO _audioSettings;
        [SerializeField] private AudioMixer _masterMixer;

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

        private Slider _musicSlider;
        private Slider _sfxSlider;
        private Slider _voiceSlider;
        private Slider _envSlider;
        private Text _musicValueText;
        private Text _sfxValueText;
        private Text _voiceValueText;
        private Text _envValueText;
        private Transform _panelTransform;
        private AudioSettingsViewModel _viewModel;

        public float MusicVolume => _viewModel?.MusicVolume ?? 1.0f;
        public float SfxVolume => _viewModel?.SfxVolume ?? 1.0f;
        public float VoiceVolume => _viewModel?.VoiceVolume ?? 1.0f;
        public float EnvVolume => _viewModel?.EnvVolume ?? 0.8f;

        private void Awake()
        {
            _viewModel = new AudioSettingsViewModel();
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
            _viewModel.OnMusicVolumeChanged += v => UpdateSliderDisplay("music", v, ref _musicSlider, ref _musicValueText);
            _viewModel.OnSfxVolumeChanged += v => UpdateSliderDisplay("sfx", v, ref _sfxSlider, ref _sfxValueText);
            _viewModel.OnVoiceVolumeChanged += v => UpdateSliderDisplay("voice", v, ref _voiceSlider, ref _voiceValueText);
            _viewModel.OnEnvVolumeChanged += v => UpdateSliderDisplay("environment", v, ref _envSlider, ref _envValueText);
        }

        private void UnbindViewModelEvents()
        {
            if (_viewModel == null) return;
            _viewModel.OnMusicVolumeChanged -= null;
            _viewModel.OnSfxVolumeChanged -= null;
            _viewModel.OnVoiceVolumeChanged -= null;
            _viewModel.OnEnvVolumeChanged -= null;
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
            CreateSliderRow("BGM音量", "music", 0f, 1f, _viewModel.MusicVolume, 0.1f, ref _musicSlider, ref _musicValueText, startY - 40f, 0);
            CreateSliderRow("効果音音量", "sfx", 0f, 1f, _viewModel.SfxVolume, 0.1f, ref _sfxSlider, ref _sfxValueText, startY - 80f, 1);
            CreateSliderRow("ボイス音量", "voice", 0f, 1f, _viewModel.VoiceVolume, 0.1f, ref _voiceSlider, ref _voiceValueText, startY - 120f, 2);
            CreateSliderRow("環境音量", "environment", 0f, 1f, _viewModel.EnvVolume, 0.1f, ref _envSlider, ref _envValueText, startY - 160f, 3);
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
                case "music":
                    _viewModel.MusicVolume = value;
                    valueText.text = FormatVolume(value);
                    ApplyAudioSetting("Music", value);
                    break;
                case "sfx":
                    _viewModel.SfxVolume = value;
                    valueText.text = FormatVolume(value);
                    ApplyAudioSetting("SFX", value);
                    break;
                case "voice":
                    _viewModel.VoiceVolume = value;
                    valueText.text = FormatVolume(value);
                    ApplyAudioSetting("Voice", value);
                    break;
                case "environment":
                    _viewModel.EnvVolume = value;
                    valueText.text = FormatVolume(value);
                    ApplyAudioSetting("Environment", value);
                    break;
            }
        }

        private void UpdateSliderDisplay(string key, float value, ref Slider slider, ref Text valueText)
        {
            if (slider != null) slider.value = value;
            if (valueText != null) valueText.text = FormatSliderValue(key, value);
        }

        private void ApplyAudioSetting(string category, float value)
        {
            if (_audioSettings == null) return;
            switch (category)
            {
                case "Music": _audioSettings.MusicVolume = value; break;
                case "SFX": _audioSettings.SfxVolume = value; break;
                case "Voice": _audioSettings.VoiceVolume = value; break;
                case "Environment": _audioSettings.EnvironmentVolume = value; break;
            }
            if (_masterMixer != null)
            {
                var dbValue = Mathf.Log10(Mathf.Max(value, 0.001f)) * 20f;
                _masterMixer.SetFloat("Volume", dbValue);
            }
        }

        private string FormatVolume(float volume) => Mathf.RoundToInt(volume * 100).ToString() + "%";

        private string FormatSliderValue(string key, float value) => FormatVolume(value);

    }
}
