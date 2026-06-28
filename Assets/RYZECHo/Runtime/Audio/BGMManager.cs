using System;
using UnityEngine;
using UnityEngine.Audio;

namespace RYZECHo.Audio
{
    /// <summary>
    /// BGM管理専用マネージャー（フェードイン/アウト対応）
    /// </summary>
    public sealed class BGMManager : IDisposable
    {
        private AudioSource _bgmSource;
        private AudioClip _currentClip;
        private float _targetVolume = 1f;
        private float _currentVolume = 0f;
        private bool _isFading;
        private bool _disposed;

        public event Action<BGMState>? OnStateChanged;
        public event Action<float>? OnFadeProgressChanged;

        public string CurrentBGMName => _currentClip?.name ?? string.Empty;
        public bool IsPlaying => _bgmSource != null && _bgmSource.isPlaying;
        public bool IsFading => _isFading;
        public float CurrentVolume => _currentVolume;
        public float TargetVolume { get; private set; } = 1f;

        public enum BGMState
        {
            Stopped,
            Playing,
            FadingIn,
            FadingOut,
            Paused
        }

        public BGMManager(AudioMixerGroup mixerGroup = null)
        {
            var go = new GameObject("BGMManager") { hideFlags = HideFlags.HideInHierarchy };
            _bgmSource = go.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.spatialBlend = 0f;

            if (mixerGroup != null)
                _bgmSource.outputAudioMixerGroup = mixerGroup;
        }

        /// <summary>BGMをフェードインで再生</summary>
        public void Play(AudioClip clip, float fadeInDuration = 1f)
        {
            if (clip == null) return;

            if (_currentClip == clip && IsPlaying) return;

            _currentClip = clip;
            _bgmSource.clip = clip;
            _targetVolume = _bgmSource.volume;

            if (fadeInDuration <= 0f)
            {
                _bgmSource.Play();
                _currentVolume = _targetVolume;
                _isFading = false;
                OnStateChanged?.Invoke(BGMState.Playing);
                return;
            }

            _isFading = true;
            _currentVolume = 0f;
            OnStateChanged?.Invoke(BGMState.FadingIn);

            FadeInCoroutine(clip, fadeInDuration);
        }

        /// <summary>BGMをフェードアウトして停止</summary>
        public void Stop(float fadeOutDuration = 1f)
        {
            if (!_isFading && _currentVolume <= 0.01f)
            {
                OnStateChanged?.Invoke(BGMState.Stopped);
                return;
            }

            _isFading = true;
            _targetVolume = 0f;
            OnStateChanged?.Invoke(BGMState.FadingOut);

            FadeOutCoroutine(fadeOutDuration);
        }

        /// <summary>BGMをフェードでボリューム変更</summary>
        public void SetVolume(float volume, float duration = 0.5f)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.ApproxEquals(_targetVolume, volume) && !_isFading) return;

            _isFading = true;
            _targetVolume = volume;
            OnStateChanged?.Invoke(_targetVolume > 0 ? BGMState.Playing : BGMState.FadingOut);

            if (duration <= 0f)
            {
                _currentVolume = volume;
                _bgmSource.volume = volume;
                _isFading = false;
            }
            else
            {
                FadeToCoroutine(volume, duration);
            }
        }

        /// <summary>BGMを一時停止</summary>
        public void Pause()
        {
            if (!IsPlaying) return;
            _bgmSource.Pause();
            OnStateChanged?.Invoke(BGMState.Paused);
        }

        /// <summary>BGMを再開</summary>
        public void Resume()
        {
            if (_bgmSource == null || _bgmSource.clip == null) return;
            _bgmSource.UnPause();
            OnStateChanged?.Invoke(BGMState.Playing);
        }

        /// <summary>BGMをスキップ（先頭から再再生）</summary>
        public void Skip()
        {
            if (_currentClip == null) return;
            _bgmSource.time = 0f;
            _bgmSource.Play();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _bgmSource?.Stop(); } catch { }
            if (_bgmSource != null)
            {
                var go = _bgmSource.gameObject;
                _bgmSource = null;
                GameObject.Destroy(go);
            }
        }

        private void FadeInCoroutine(AudioClip clip, float duration)
        {
            var go = _bgmSource.gameObject;
            go.AddComponent<FadeInBGM>().Initialize(this, clip, duration);
        }

        private void FadeOutCoroutine(float duration)
        {
            var go = _bgmSource.gameObject;
            go.AddComponent<FadeOutBGM>().Initialize(this, duration);
        }

        private void FadeToCoroutine(float target, float duration)
        {
            var go = _bgmSource.gameObject;
            go.AddComponent<FadeToBGM>().Initialize(this, target, duration);
        }

        internal void UpdateFade(float deltaTime)
        {
            if (!_isFading) return;

            if (_currentVolume < _targetVolume)
            {
                _currentVolume = Mathf.MoveTowards(_currentVolume, _targetVolume, deltaTime * 10f);
                if (Mathf.ApproxEquals(_currentVolume, _targetVolume))
                {
                    _currentVolume = _targetVolume;
                    _isFading = false;
                    OnStateChanged?.Invoke(_targetVolume > 0 ? BGMState.Playing : BGMState.Stopped);
                }
            }
            else
            {
                _currentVolume = Mathf.MoveTowards(_currentVolume, _targetVolume, deltaTime * 10f);
                if (Mathf.ApproxEquals(_currentVolume, _targetVolume))
                {
                    _currentVolume = _targetVolume;
                    _isFading = false;
                    OnStateChanged?.Invoke(_targetVolume > 0 ? BGMState.Playing : BGMState.Stopped);
                }
            }

            _bgmSource.volume = _currentVolume;
            OnFadeProgressChanged?.Invoke(_currentVolume);
        }
    }

    /// <summary>BGMフェードイン用コルーチン</summary>
    private sealed class FadeInBGM : MonoBehaviour
    {
        private BGMManager _manager;
        private float _duration;
        private float _elapsed;

        public void Initialize(BGMManager manager, AudioClip clip, float duration)
        {
            _manager = manager;
            _duration = duration;
            _elapsed = 0f;
            _manager._bgmSource.Play();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / _duration);
            _manager._bgmSource.volume = t;
            if (t >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>BGMフェードアウト用コルーチン</summary>
    private sealed class FadeOutBGM : MonoBehaviour
    {
        private BGMManager _manager;
        private float _duration;
        private float _elapsed;

        public void Initialize(BGMManager manager, float duration)
        {
            _manager = manager;
            _duration = duration;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var t = 1f - Mathf.Clamp01(_elapsed / _duration);
            _manager._bgmSource.volume = t;
            if (t <= 0f)
            {
                _manager._bgmSource.Stop();
                _manager._isFading = false;
                _manager.OnStateChanged?.Invoke(BGMManager.BGMState.Stopped);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>BGMボリューム変更用コルーチン</summary>
    private sealed class FadeToBGM : MonoBehaviour
    {
        private BGMManager _manager;
        private float _target;
        private float _duration;
        private float _startVolume;
        private float _elapsed;

        public void Initialize(BGMManager manager, float target, float duration)
        {
            _manager = manager;
            _target = target;
            _duration = duration;
            _startVolume = manager._currentVolume;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / _duration);
            _manager._bgmSource.volume = Mathf.Lerp(_startVolume, _target, t);
            if (t >= 1f)
            {
                _manager._currentVolume = _target;
                _manager._isFading = false;
                Destroy(gameObject);
            }
        }
    }
}
