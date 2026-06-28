using System.Collections.Generic;
using UnityEngine;

namespace RYZECHo.Profiling
{
    /// <summary>
    /// オブジェクトプールの効果測定モニター
    /// プールの使用状況、効果の測定、警告機能を提供する。
    /// </summary>
    public class ObjectPoolProfiler : MonoBehaviour
    {
        public static ObjectPoolProfiler Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("警告閾値 (解放/秒)")]
        public float warningThresholdPerSec = 100f;
        [Tooltip("危険閾値 (解放/秒)")]
        public float criticalThresholdPerSec = 500f;

        [Header("Current Stats")]
        public float PoolUtilizationPercent => poolUtilizationPercent;
        public int TotalPoolHits => totalPoolHits;
        public int TotalPoolMisses => totalPoolMisses;
        public float TotalAllocations => totalAllocations;
        public float TotalReuses => totalReuses;

        private float poolUtilizationPercent = 0f;
        private int totalPoolHits = 0;
        private int totalPoolMisses = 0;
        private float totalAllocations = 0f;
        private float totalReuses = 0f;

        // プールごとの統計
        private readonly Dictionary<string, PoolStats> poolStats = new();
        private readonly List<float> utilizationHistory = new(256);

        public IReadOnlyList<float> UtilizationHistory => utilizationHistory;

        public struct PoolStats
        {
            public int size;
            public int activeCount;
            public int inactiveCount;
            public int hits;
            public int misses;
        }

        public delegate void PoolWarningHandler(string poolName, int count);
        public event PoolWarningHandler OnPoolWarning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // プールの利用率を計算
            int totalSize = 0;
            int totalActive = 0;
            foreach (var stats in poolStats.Values)
            {
                totalSize += stats.size;
                totalActive += stats.activeCount;
            }

            if (totalSize > 0)
                poolUtilizationPercent = (totalActive / (float)totalSize) * 100f;

            utilizationHistory.Add(poolUtilizationPercent);
            if (utilizationHistory.Count > 256)
                utilizationHistory.RemoveAt(0);
        }

        /// <summary>
        /// プールの使用を記録（ヒット時）
        /// </summary>
        public void RecordPoolHit(string poolName, int currentSize, int activeCount)
        {
            totalPoolHits++;
            totalReuses++;

            if (!poolStats.TryGetValue(poolName, out var stats))
            {
                stats = new PoolStats();
                poolStats[poolName] = stats;
            }
            stats.hits++;
            stats.size = currentSize;
            stats.activeCount = activeCount;
            stats.inactiveCount = currentSize - activeCount;
        }

        /// <summary>
        /// プールの使用を記録（ミス時 = 新規割り当て）
        /// </summary>
        public void RecordPoolMiss(string poolName)
        {
            totalPoolMisses++;
            totalAllocations++;

            if (!poolStats.TryGetValue(poolName, out var stats))
            {
                stats = new PoolStats();
                poolStats[poolName] = stats;
            }
            stats.misses++;
        }

        /// <summary>
        /// プールの使用を記録（解放時）
        /// </summary>
        public void RecordPoolRelease(string poolName, int currentSize, int activeCount)
        {
            if (poolStats.TryGetValue(poolName, out var stats))
            {
                stats.size = currentSize;
                stats.activeCount = activeCount;
                stats.inactiveCount = currentSize - activeCount;
            }
        }

        /// <summary>
        /// 履歴をクリア
        /// </summary>
        public void ClearHistory() => utilizationHistory.Clear();

        /// <summary>
        /// プールごとの再利用率を計算
        /// </summary>
        public float GetReuseRate(string poolName)
        {
            if (!poolStats.TryGetValue(poolName, out var stats))
                return 0f;
            int total = stats.hits + stats.misses;
            return total > 0 ? (stats.hits / (float)total) * 100f : 0f;
        }

        /// <summary>
        /// 全プールの平均再利用率を取得
        /// </summary>
        public float GetAverageReuseRate()
        {
            int totalHits = 0;
            int totalMisses = 0;
            foreach (var stats in poolStats.Values)
            {
                totalHits += stats.hits;
                totalMisses += stats.misses;
            }
            int total = totalHits + totalMisses;
            return total > 0 ? (totalHits / (float)total) * 100f : 0f;
        }
    }
}
