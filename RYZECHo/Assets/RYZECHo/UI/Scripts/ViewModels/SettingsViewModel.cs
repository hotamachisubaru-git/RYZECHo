using System;
using System.Collections.Generic;

namespace RYZECHo.UI.ViewModels
{
    /// <summary>
    /// Settings画面全体のViewModel。
    /// 現在選択中のタブ、各タブのViewModelを管理する。
    /// </summary>
    public class SettingsViewModel : IDisposable
    {
        public enum TabType
        {
            Audio,
            Display,
            KeyConfig
        }

        private TabType _currentTab;
        private AudioSettingsViewModel _audioSettings;
        private DisplaySettingsViewModel _displaySettings;
        private KeyBindingViewModel _keyBinding;

        public TabType CurrentTab => _currentTab;
        public AudioSettingsViewModel AudioSettings => _audioSettings ??= new AudioSettingsViewModel();
        public DisplaySettingsViewModel DisplaySettings => _displaySettings ??= new DisplaySettingsViewModel();
        public KeyBindingViewModel KeyBinding => _keyBinding ??= new KeyBindingViewModel();

        public event Action<TabType> OnTabChanged;

        public SettingsViewModel()
        {
            _currentTab = TabType.Audio;
        }

        /// <summary>
        /// タブを切り替える
        /// </summary>
        public void SetTab(TabType tab)
        {
            if (_currentTab != tab)
            {
                _currentTab = tab;
                OnTabChanged?.Invoke(tab);
            }
        }

        /// <summary>
        /// 設定をすべて保存する
        /// </summary>
        public void SaveAllSettings()
        {
            _audioSettings?.Save();
            _displaySettings?.Save();
            _keyBinding?.Save();
        }

        /// <summary>
        /// 設定をデフォルト値にリセットする
        /// </summary>
        public void ResetAllSettings()
        {
            _audioSettings?.ResetToDefaults();
            _displaySettings?.ResetToDefaults();
            _keyBinding?.ResetToDefaults();
        }

        public void Dispose()
        {
            _audioSettings?.Dispose();
            _displaySettings?.Dispose();
            _keyBinding?.Dispose();
        }
    }
}
