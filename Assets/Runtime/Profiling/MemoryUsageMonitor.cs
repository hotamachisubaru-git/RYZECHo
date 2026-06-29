using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace RYZECHo.Profiling
{
    /// <summary>
    /// メモリ使用量監視モニター
    /// メモリ使用量の測定、履歴管理、警告機能を提供する。
    /// </summary>
    public class MemoryUsageMonitor : MonoBehaviour
    {
        public static MemoryUsageMonitor Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("平均計算回数")]
        public int averagingSamples = 60;
        [Tooltip("警告閾値 (MB)")]
        public float warningThresholdMB = 500f;
        [Tooltip("危険閾値 (MB)")]
        public float criticalThresholdMB = 800f;

        [Header("Current Stats")]
        public long UsedHeapSize => usedHeapSize;
        public float UsedHeapSizeMB => usedHeapSizeMB;
        public float MemoryLoadPercent => memoryLoadPercent;

        private long usedHeapSize = 0;
        private float usedHeapSizeMB = 0f;
        private float memoryLoadPercent = 0f;
        private readonly List<float> memoryHistory = new(256);

        public IReadOnlyList<float> MemoryHistory => memoryHistory;

        public delegate void MemoryWarningHandler(float mb);
        public event MemoryWarningHandler OnMemoryWarning;
        public event MemoryWarningHandler OnMemoryCritical;

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
            // ヒープメモリ使用量を取得
            usedHeapSize = System.GC.GetTotalMemory(false);
            usedHeapSizeMB = usedHeapSize / (1024f * 1024f);

            // メモリ負荷率（SystemInfo.systemMemorySizeを基準）
            if (SystemInfo.systemMemorySize > 0)
                memoryLoadPercent = (usedHeapSizeMB / SystemInfo.systemMemorySize) * 100f;

            memoryHistory.Add(usedHeapSizeMB);
            if (memoryHistory.Count > averagingSamples)
                memoryHistory.RemoveAt(0);

            // 警告判定
            if (usedHeapSizeMB > criticalThresholdMB)
            {
                OnMemoryCritical?.Invoke(usedHeapSizeMB);
            }
            else if (usedHeapSizeMB > warningThresholdMB)
            {
                OnMemoryWarning?.Invoke(usedHeapSizeMB);
            }
        }

        /// <summary>
        /// 履歴をクリア
        /// </summary>
        public void ClearHistory() => memoryHistory.Clear();

        /// <summary>
        /// GCコレクションを強制実行
        /// </summary>
        public void ForceGC() => System.GC.Collect();

        /// <summary>
        /// 指定サンプル数の平均メモリ使用量(MB)を取得
        /// </summary>
        public float GetAverageMemoryMB(int samples)
        {
            if (memoryHistory.Count == 0) return 0f;
            int count = Mathf.Min(samples, memoryHistory.Count);
            float sum = 0f;
            for (int i = memoryHistory.Count - 1; i >= Mathf.Max(0, memoryHistory.Count - count); i--)
                sum += memoryHistory[i];
            return sum / count;
        }
    }
}
