using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo.UI.ViewModels
{
    /// <summary>
    /// キーコンフィグのViewModel。
    /// キーバインディングのマッピング、記録状態、選択状態を管理。
    /// </summary>
    public class KeyBindingViewModel : IDisposable
    {
        #region Events

        public event Action<int> OnSelectedIndexChanged;
        public event Action<string> OnKeybindChanged;
        public event Action<int> OnKeyRecordingStarted;
        public event Action<int> OnKeyRecordingCompleted;
        public event Action<int> OnKeyRecordingCancelled;

        #endregion

        #region Nested Types

        /// <summary>
        /// 1つのキーバインディングを表すViewModel。
        /// </summary>
        public class KeybindEntryViewModel
        {
            public string ActionKey { get; set; }
            public string Label { get; set; }
            public KeyCode DefaultValue { get; set; }
            public KeyCode CurrentValue { get; set; }
            public bool IsRecording { get; set; }

            public string DisplayKey => IsRecording ? "..." : CurrentValue.ToString();
        }

        #endregion

        #region Private Fields

        private List<KeybindEntryViewModel> _keybinds;
        private int _selectedIndex = -1;
        private readonly string _settingsFilePath;

        // デフォルトキーバインディング定義
        private static readonly Dictionary<string, KeyCode> DefaultKeybinds = new()
        {
            { "MoveUp", KeyCode.W },
            { "MoveLeft", KeyCode.A },
            { "MoveDown", KeyCode.S },
            { "MoveRight", KeyCode.D },
            { "AdjustBetLeft", KeyCode.Q },
            { "AdjustBetRight", KeyCode.E },
            { "Confirm", KeyCode.Space },
            { "Press1", KeyCode.Alpha1 },
            { "Press2", KeyCode.Alpha2 },
            { "Press3", KeyCode.Alpha3 },
            { "Press4", KeyCode.Alpha4 },
            { "Press5", KeyCode.Alpha5 },
            { "Press6", KeyCode.Alpha6 },
            { "Fire", KeyCode.Mouse0 },
            { "Interact", KeyCode.Mouse1 },
            { "Pause", KeyCode.Escape },
            { "ToggleBuildPanel", KeyCode.B },
            { "ToggleLoadoutPanel", KeyCode.L },
            { "ToggleStatusPanel", KeyCode.Tab },
            { "ToggleMap", KeyCode.M },
            { "Ping", KeyCode.G },
        };

        #endregion

        #region Properties

        public IReadOnlyList<KeybindEntryViewModel> Keybinds => _keybinds;
        public int SelectedIndex
        {
            get => _selectedIndex;
            private set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    OnSelectedIndexChanged?.Invoke(_selectedIndex);
                }
            }
        }

        public int Count => _keybinds.Count;

        public bool IsRecording => _selectedIndex >= 0 && _selectedIndex < _keybinds.Count
            && _keybinds[_selectedIndex].IsRecording;

        #endregion

        #region Constructor

        public KeyBindingViewModel()
        {
            _settingsFilePath = $"{Application.persistentDataPath}/RYZECHo/Settings/keybinds.json";
            LoadDefaultKeybinds();
            Load();
        }

        #endregion

        #region Initialization

        private void LoadDefaultKeybinds()
        {
            _keybinds = new List<KeybindEntryViewModel>();
            foreach (var kvp in DefaultKeybinds)
            {
                _keybinds.Add(new KeybindEntryViewModel
                {
                    ActionKey = kvp.Key,
                    Label = GetKeybindLabel(kvp.Key),
                    DefaultValue = kvp.Value,
                    CurrentValue = kvp.Value,
                    IsRecording = false,
                });
            }
        }

        private string GetKeybindLabel(string key)
        {
            return key switch
            {
                "MoveUp" => "移動（上）",
                "MoveLeft" => "移動（左）",
                "MoveDown" => "移動（下）",
                "MoveRight" => "移動（右）",
                "AdjustBetLeft" => "ベット減",
                "AdjustBetRight" => "ベット増",
                "Confirm" => "確定",
                "Press1" => "選択 1",
                "Press2" => "選択 2",
                "Press3" => "選択 3",
                "Press4" => "選択 4",
                "Press5" => "選択 5",
                "Press6" => "選択 6",
                "Fire" => "射撃",
                "Interact" => "インタラクト",
                "Pause" => "ポーズ",
                "ToggleBuildPanel" => "ビルドパネル",
                "ToggleLoadoutPanel" => "ロードアウトパネル",
                "ToggleStatusPanel" => "ステータスパネル",
                "ToggleMap" => "マップ",
                "Ping" => "ピン",
                _ => key,
            };
        }

        #endregion

        #region Load / Save

        public void Load()
        {
            try
            {
                if (System.IO.File.Exists(_settingsFilePath))
                {
                    var json = System.IO.File.ReadAllText(_settingsFilePath);
                    var data = JsonUtility.FromJson<KeybindData>(json);
                    if (data != null)
                    {
                        foreach (var kvp in data.keybinds)
                        {
                            var entry = _keybinds.FirstOrDefault(e => e.ActionKey == kvp.Key);
                            if (entry != null && Enum.TryParse<KeyCode>(kvp.Value, out var keyCode))
                            {
                                entry.CurrentValue = keyCode;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[KeyBindingViewModel] Failed to load keybinds: {e.Message}");
            }
        }

        public void Save()
        {
            try
            {
                var basePath = $"{Application.persistentDataPath}/RYZECHo/Settings/";
                System.IO.Directory.CreateDirectory(basePath);

                var keybindsDict = _keybinds.ToDictionary(
                    e => e.ActionKey,
                    e => e.CurrentValue.ToString()
                );
                var data = new KeybindData { keybinds = keybindsDict };
                var json = JsonUtility.ToJson(data, true);
                System.IO.File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[KeyBindingViewModel] Failed to save keybinds: {e.Message}");
            }
        }

        public void ResetToDefaults()
        {
            for (int i = 0; i < _keybinds.Count; i++)
            {
                var entry = _keybinds[i];
                if (entry.CurrentValue != entry.DefaultValue)
                {
                    entry.CurrentValue = entry.DefaultValue;
                    OnKeybindChanged?.Invoke(entry.ActionKey);
                }
            }
        }

        #endregion

        #region Key Recording

        /// <summary>
        /// キー記録を開始する
        /// </summary>
        public bool StartKeyRecording()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _keybinds.Count) return false;

            _keybinds[_selectedIndex].IsRecording = true;
            OnKeyRecordingStarted?.Invoke(_selectedIndex);
            return true;
        }

        /// <summary>
        /// キー記録をキャンセルする
        /// </summary>
        public void CancelKeyRecording()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _keybinds.Count) return;

            _keybinds[_selectedIndex].IsRecording = false;
            OnKeyRecordingCancelled?.Invoke(_selectedIndex);
        }

        /// <summary>
        /// キー記録を確定する
        /// </summary>
        public void CompleteKeyRecording(KeyCode newKey)
        {
            if (_selectedIndex < 0 || _selectedIndex >= _keybinds.Count) return;

            _keybinds[_selectedIndex].CurrentValue = newKey;
            _keybinds[_selectedIndex].IsRecording = false;
            OnKeyRecordingCompleted?.Invoke(_selectedIndex);
            OnKeybindChanged?.Invoke(_keybinds[_selectedIndex].ActionKey);
        }

        #endregion

        #region Navigation

        public void NavigateSelected(int direction)
        {
            var newIndex = _selectedIndex + direction;
            if (newIndex >= 0 && newIndex < _keybinds.Count)
            {
                SelectedIndex = newIndex;
            }
        }

        public void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < _keybinds.Count)
            {
                SelectedIndex = index;
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                Save();
                OnSelectedIndexChanged = null;
                OnKeybindChanged = null;
                OnKeyRecordingStarted = null;
                OnKeyRecordingCompleted = null;
                OnKeyRecordingCancelled = null;
                _disposed = true;
            }
        }

        #endregion

        #region Serializable Data

        [System.Serializable]
        private class KeybindData
        {
            public System.Collections.Generic.Dictionary<string, string> keybinds;
        }

        #endregion
    }
}
