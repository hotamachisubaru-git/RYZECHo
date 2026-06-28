using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace RYZECHo.Profiling
{
    /// <summary>
    /// GC alloc の追跡モニター
    /// GC alloc の測定、履歴管理、警告機能を提供する。
    /// </summary>
    public class GCAllocationTracker : MonoBehaviour
    {
        public static GCAllocationTracker Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("警告閾値 (MB/秒)")]
        public float warningThresholdMBPerSec = 2f;
        [Tooltip("危険閾値 (MB/秒)")]
        public float criticalThresholdMBPerSec = 5f;

        [Header("Current Stats")]
        public float CurrentAllocMBPerSec => currentAllocMBPerSec;
        public int TotalAllocCount => totalAllocCount;

        private float currentAllocMBPerSec = 0f;
        private float allocBuffer = 0f;
        private float elapsed = 0f;
        private int totalAllocCount = 0;
        private readonly List<float> allocHistory = new(256);

        public IReadOnlyList<float> AllocHistory => allocHistory;

        public delegate void GCWarningHandler(float mbPerSec);
        public event GCWarningHandler OnGCWarning;
        public event GCWarningHandler OnGCCritical;

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
            // GC alloc の測定（Perframe alloc を取得）
            float prevAlloc = allocBuffer;
            long beforeBytes = Profiling.GetAllocBytesForFrameAllocator(Allocator.Persistent);

            // 実際の測定はフレーム間で差分を取る
            allocBuffer = Profiling.GetAllocBytesForFrameAllocator(Allocator.Persistent);
            float deltaAllocMB = (allocBuffer - prevAlloc) / (1024f * 1024f);

            // フレーム間差分が負の場合はリセット（GC collect 等）
            if (deltaAllocMB < 0f)
                deltaAllocMB = 0f;

            elapsed += Time.deltaTime;
            currentAllocMBPerSec += deltaAllocMB;
            totalAllocCount++;

            // 1秒ごとに履歴に記録
            if (elapsed >= 1f)
            {
                allocHistory.Add(currentAllocMBPerSec);
                if (allocHistory.Count > 256)
                    allocHistory.RemoveAt(0);

                currentAllocMBPerSec = 0f;
                elapsed = 0f;

                // 警告判定
                if (currentAllocMBPerSec > criticalThresholdMBPerSec)
                {
                    OnGCCritical?.Invoke(currentAllocMBPerSec);
                }
                else if (currentAllocMBPerSec > warningThresholdMBPerSec)
                {
                    OnGCWarning?.Invoke(currentAllocMBPerSec);
                }
            }
        }

        /// <summary>
        /// 履歴をクリア
        /// </summary>
        public void ClearHistory() => allocHistory.Clear();

        /// <summary>
        /// 強制GC実行
        /// </summary>
        public void ForceGC() => System.GC.Collect();

        /// <summary>
        /// 指定秒数の平均GC alloc (MB/秒) を取得
        /// </summary>
        public float GetAverageAllocMBPerSec(int seconds)
        {
            if (allocHistory.Count == 0) return 0f;
            int count = Mathf.Min(seconds, allocHistory.Count);
            float sum = 0f;
            for (int i = allocHistory.Count - 1; i >= Mathf.Max(0, allocHistory.Count - count); i--)
                sum += allocHistory[i];
            return sum / count;
        }
    }
}
