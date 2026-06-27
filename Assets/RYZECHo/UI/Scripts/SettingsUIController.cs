using UnityEngine;
using UnityEngine.UI;
using RYZECHo.UI.ViewModels;

namespace RYZECHo.UI
{
    /// <summary>
    /// Settings画面の全体を制御するコントローラ。
    /// タブ切り替え、各タブのPanelとViewModelの連携、設定の保存を管理する。
    /// </summary>
    public class SettingsUIController : MonoBehaviour
    {
        #region Serializable Fields

        [Header("Tab Buttons")]
        [SerializeField] private Button _audioTabButton;
        [SerializeField] private Button _displayTabButton;
        [SerializeField] private Button _keyConfigTabButton;

        [Header("Panel Containers")]
        [SerializeField] private RectTransform _audioPanelContainer;
        [SerializeField] private RectTransform _displayPanelContainer;
        [SerializeField] private RectTransform _keyConfigPanelContainer;

        [Header("Panel References (optional - will be created if missing)")]
        [SerializeField] private AudioSettingsPanel _audioPanelPrefab;
        [SerializeField] private DisplaySettingsPanel _displayPanelPrefab;
        [SerializeField] private KeyConfigPanel _keyConfigPanelPrefab;

        [Header("UI Configuration")]
        [Tooltip("Content area left position")]
        public float contentLeft = 200f;
        [Tooltip("Content area width")]
        public float contentWidth = 600f;
        [Tooltip("Content area top position")]
        public float contentTop = -50f;

        #endregion

        #region Private Fields

        private SettingsViewModel _viewModel;
        private AudioSettingsPanel _activeAudioPanel;
        private DisplaySettingsPanel _activeDisplayPanel;
        private KeyConfigPanel _activeKeyConfigPanel;
        private SettingsViewModel.TabType _currentTab;
        private Button _activeTabButton;

        #endregion

        #region Properties

        public SettingsViewModel ViewModel => _viewModel;
        public SettingsViewModel.TabType CurrentTab => _currentTab;
        public AudioSettingsPanel AudioPanel => _activeAudioPanel;
        public DisplaySettingsPanel DisplayPanel => _activeDisplayPanel;
        public KeyConfigPanel KeyConfigPanel => _activeKeyConfigPanel;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _viewModel = new SettingsViewModel();
            InitializePanels();
            SetupTabButtons();
            SetTab(SettingsViewModel.TabType.Audio);
        }

        private void OnDestroy()
        {
            _viewModel?.SaveAllSettings();
            _viewModel?.Dispose();
        }

        #endregion

        #region Initialization

        private void InitializePanels()
        {
            // Initialize Audio Panel
            if (_audioPanelPrefab != null)
            {
                var audioGO = Instantiate(_audioPanelPrefab, _audioPanelContainer);
                _activeAudioPanel = audioGO.GetComponent<AudioSettingsPanel>();
                _activeAudioPanel.Initialize(_audioPanelContainer, contentLeft, contentWidth, contentTop);
            }

            // Initialize Display Panel
            if (_displayPanelPrefab != null)
            {
                var displayGO = Instantiate(_displayPanelPrefab, _displayPanelContainer);
                _activeDisplayPanel = displayGO.GetComponent<DisplaySettingsPanel>();
                _activeDisplayPanel.Initialize(_displayPanelContainer, contentLeft, contentWidth, contentTop);
            }

            // Initialize Key Config Panel
            if (_keyConfigPanelPrefab != null)
            {
                var keyConfigGO = Instantiate(_keyConfigPanelPrefab, _keyConfigPanelContainer);
                _activeKeyConfigPanel = keyConfigGO.GetComponent<KeyConfigPanel>();
                _activeKeyConfigPanel.Initialize(_keyConfigPanelContainer, contentLeft, contentWidth, contentTop);
            }
        }

        private void SetupTabButtons()
        {
            if (_audioTabButton != null)
                _audioTabButton.onClick.AddListener(() => SetTab(SettingsViewModel.TabType.Audio));
            if (_displayTabButton != null)
                _displayTabButton.onClick.AddListener(() => SetTab(SettingsViewModel.TabType.Display));
            if (_keyConfigTabButton != null)
                _keyConfigTabButton.onClick.AddListener(() => SetTab(SettingsViewModel.TabType.KeyConfig));

            _viewModel.OnTabChanged += OnTabChanged;
        }

        #endregion

        #region Tab Management

        public void SetTab(SettingsViewModel.TabType tab)
        {
            if (_currentTab == tab) return;

            _viewModel.SetTab(tab);
            _currentTab = tab;
            UpdateTabVisuals();
        }

        private void OnTabChanged(SettingsViewModel.TabType tab)
        {
            _currentTab = tab;
            UpdateTabVisuals();
        }

        private void UpdateTabVisuals()
        {
            // Toggle panel visibility
            if (_audioPanelContainer != null) _audioPanelContainer.gameObject.SetActive(_currentTab == SettingsViewModel.TabType.Audio);
            if (_displayPanelContainer != null) _displayPanelContainer.gameObject.SetActive(_currentTab == SettingsViewModel.TabType.Display);
            if (_keyConfigPanelContainer != null) _keyConfigPanelContainer.gameObject.SetActive(_currentTab == SettingsViewModel.TabType.KeyConfig);

            // Update button visuals
            UpdateTabButton(_audioTabButton, _currentTab == SettingsViewModel.TabType.Audio);
            UpdateTabButton(_displayTabButton, _currentTab == SettingsViewModel.TabType.Display);
            UpdateTabButton(_keyConfigTabButton, _currentTab == SettingsViewModel.TabType.KeyConfig);
        }

        private void UpdateTabButton(Button button, bool isActive)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = isActive ? new Color(0.24f, 0.55f, 0.97f) : new Color(0.15f, 0.2f, 0.3f);
            }
        }

        #endregion

        #region Save / Reset

        public void SaveAllSettings()
        {
            _viewModel?.SaveAllSettings();
        }

        public void ResetAllSettings()
        {
            _viewModel?.ResetAllSettings();
            RefreshCurrentPanel();
        }

        private void RefreshCurrentPanel()
        {
            switch (_currentTab)
            {
                case SettingsViewModel.TabType.Audio:
                    RefreshAudioPanel();
                    break;
                case SettingsViewModel.TabType.Display:
                    RefreshDisplayPanel();
                    break;
                case SettingsViewModel.TabType.KeyConfig:
                    RefreshKeyConfigPanel();
                    break;
            }
        }

        #endregion

        #region Panel Refresh Methods

        public void RefreshAudioPanel()
        {
            if (_activeAudioPanel == null || _viewModel == null) return;
            var vm = _viewModel.AudioSettings;

            // Update slider values from ViewModel
            UpdateSliderValue(_activeAudioPanel, "master", vm.MasterVolume);
            UpdateSliderValue(_activeAudioPanel, "music", vm.MusicVolume);
            UpdateSliderValue(_activeAudioPanel, "sfx", vm.SfxVolume);
            UpdateSliderValue(_activeAudioPanel, "voice", vm.VoiceVolume);
            UpdateSliderValue(_activeAudioPanel, "environment", vm.EnvVolume);
        }

        public void RefreshDisplayPanel()
        {
            if (_activeDisplayPanel == null || _viewModel == null) return;
            var vm = _viewModel.DisplaySettings;

            UpdateSliderValue(_activeDisplayPanel, "fov", vm.Fov);
            UpdateSliderValue(_activeDisplayPanel, "sensitivity", vm.Sensitivity);
            UpdateSliderValue(_activeDisplayPanel, "crosshair", vm.CrosshairSize);
        }

        public void RefreshKeyConfigPanel()
        {
            if (_activeKeyConfigPanel == null || _viewModel == null) return;
            _activeKeyConfigPanel.RefreshUI();
        }

        private void UpdateSliderValue(MonoBehaviour panel, string key, float value)
        {
            // Use reflection to update slider values
            var sliderField = panel.GetType().GetField($"_{key}Slider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sliderField?.GetValue(panel) is Slider slider)
            {
                slider.value = value;
            }
        }

        #endregion
    }
}
