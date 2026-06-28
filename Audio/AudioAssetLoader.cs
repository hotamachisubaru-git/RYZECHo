using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;

namespace RYZECHo.Audio
{
    /// <summary>
    /// 音源の非同期ローディングを管理する。
    /// アセットバンドル対応・ローディング進捗の通知に対応。
    /// </summary>
    public sealed class AudioAssetLoader : IDisposable
    {
        private readonly string _assetRoot;
        private readonly Dictionary<string, string> _pathCache;
        private readonly HashSet<string> _loadingKeys;
        private readonly HashSet<string> _loadedKeys;
        private readonly HashSet<string> _failedKeys;
        private float _loadProgress;
        private int _totalTasks;
        private int _completedTasks;
        private bool _disposed;

        // Progress events
        public event Action<float>? OnLoadProgressChanged;
        public event Action<string, bool>? OnAssetLoaded;  // (key, success)
        public event Action<string, Exception?>? OnAssetLoadError;

        public float LoadProgress => _totalTasks > 0 ? _completedTasks / (float)_totalTasks : 0f;
        public bool IsLoading { get; private set; }

        public AudioAssetLoader(string? assetRoot = null)
        {
            _assetRoot = ResolveAssetRoot(assetRoot);
            _pathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _loadingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _loadedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _failedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 単一の音源を非同期でロードする。
        /// </summary>
        public async Task<SoundEffect?> LoadAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_disposed) return null;
            if (_loadedKeys.Contains(key)) return null; // Already loaded

            var path = GetAssetPath(key);
            if (!File.Exists(path))
            {
                _failedKeys.Add(key);
                OnAssetLoadError?.Invoke(key, new FileNotFoundException($"Audio asset not found: {key}", key));
                return null;
            }

            try
            {
                using var stream = File.OpenRead(path);
                var buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                var effect = SoundEffect.FromStream(new MemoryStream(buffer));
                _loadedKeys.Add(key);
                OnAssetLoaded?.Invoke(key, true);
                return effect;
            }
            catch (Exception ex)
            {
                _failedKeys.Add(key);
                OnAssetLoadError?.Invoke(key, ex);
                return null;
            }
        }

        /// <summary>
        /// 複数の音源を一括非同期ロードする（進捗通知付き）。
        /// </summary>
        public async Task LoadBatchAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            if (_disposed) return;
            IsLoading = true;
            _loadingKeys.Clear();
            _totalTasks = 0;
            _completedTasks = 0;

            var keyList = new List<string>(keys);
            _totalTasks = keyList.Count;

            foreach (var key in keyList)
            {
                if (cancellationToken.IsCancellationRequested) break;
                _loadingKeys.Add(key);
                await LoadAsync(key, cancellationToken);
                _loadingKeys.Remove(key);
                _completedTasks++;
                OnLoadProgressChanged?.Invoke(LoadProgress);
            }

            IsLoading = false;
            OnLoadProgressChanged?.Invoke(1.0f);
        }

        /// <summary>
        /// 音源がロード済みかチェックする。
        /// </summary>
        public bool IsLoaded(string key) => _loadedKeys.Contains(key);

        /// <summary>
        /// 音源が読み込み中かチェックする。
        /// </summary>
        public bool IsLoadingKey(string key) => _loadingKeys.Contains(key);

        /// <summary>
        /// 音源がロード失敗したかチェックする。
        /// </summary>
        public bool IsFailed(string key) => _failedKeys.Contains(key);

        /// <summary>
        /// 失敗した音源を再ロードする。
        /// </summary>
        public async Task RetryFailedAsync(CancellationToken cancellationToken = default)
        {
            var failedKeys = new List<string>(_failedKeys);
            _failedKeys.Clear();
            await LoadBatchAsync(failedKeys, cancellationToken);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _pathCache.Clear();
            _loadingKeys.Clear();
            _loadedKeys.Clear();
            _failedKeys.Clear();
        }

        private string GetAssetPath(string key)
        {
            if (_pathCache.TryGetValue(key, out var cached)) return cached;

            var normalizedKey = key
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            if (!Path.HasExtension(normalizedKey))
                normalizedKey += ".wav";

            var path = Path.IsPathRooted(normalizedKey)
                ? normalizedKey
                : Path.Combine(_assetRoot, normalizedKey);

            _pathCache[key] = path;
            return path;
        }

        private static string ResolveAssetRoot(string? assetRoot)
        {
            var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio");
            var workingDir = Path.Combine(Environment.CurrentDirectory, "Assets", "Audio");

            if (!string.IsNullOrWhiteSpace(assetRoot) && Directory.Exists(assetRoot))
                return assetRoot;
            if (Directory.Exists(baseDir))
                return baseDir;
            if (Directory.Exists(workingDir))
                return workingDir;
            return baseDir;
        }
    }
}
