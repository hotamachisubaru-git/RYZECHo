using System;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo.UI.ViewModels
{
    /// <summary>
    /// 表示設定（Display Settings）のViewModel。
    /// FOV、マウス感度、クロスヘア設定、HUD表示オプションを管理。
    /// </summary>
    public class DisplaySettingsViewModel : IDisposable
    {
        #region Events

        public event Action<float> OnFovChanged;
        public event Action<float> OnSensitivityChanged;
        public event Action<float> OnCrosshairSizeChanged;
        public event Action<bool> OnCrosshairVisibleChanged;
        public event Action<bool> OnHealthBarVisibleChanged;
        public event Action<bool> OnShieldBarVisibleChanged;
        public event Action<bool> OnMinimapVisibleChanged;
        public event Action<bool> OnDamageNumbersVisibleChanged;

        #endregion

        #region Private Fields

        private float _fov = 100f;
        private float _sensitivity = 1.0f;
        private float _crosshairSize = 1.0f;
        private bool _crosshairVisible = true;
        private bool _healthBarVisible = true;
        private bool _shieldBarVisible = true;
        private bool _minimapVisible = true;
        private bool _damageNumbersVisible = true;

        // Default values
        private const float DefaultFov = 100f;
        private const float DefaultSensitivity = 1.0f;
        private const float DefaultCrosshairSize = 1.0f;
        private const bool DefaultCrosshairVisible = true;
        private const bool DefaultHealthBarVisible = true;
        private const bool DefaultShieldBarVisible = true;
        private const bool DefaultMinimapVisible = true;
        private const bool DefaultDamageNumbersVisible = true;

        private readonly string _settingsFilePath;

        #endregion

        #region Properties

        public float Fov
        {
            get => _fov;
            set
            {
                if (_fov != value)
                {
                    _fov = Mathf.Clamp(value, 60f, 120f);
                    OnFovChanged?.Invoke(_fov);
                }
            }
        }

        public float Sensitivity
        {
            get => _sensitivity;
            set
            {
                if (_sensitivity != value)
                {
                    _sensitivity = Mathf.Clamp(value, 0.1f, 3.0f);
                    OnSensitivityChanged?.Invoke(_sensitivity);
                }
            }
        }

        public float CrosshairSize
        {
            get => _crosshairSize;
            set
            {
                if (_crosshairSize != value)
                {
                    _crosshairSize = Mathf.Clamp(value, 0.5f, 2.0f);
                    OnCrosshairSizeChanged?.Invoke(_crosshairSize);
                }
            }
        }

        public bool CrosshairVisible
        {
            get => _crosshairVisible;
            set
            {
                if (_crosshairVisible != value)
                {
                    _crosshairVisible = value;
                    OnCrosshairVisibleChanged?.Invoke(_crosshairVisible);
                }
            }
        }

        public bool HealthBarVisible
        {
            get => _healthBarVisible;
            set
            {
                if (_healthBarVisible != value)
                {
                    _healthBarVisible = value;
                    OnHealthBarVisibleChanged?.Invoke(_healthBarVisible);
                }
            }
        }

        public bool ShieldBarVisible
        {
            get => _shieldBarVisible;
            set
            {
                if (_shieldBarVisible != value)
                {
                    _shieldBarVisible = value;
                    OnShieldBarVisibleChanged?.Invoke(_shieldBarVisible);
                }
            }
        }

        public bool MinimapVisible
        {
            get => _minimapVisible;
            set
            {
                if (_minimapVisible != value)
                {
                    _minimapVisible = value;
                    OnMinimapVisibleChanged?.Invoke(_minimapVisible);
                }
            }
        }

        public bool DamageNumbersVisible
        {
            get => _damageNumbersVisible;
            set
            {
                if (_damageNumbersVisible != value)
                {
                    _damageNumbersVisible = value;
                    OnDamageNumbersVisibleChanged?.Invoke(_damageNumbersVisible);
                }
            }
        }

        #endregion

        #region Constructor

        public DisplaySettingsViewModel()
        {
            _settingsFilePath = $"{Application.persistentDataPath}/RYZECHo/Settings/visual.json";
            Load();
        }

        #endregion

        #region Load / Save

        /// <summary>
        /// 設定ファイルから読み込む
        /// </summary>
        public void Load()
        {
            try
            {
                if (System.IO.File.Exists(_settingsFilePath))
                {
                    var json = System.IO.File.ReadAllText(_settingsFilePath);
                    var data = JsonUtilityFromNewtonton.Parse<VisualSettingsData>(json);
                    if (data != null)
                    {
                        _fov = data.fov;
                        _sensitivity = data.sensitivity;
                        _crosshairSize = data.crosshairSize;
                        _crosshairVisible = data.crosshairVisible;
                        _healthBarVisible = data.healthBarVisible;
                        _shieldBarVisible = data.shieldBarVisible;
                        _minimapVisible = data.minimapVisible;
                        _damageNumbersVisible = data.damageNumbersVisible;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DisplaySettingsViewModel] Failed to load settings: {e.Message}");
            }
        }

        /// <summary>
        /// 設定をファイルに保存する
        /// </summary>
        public void Save()
        {
            try
            {
                var basePath = $"{Application.persistentDataPath}/RYZECHo/Settings/";
                System.IO.Directory.CreateDirectory(basePath);

                var data = new VisualSettingsData
                {
                    fov = _fov,
                    sensitivity = _sensitivity,
                    crosshairSize = _crosshairSize,
                    crosshairVisible = _crosshairVisible,
                    healthBarVisible = _healthBarVisible,
                    shieldBarVisible = _shieldBarVisible,
                    minimapVisible = _minimapVisible,
                    damageNumbersVisible = _damageNumbersVisible,
                };

                var json = JsonUtility.ToJson(data, true);
                System.IO.File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DisplaySettingsViewModel] Failed to save settings: {e.Message}");
            }
        }

        /// <summary>
        /// デフォルト値にリセットする
        /// </summary>
        public void ResetToDefaults()
        {
            _fov = DefaultFov;
            _sensitivity = DefaultSensitivity;
            _crosshairSize = DefaultCrosshairSize;
            _crosshairVisible = DefaultCrosshairVisible;
            _healthBarVisible = DefaultHealthBarVisible;
            _shieldBarVisible = DefaultShieldBarVisible;
            _minimapVisible = DefaultMinimapVisible;
            _damageNumbersVisible = DefaultDamageNumbersVisible;

            OnFovChanged?.Invoke(_fov);
            OnSensitivityChanged?.Invoke(_sensitivity);
            OnCrosshairSizeChanged?.Invoke(_crosshairSize);
            OnCrosshairVisibleChanged?.Invoke(_crosshairVisible);
            OnHealthBarVisibleChanged?.Invoke(_healthBarVisible);
            OnShieldBarVisibleChanged?.Invoke(_shieldBarVisible);
            OnMinimapVisibleChanged?.Invoke(_minimapVisible);
            OnDamageNumbersVisibleChanged?.Invoke(_damageNumbersVisible);
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                Save();
                OnFovChanged = null;
                OnSensitivityChanged = null;
                OnCrosshairSizeChanged = null;
                OnCrosshairVisibleChanged = null;
                OnHealthBarVisibleChanged = null;
                OnShieldBarVisibleChanged = null;
                OnMinimapVisibleChanged = null;
                OnDamageNumbersVisibleChanged = null;
                _disposed = true;
            }
        }

        #endregion

        #region Serializable Data

        [System.Serializable]
        private class VisualSettingsData
        {
            public float fov;
            public float sensitivity;
            public float crosshairSize;
            public bool crosshairVisible;
            public bool healthBarVisible;
            public bool shieldBarVisible;
            public bool minimapVisible;
            public bool damageNumbersVisible;
        }

        #endregion
    }

    /// <summary>
    /// JsonUtilityでNewtonsoft.Jsonの型をシリアライズするためのヘルパークラス
    /// </summary>
    internal static class JsonUtilityFromNewtonton
    {
        public static T Parse<T>(string json) where T : new()
        {
            var data = new T();
            var fields = typeof(T).GetFields();
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start < 0 || end < 0) return data;

            var inner = json.Substring(start + 1, end - start - 1);
            foreach (var field in fields)
            {
                var key = $"\"{field.Name}\"";
                var idx = inner.IndexOf(key);
                if (idx >= 0)
                {
                    var afterColon = inner.IndexOf(':', idx + key.Length);
                    var valueStr = inner.Substring(afterColon + 1);
                    var commaIdx = valueStr.IndexOf(',');
                    if (commaIdx > 0) valueStr = valueStr.Substring(0, commaIdx).Trim();
                    else valueStr = valueStr.Trim().Trim('"');

                    // Remove surrounding quotes for string values
                    if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
                        valueStr = valueStr.Trim('"');

                    try
                    {
                        if (field.FieldType == typeof(float))
                            field.SetValue(data, float.Parse(valueStr));
                        else if (field.FieldType == typeof(bool))
                            field.SetValue(data, valueStr == "true");
                    }
                    catch { }
                }
            }
            return data;
        }
    }
}
