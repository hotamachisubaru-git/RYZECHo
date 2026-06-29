using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace RYZECHo.Audio
{
    /// <summary>
    /// 音源のキャッシュ管理を行う。
    /// キャッシュのヒット率向上とメモリ管理に対応。
    /// </summary>
    public sealed class AudioCacheManager : IDisposable
    {
        private readonly Dictionary<string, CachedSound> _cache;
        private readonly int _maxCacheSize;
        private int _hits;
        private int _misses;
        private long _totalMemoryBytes;
        private bool _disposed;

        public int CacheSize => _cache.Count;
        public int TotalHits => _hits;
        public int TotalMisses => _misses;
        public float HitRate => (_hits + _misses) > 0 ? _hits / (_hits + _misses) : 0f;
        public long TotalMemoryBytes => _totalMemoryBytes;
        public int MaxCacheSize => _maxCacheSize;

        public AudioCacheManager(int maxCacheSize = 128)
        {
            _maxCacheSize = maxCacheSize;
            _cache = new Dictionary<string, CachedSound>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// キャッシュに音源を登録する（LRU淘汰付き）。
        /// </summary>
        public void Add(string key, SoundEffect effect, int estimatedSizeBytes)
        {
            if (_disposed) return;
            if (_cache.ContainsKey(key))
            {
                _cache[key].Effect = effect;
                _cache[key].LastAccessed = DateTime.UtcNow;
                return;
            }

            // キャッシュがいっぱいの場合はLRU淘汰
            while (_cache.Count >= _maxCacheSize && _cache.Count > 0)
            {
                EvictLeastRecentlyUsed();
            }

            _cache[key] = new CachedSound(effect, estimatedSizeBytes);
            _totalMemoryBytes += estimatedSizeBytes;
        }

        /// <summary>
        /// キャッシュから音源を取得する。
        /// </summary>
        public bool TryGet(string key, out SoundEffect? effect)
        {
            effect = null;
            if (_disposed) return false;

            if (_cache.TryGetValue(key, out var cached))
            {
                cached.LastAccessed = DateTime.UtcNow;
                _hits++;
                effect = cached.Effect;
                return true;
            }

            _misses++;
            return false;
        }

        /// <summary>
        /// キャッシュから音源を削除する。
        /// </summary>
        public void Remove(string key)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                _totalMemoryBytes -= cached.EstimatedSizeBytes;
                cached.Effect?.Dispose();
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// 全キャッシュをクリアする。
        /// </summary>
        public void Clear()
        {
            foreach (var entry in _cache.Values)
            {
                entry.Effect?.Dispose();
            }
            _cache.Clear();
            _totalMemoryBytes = 0;
        }

        /// <summary>
        /// キャッシュ統計情報を取得する。
        /// </summary>
        public Dictionary<string, object> GetStats()
        {
            return new Dictionary<string, object>
            {
                ["CacheSize"] = _cache.Count,
                ["MaxCacheSize"] = _maxCacheSize,
                ["Hits"] = _hits,
                ["Misses"] = _misses,
                ["HitRate"] = HitRate,
                ["TotalMemoryBytes"] = _totalMemoryBytes,
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
        }

        private void EvictLeastRecentlyUsed()
        {
            string? evictKey = null;
            DateTime oldest = DateTime.UtcNow;

            foreach (var kvp in _cache)
            {
                if (kvp.Value.LastAccessed < oldest)
                {
                    oldest = kvp.Value.LastAccessed;
                    evictKey = kvp.Key;
                }
            }

            if (evictKey != null)
            {
                Remove(evictKey);
            }
        }

        private sealed class CachedSound
        {
            public SoundEffect Effect { get; set; }
            public int EstimatedSizeBytes { get; }
            public DateTime LastAccessed { get; set; }

            public CachedSound(SoundEffect effect, int estimatedSizeBytes)
            {
                Effect = effect;
                EstimatedSizeBytes = estimatedSizeBytes;
                LastAccessed = DateTime.UtcNow;
            }
        }
    }
}
