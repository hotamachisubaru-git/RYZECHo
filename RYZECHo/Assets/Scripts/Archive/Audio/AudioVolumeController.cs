using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace RYZECHo.Audio
{
    /// <summary>
    /// BGM/SFX/ボイスの個別ボリューム管理を行うコントローラー。
    /// シーン別のボリューム設定に対応。
    /// </summary>
    public sealed class AudioVolumeController : IDisposable
    {
        private readonly AudioManager _audioManager;
        private readonly Dictionary<string, SceneVolumeProfile> _sceneProfiles;
        private string _currentScene = string.Empty;
        private float _masterVolume = 1.0f;
        private float _bgmVolume = 1.0f;
        private float _sfxVolume = 1.0f;
        private float _voiceVolume = 1.0f;
        private bool _masterMuted;
        private bool _bgmMuted;
        private bool _sfxMuted;
        private bool _voiceMuted;
        private bool _disposed;

        // Events
        public event Action<float>? OnMasterVolumeChanged;
        public event Action<float>? OnBgmVolumeChanged;
        public event Action<float>? OnSfxVolumeChanged;
        public event Action<float>? OnVoiceVolumeChanged;
        public event Action<bool>? OnMasterMuteChanged;
        public event Action<bool>? OnBgmMuteChanged;
        public event Action<bool>? OnSfxMuteChanged;
        public event Action<bool>? OnVoiceMuteChanged;

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
                ApplyVolumes();
                OnMasterVolumeChanged?.Invoke(_masterVolume);
            }
        }

        public float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
                ApplyVolumes();
                OnBgmVolumeChanged?.Invoke(_bgmVolume);
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
                ApplyVolumes();
                OnSfxVolumeChanged?.Invoke(_sfxVolume);
            }
        }

        public float VoiceVolume
        {
            get => _voiceVolume;
            set
            {
                _voiceVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
                ApplyVolumes();
                OnVoiceVolumeChanged?.Invoke(_voiceVolume);
            }
        }

        public bool IsMasterMuted
        {
            get => _masterMuted;
            set
            {
                _masterMuted = value;
                ApplyVolumes();
                OnMasterMuteChanged?.Invoke(_masterMuted);
            }
        }

        public bool IsBgmMuted
        {
            get => _bgmMuted;
            set
            {
                _bgmMuted = value;
                ApplyVolumes();
                OnBgmMuteChanged?.Invoke(_bgmMuted);
            }
        }

        public bool IsSfxMuted
        {
            get => _sfxMuted;
            set
            {
                _sfxMuted = value;
                ApplyVolumes();
                OnSfxMuteChanged?.Invoke(_sfxMuted);
            }
        }

        public bool IsVoiceMuted
        {
            get => _voiceMuted;
            set
            {
                _voiceMuted = value;
                ApplyVolumes();
                OnVoiceMuteChanged?.Invoke(_voiceMuted);
            }
        }

        public AudioVolumeController(AudioManager audioManager)
        {
            _audioManager = audioManager;
            _sceneProfiles = new Dictionary<string, SceneVolumeProfile>(StringComparer.OrdinalIgnoreCase);
            _sceneProfiles["Default"] = new SceneVolumeProfile(1.0f, 1.0f, 1.0f, 1.0f);
            _sceneProfiles["Menu"] = new SceneVolumeProfile(1.0f, 1.0f, 1.0f, 0.0f);
            _sceneProfiles["Game"] = new SceneVolumeProfile(1.0f, 0.8f, 1.0f, 0.6f);
            _sceneProfiles["Pause"] = new SceneVolumeProfile(0.5f, 0.3f, 0.5f, 0.0f);
        }

        /// <summary>
        /// シーン別のボリュームプロファイルを登録する。
        /// </summary>
        public void RegisterSceneProfile(string sceneName, float bgmVol, float sfxVol, float voiceVol)
        {
            _sceneProfiles[sceneName] = new SceneVolumeProfile(bgmVol, sfxVol, voiceVol, 1.0f);
        }

        /// <summary>
        /// シーンを切り替え、対応するボリュームプロファイルを適用する。
        /// </summary>
        public void SwitchScene(string sceneName)
        {
            if (_currentScene == sceneName) return;
            _currentScene = sceneName;

            if (_sceneProfiles.TryGetValue(sceneName, out var profile))
            {
                _bgmVolume = profile.BgmVolume;
                _sfxVolume = profile.SfxVolume;
                _voiceVolume = profile.VoiceVolume;
                ApplyVolumes();
            }
        }

        /// <summary>
        /// 全ボリュームをデフォルト値にリセットする。
        /// </summary>
        public void ResetToDefaults()
        {
            _masterVolume = 1.0f;
            _bgmVolume = 1.0f;
            _sfxVolume = 1.0f;
            _voiceVolume = 1.0f;
            _masterMuted = false;
            _bgmMuted = false;
            _sfxMuted = false;
            _voiceMuted = false;
            ApplyVolumes();
        }

        /// <summary>
        /// 現在のボリューム設定をシリアライズ可能な辞書として取得する。
        /// </summary>
        public Dictionary<string, float> GetVolumeSnapshot()
        {
            return new Dictionary<string, float>
            {
                ["Master"] = _masterVolume,
                ["Bgm"] = _bgmVolume,
                ["Sfx"] = _sfxVolume,
                ["Voice"] = _voiceVolume,
            };
        }

        /// <summary>
        /// シリアライズされたボリューム設定を復元する。
        /// </summary>
        public void RestoreVolumeSnapshot(Dictionary<string, float> snapshot)
        {
            if (snapshot.TryGetValue("Master", out var master)) _masterVolume = master;
            if (snapshot.TryGetValue("Bgm", out var bgm)) _bgmVolume = bgm;
            if (snapshot.TryGetValue("Sfx", out var sfx)) _sfxVolume = sfx;
            if (snapshot.TryGetValue("Voice", out var voice)) _voiceVolume = voice;
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            var effectiveMaster = _masterMuted ? 0.0f : _masterVolume;
            var effectiveBgm = _bgmMuted ? 0.0f : _bgmVolume;
            var effectiveSfx = _sfxMuted ? 0.0f : _sfxVolume;
            var effectiveVoice = _voiceMuted ? 0.0f : _voiceVolume;

            _audioManager.MasterVolume = effectiveMaster;
            _audioManager.BgmVolume = effectiveBgm;
            _audioManager.SfxVolume = effectiveSfx;
        }

        private readonly record struct SceneVolumeProfile(
            float BgmVolume,
            float SfxVolume,
            float VoiceVolume,
            float MasterVolume);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ResetToDefaults();
        }
    }
}
