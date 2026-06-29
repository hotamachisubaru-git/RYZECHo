using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace RYZECHo.Audio
{
    /// <summary>
    /// ゲーム全体のサウンド管理を行うマネージャー。
    /// Assets/Audio 配下の WAV を MonoGame の SoundEffect として直接ロードします。
    /// </summary>
    public sealed class AudioManager : IDisposable
    {
        private readonly string _assetRoot;
        private readonly Dictionary<string, SoundEffect> _loadedEffects = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, long> _lastPlayedTicks = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _missingEffects = new(StringComparer.OrdinalIgnoreCase);
        private SoundEffectInstance? _musicInstance;
        private string? _currentMusicKey;
        private float _masterVolume = 1.0f;
        private float _sfxVolume = 1.0f;
        private float _bgmVolume = 1.0f;
        private bool _disposed;

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
                ApplyMusicVolume();
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
        }

        public float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
                ApplyMusicVolume();
            }
        }

        public AudioManager(string? assetRoot = null)
        {
            _assetRoot = ResolveAssetRoot(assetRoot);
        }

        /// <summary>
        /// 効果音をロードします。失敗したキーは次回以降サイレントにスキップします。
        /// </summary>
        public bool LoadEffect(string key)
        {
            return TryGetEffect(key) is not null;
        }

        public void PreloadEffects(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                LoadEffect(key);
            }
        }

        /// <summary>
        /// 指定されたキーの効果を再生します。
        /// </summary>
        public void PlayEffect(
            string key,
            float volume = 1.0f,
            float pitch = 0.0f,
            float pan = 0.0f,
            float minIntervalSeconds = 0.0f)
        {
            if (_disposed || IsThrottled(key, minIntervalSeconds))
            {
                return;
            }

            var effect = TryGetEffect(key);
            if (effect is null)
            {
                return;
            }

            var effectiveVolume = MathHelper.Clamp(volume * _masterVolume * _sfxVolume, 0.0f, 1.0f);
            if (effectiveVolume <= 0.001f)
            {
                return;
            }

            try
            {
                effect.Play(
                    effectiveVolume,
                    MathHelper.Clamp(pitch, -1.0f, 1.0f),
                    MathHelper.Clamp(pan, -1.0f, 1.0f));
                _lastPlayedTicks[key] = Stopwatch.GetTimestamp();
            }
            catch
            {
                _missingEffects.Add(key);
            }
        }

        public void PlayMusic(string key, bool loop = true)
        {
            if (_disposed || _currentMusicKey == key && _musicInstance?.State == SoundState.Playing)
            {
                return;
            }

            StopMusic();

            var effect = TryGetEffect(key);
            if (effect is null)
            {
                return;
            }

            try
            {
                _musicInstance = effect.CreateInstance();
                _musicInstance.IsLooped = loop;
                _currentMusicKey = key;
                ApplyMusicVolume();
                _musicInstance.Play();
            }
            catch
            {
                StopMusic();
                _missingEffects.Add(key);
            }
        }

        /// <summary>
        /// 全ての音を停止します。
        /// </summary>
        public void StopAll()
        {
            StopMusic();
            _lastPlayedTicks.Clear();
        }

        public void StopMusic()
        {
            if (_musicInstance is null)
            {
                _currentMusicKey = null;
                return;
            }

            try
            {
                _musicInstance.Stop();
            }
            catch
            {
                // 停止時のデバイス例外はプロトタイプ継続を優先して握りつぶす。
            }

            _musicInstance.Dispose();
            _musicInstance = null;
            _currentMusicKey = null;
        }

        private SoundEffect? TryGetEffect(string key)
        {
            if (_loadedEffects.TryGetValue(key, out var loaded))
            {
                return loaded;
            }

            if (_missingEffects.Contains(key))
            {
                return null;
            }

            var path = ResolveEffectPath(key);
            if (!File.Exists(path))
            {
                _missingEffects.Add(key);
                return null;
            }

            try
            {
                using var stream = File.OpenRead(path);
                var effect = SoundEffect.FromStream(stream);
                _loadedEffects[key] = effect;
                return effect;
            }
            catch
            {
                _missingEffects.Add(key);
                return null;
            }
        }

        private bool IsThrottled(string key, float minIntervalSeconds)
        {
            if (minIntervalSeconds <= 0.0f || !_lastPlayedTicks.TryGetValue(key, out var lastTicks))
            {
                return false;
            }

            var elapsedSeconds = (Stopwatch.GetTimestamp() - lastTicks) / (float)Stopwatch.Frequency;
            return elapsedSeconds < minIntervalSeconds;
        }

        private void ApplyMusicVolume()
        {
            if (_musicInstance is not null)
            {
                _musicInstance.Volume = MathHelper.Clamp(_masterVolume * _bgmVolume, 0.0f, 1.0f);
            }
        }

        private string ResolveEffectPath(string key)
        {
            var normalizedKey = key
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            if (!Path.HasExtension(normalizedKey))
            {
                normalizedKey += ".wav";
            }

            return Path.IsPathRooted(normalizedKey)
                ? normalizedKey
                : Path.Combine(_assetRoot, normalizedKey);
        }

        private static string ResolveAssetRoot(string? assetRoot)
        {
            var baseDirectoryRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio");
            var workingDirectoryRoot = Path.Combine(Environment.CurrentDirectory, "Assets", "Audio");

            var candidates = string.IsNullOrWhiteSpace(assetRoot)
                ? new string[] { baseDirectoryRoot, workingDirectoryRoot }
                : new[] { assetRoot, baseDirectoryRoot, workingDirectoryRoot };

            return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopMusic();

            foreach (var effect in _loadedEffects.Values)
            {
                effect.Dispose();
            }

            _loadedEffects.Clear();
        }
    }
}
