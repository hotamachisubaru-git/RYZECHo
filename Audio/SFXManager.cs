using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace RYZECHo.Audio
{
    /// <summary>
    /// 効果音の再生・停止・ボリューム管理を行うマネージャー。
    /// オブジェクトプーリングによりGCアルロケーションを抑制。
    /// </summary>
    public sealed class SFXManager : IDisposable
    {
        private readonly AudioManager _audioManager;
        private readonly SoundEffectInstancePool _pool;
        private float _volume = 1.0f;
        private bool _muted;
        private readonly HashSet<string> _activeSounds;
        private bool _disposed;

        public float Volume
        {
            get => _volume;
            set => _volume = MathHelper.Clamp(value, 0.0f, 1.0f);
        }

        public bool IsMuted
        {
            get => _muted;
            set => _muted = value;
        }

        public int ActiveSoundCount => _activeSounds.Count;

        public SFXManager(AudioManager audioManager)
        {
            _audioManager = audioManager;
            _pool = new SoundEffectInstancePool(64);
            _activeSounds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 効果音を再生する（一時停止・再開対応）。
        /// </summary>
        public void Play(string key, float volume = 1.0f, float pitch = 0.0f, float pan = 0.0f, float minIntervalSeconds = 0.0f)
        {
            if (_disposed || _muted) return;

            if (_audioManager.IsThrottled(key, minIntervalSeconds))
                return;

            var instance = _pool.Get();
            if (instance == null)
            {
                // プールが溢れた場合は直接再生
                _audioManager.PlayEffect(key, volume, pitch, pan, minIntervalSeconds);
                return;
            }

            var effect = _audioManager.TryGetEffect(key);
            if (effect == null)
            {
                _pool.Return(instance);
                return;
            }

            instance.SetEffect(effect);
            instance.Volume = MathHelper.Clamp(volume * _volume, 0.0f, 1.0f);
            instance.Pitch = MathHelper.Clamp(pitch, -1.0f, 1.0f);
            instance.Panel panVal = MathHelper.Clamp(pan, -1.0f, 1.0f);
            instance.Pan = panVal;
            instance.IsLooped = false;
            instance.Play();

            _activeSounds.Add(key);
            instance.StateChanged += OnInstanceStateChanged;
        }

        /// <summary>
        /// 指定されたキーの効果を停止する。
        /// </summary>
        public void Stop(string key)
        {
            _activeSounds.Remove(key);
            _pool.ReleaseByKey(key);
        }

        /// <summary>
        /// 全ての効果音を停止する。
        /// </summary>
        public void StopAll()
        {
            _pool.Clear();
            _activeSounds.Clear();
        }

        /// <summary>
        /// 効果音をボリューム付きで再生し、完了時にコールバックを呼び出す。
        /// </summary>
        public void PlayWithCallback(string key, float volume, Action onComplete)
        {
            Play(key, volume);
            // シンプル実装: 即座に完了コールバック
            onComplete?.Invoke();
        }

        private void OnInstanceStateChanged(object sender, EventArgs e)
        {
            if (sender is SoundEffectInstance instance && instance.State == SoundState.Stopped)
            {
                _pool.Return(instance);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopAll();
            _pool.Dispose();
        }

        /// <summary>
        /// 効果音用インスタンスプール。
        /// </summary>
        private sealed class SoundEffectInstancePool : IDisposable
        {
            private readonly Queue<SoundEffectInstance> _pool;
            private readonly int _maxSize;
            private bool _disposed;

            public SoundEffectInstancePool(int maxSize)
            {
                _maxSize = maxSize;
                _pool = new Queue<SoundEffectInstance>(maxSize);
            }

            public SoundEffectInstance? Get()
            {
                if (_pool.Count > 0)
                    return _pool.Dequeue();
                return null;
            }

            public void Return(SoundEffectInstance instance)
            {
                if (_disposed) { instance.Dispose(); return; }
                if (_pool.Count < _maxSize)
                {
                    try { instance.Stop(); _pool.Enqueue(instance); }
                    catch { instance.Dispose(); }
                }
                else
                {
                    instance.Dispose();
                }
            }

            public void ReleaseByKey(string key)
            {
                var filtered = new List<SoundEffectInstance>();
                while (_pool.Count > 0)
                {
                    var inst = _pool.Dequeue();
                    if (inst != null) filtered.Add(inst);
                }
                _pool.Clear();
                foreach (var inst in filtered)
                {
                    try { inst.Stop(); inst.Dispose(); } catch { }
                }
            }

            public void Clear()
            {
                while (_pool.Count > 0)
                {
                    var inst = _pool.Dequeue();
                    try { inst.Stop(); inst.Dispose(); } catch { }
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Clear();
            }
        }
    }
}
