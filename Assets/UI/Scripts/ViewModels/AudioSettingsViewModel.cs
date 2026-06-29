using System;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo.UI.ViewModels
{
    /// <summary>
    /// 音声設定（Audio Settings）のViewModel。
    /// マスター/BGM/SE/ボイス/環境の各音量を管理。
    /// </summary>
    public class AudioSettingsViewModel : IDisposable
    {
        #region Events

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;
        public event Action<float> OnVoiceVolumeChanged;
        public event Action<float> OnEnvVolumeChanged;

        #endregion

        #region Private Fields

        private float _masterVolume = 1.0f;
        private float _musicVolume = 1.0f;
        private float _sfxVolume = 1.0f;
        private float _voiceVolume = 1.0f;
        private float _envVolume = 0.8f;

        // Default values
        private const float DefaultMasterVolume = 1.0f;
        private const float DefaultMusicVolume = 1.0f;
        private const float DefaultSfxVolume = 1.0f;
        private const float DefaultVoiceVolume = 1.0f;
        private const float DefaultEnvVolume = 0.8f;

        private readonly string _settingsFilePath;

        #endregion

        #region Properties

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                if (_masterVolume != value)
                {
                    _masterVolume = Mathf.Clamp01(value);
                    OnMasterVolumeChanged?.Invoke(_masterVolume);
                }
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                if (_musicVolume != value)
                {
                    _musicVolume = Mathf.Clamp01(value);
                    OnMusicVolumeChanged?.Invoke(_musicVolume);
                }
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                if (_sfxVolume != value)
                {
                    _sfxVolume = Mathf.Clamp01(value);
                    OnSfxVolumeChanged?.Invoke(_sfxVolume);
                }
            }
        }

        public float VoiceVolume
        {
            get => _voiceVolume;
            set
            {
                if (_voiceVolume != value)
                {
                    _voiceVolume = Mathf.Clamp01(value);
                    OnVoiceVolumeChanged?.Invoke(_voiceVolume);
                }
            }
        }

        public float EnvVolume
        {
            get => _envVolume;
            set
            {
                if (_envVolume != value)
                {
                    _envVolume = Mathf.Clamp01(value);
                    OnEnvVolumeChanged?.Invoke(_envVolume);
                }
            }
        }

        #endregion

        #region Constructor

        public AudioSettingsViewModel()
        {
            _settingsFilePath = $"{Application.persistentDataPath}/RYZECHo/Settings/audio.json";
            Load();
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
                    var data = JsonUtility.FromJson<AudioSettingsData>(json);
                    if (data != null)
                    {
                        _masterVolume = data.masterVolume;
                        _musicVolume = data.musicVolume;
                        _sfxVolume = data.sfxVolume;
                        _voiceVolume = data.voiceVolume;
                        _envVolume = data.environmentVolume;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AudioSettingsViewModel] Failed to load settings: {e.Message}");
            }
        }

        public void Save()
        {
            try
            {
                var basePath = $"{Application.persistentDataPath}/RYZECHo/Settings/";
                System.IO.Directory.CreateDirectory(basePath);

                var data = new AudioSettingsData
                {
                    masterVolume = _masterVolume,
                    musicVolume = _musicVolume,
                    sfxVolume = _sfxVolume,
                    voiceVolume = _voiceVolume,
                    environmentVolume = _envVolume,
                };

                var json = JsonUtility.ToJson(data, true);
                System.IO.File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioSettingsViewModel] Failed to save settings: {e.Message}");
            }
        }

        public void ResetToDefaults()
        {
            _masterVolume = DefaultMasterVolume;
            _musicVolume = DefaultMusicVolume;
            _sfxVolume = DefaultSfxVolume;
            _voiceVolume = DefaultVoiceVolume;
            _envVolume = DefaultEnvVolume;

            OnMasterVolumeChanged?.Invoke(_masterVolume);
            OnMusicVolumeChanged?.Invoke(_musicVolume);
            OnSfxVolumeChanged?.Invoke(_sfxVolume);
            OnVoiceVolumeChanged?.Invoke(_voiceVolume);
            OnEnvVolumeChanged?.Invoke(_envVolume);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// 音量値をデシベルに変換
        /// </summary>
        public static float VolumeToDecibel(float linearVolume)
        {
            return Mathf.Log10(Mathf.Max(linearVolume, 0.001f)) * 20f;
        }

        /// <summary>
        /// 音量値をパーセント文字列に変換
        /// </summary>
        public static string FormatVolume(float volume)
        {
            return Mathf.RoundToInt(volume * 100).ToString() + "%";
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                Save();
                OnMasterVolumeChanged = null;
                OnMusicVolumeChanged = null;
                OnSfxVolumeChanged = null;
                OnVoiceVolumeChanged = null;
                OnEnvVolumeChanged = null;
                _disposed = true;
            }
        }

        #endregion

        #region Serializable Data

        [System.Serializable]
        private class AudioSettingsData
        {
            public float masterVolume;
            public float musicVolume;
            public float sfxVolume;
            public float voiceVolume;
            public float environmentVolume;
        }

        #endregion
    }
}
