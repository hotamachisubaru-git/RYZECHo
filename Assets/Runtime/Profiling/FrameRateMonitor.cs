using System.Collections.Generic;
using UnityEngine;

namespace RYZECHo.Profiling
{
    /// <summary>
    /// フレームレート測定モニター
    /// 現在のフレームレート、履歴管理、警告機能を提供する。
    /// </summary>
    public class FrameRateMonitor : MonoBehaviour
    {
        public static FrameRateMonitor Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("平均計算フレーム数")]
        public int averagingFrames = 60;
        [Tooltip("警告閾値 (FPS)")]
        public float warningThreshold = 30f;
        [Tooltip("危険閾値 (FPS)")]
        public float criticalThreshold = 15f;

        [Header("Current Stats")]
        public float CurrentFPS => currentFPS;
        public float AverageFPS => averageFPS;

        private float currentFPS = 60f;
        private float averageFPS = 60f;
        private float deltaTime = 0f;
        private float elapsed = 0f;
        private readonly List<float> fpsHistory = new(256);

        public IReadOnlyList<float> FpsHistory => fpsHistory;

        public delegate void FPSWarningHandler(float fps);
        public event FPSWarningHandler OnFPSWarning;
        public event FPSWarningHandler OnFPSCritical;

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
            deltaTime = Time.deltaTime;
            currentFPS = deltaTime > 0f ? 1f / deltaTime : 60f;

            elapsed += deltaTime;
            fpsHistory.Add(currentFPS);

            if (fpsHistory.Count > averagingFrames)
                fpsHistory.RemoveAt(0);

            // 平均FPS計算
            float sum = 0f;
            foreach (var fps in fpsHistory)
                sum += fps;
            averageFPS = sum / fpsHistory.Count;

            // 警告判定
            if (currentFPS < criticalThreshold)
            {
                OnFPSCritical?.Invoke(currentFPS);
            }
            else if (currentFPS < warningThreshold)
            {
                OnFPSWarning?.Invoke(currentFPS);
            }
        }

        /// <summary>
        /// 履歴をクリア
        /// </summary>
        public void ClearHistory() => fpsHistory.Clear();

        /// <summary>
        /// 指定フレーム数の平均FPSを取得
        /// </summary>
        public float GetAverageFPS(int frames)
        {
            if (fpsHistory.Count == 0) return 0f;
            int count = Mathf.Min(frames, fpsHistory.Count);
            float sum = 0f;
            for (int i = fpsHistory.Count - 1; i >= Mathf.Max(0, fpsHistory.Count - count); i--)
                sum += fpsHistory[i];
            return sum / count;
        }
    }
}
