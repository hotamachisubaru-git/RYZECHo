using UnityEngine;
using System;

namespace RYZECHo
{
    /// <summary>
    /// カメラシェイクエフェクト。ダメージ時、爆発時にカメラを揺らす。
    /// オプションで減衰とランダム性をサポート。
    /// </summary>
    public class CameraShakeEffect : MonoBehaviour
    {
        [Header("Shake Settings")]
        [Tooltip("シェイク強度")]
        [SerializeField] private float shakeIntensity = 0.3f;

        [Tooltip("シェイク減衰")]
        [SerializeField] private float shakeDecay = 0.95f;

        [Tooltip("ランダムシード")]
        [SerializeField] private int randomSeed = 0;

        [Header("Duration")]
        [Tooltip("最小シェイク時間")]
        [SerializeField] private float minDuration = 0.1f;

        [Tooltip("最大シェイク時間")]
        [SerializeField] private float maxDuration = 0.3f;

        [Header("Frequency")]
        [Tooltip("シェイク周波数")]
        [SerializeField] private float shakeFrequency = 15f;

        [Header("Direction")]
        [Tooltip("XYシェイク比")]
        [SerializeField] private float xyRatio = 1f;

        [Tooltip("Z軸シェイク（ズーム変化）")]
        [SerializeField] private float zShakeAmount = 0f;

        // 内部状態
        private Vector3 _originalPosition;
        private float _currentIntensity;
        private float _elapsedTime;
        private float _shakeDuration;
        private bool _isShaking;
        private System.Random _random;

        public float CurrentIntensity => _currentIntensity;
        public bool IsShaking => _isShaking;

        // イベント
        public event Action<float> OnShakeStarted;
        public event Action OnShakeCompleted;

        private void Awake()
        {
            _originalPosition = transform.position;
            _random = new System.Random(randomSeed);
            _currentIntensity = 0f;
        }

        private void Update()
        {
            if (!_isShaking)
                return;

            UpdateShake();
        }

        /// <summary>
        /// シェイクを開始
        /// </summary>
        public void StartShake(float intensity = -1f, float duration = -1f)
        {
            _originalPosition = transform.position;
            _currentIntensity = intensity > 0 ? intensity : shakeIntensity;
            _shakeDuration = duration > 0 ? duration : Random.Range(minDuration, maxDuration);
            _elapsedTime = 0f;
            _isShaking = true;

            OnShakeStarted?.Invoke(_currentIntensity);
        }

        /// <summary>
        /// シェイクを即座に停止
        /// </summary>
        public void StopShake()
        {
            _isShaking = false;
            _currentIntensity = 0f;
            transform.position = _originalPosition;
            OnShakeCompleted?.Invoke();
        }

        /// <summary>
        /// シェイクを更新
        /// </summary>
        private void UpdateShake()
        {
            _elapsedTime += Time.deltaTime;

            // 減衰
            _currentIntensity *= shakeDecay;

            // 終了判定
            if (_elapsedTime >= _shakeDuration || _currentIntensity < 0.001f)
            {
                StopShake();
                return;
            }

            // ランダムなオフセットを生成
            float xShake = (float)(_random.NextDouble() * 2 - 1) * _currentIntensity;
            float yShake = (float)(_random.NextDouble() * 2 - 1) * _currentIntensity * xyRatio;
            float zShake = zShakeAmount > 0 ? (float)(_random.NextDouble() * 2 - 1) * _currentIntensity * zShakeAmount : 0f;

            // 高周波シェイク（周波数に基づくサンプリング）
            float freqFactor = Mathf.Sin(_elapsedTime * shakeFrequency * Mathf.PI * 2);
            xShake *= freqFactor;
            yShake *= freqFactor;

            // 位置を適用
            transform.position = _originalPosition + new Vector3(xShake, yShake, zShake);
        }

        /// <summary>
        /// ダメージによるシェイク（エフェクトから呼び出し用）
        /// </summary>
        public void ShakeOnDamage(float damageAmount)
        {
            float intensity = Mathf.Clamp01(damageAmount / 50f) * shakeIntensity * 2f;
            float duration = Mathf.Clamp01(damageAmount / 100f) * maxDuration * 2f;
            StartShake(intensity, duration);
        }

        /// <summary>
        /// 爆発によるシェイク
        /// </summary>
        public void ShakeOnExplosion(float explosionRadius, float distance)
        {
            float intensity = Mathf.Clamp01(1f - (distance / explosionRadius)) * shakeIntensity * 3f;
            float duration = intensity * 0.5f;
            StartShake(intensity, duration);
        }

        /// <summary>
        /// シェイク強度をセット（直接）
        /// </summary>
        public void SetShakeIntensity(float intensity)
        {
            shakeIntensity = Mathf.Max(0f, intensity);
        }

        /// <summary>
        /// シェイク状態のシリアライズ用データ構造
        /// </summary>
        [Serializable]
        public struct ShakeState
        {
            public float shakeIntensity;
            public float shakeDecay;
            public float minDuration;
            public float maxDuration;
            public float shakeFrequency;
            public float xyRatio;
            public float zShakeAmount;
            public int randomSeed;
        }

        /// <summary>
        /// シェイク状態を取得
        /// </summary>
        public ShakeState ToShakeState()
        {
            return new ShakeState
            {
                shakeIntensity = shakeIntensity,
                shakeDecay = shakeDecay,
                minDuration = minDuration,
                maxDuration = maxDuration,
                shakeFrequency = shakeFrequency,
                xyRatio = xyRatio,
                zShakeAmount = zShakeAmount,
                randomSeed = randomSeed
            };
        }

        /// <summary>
        /// シェイク状態を適用
        /// </summary>
        public void FromShakeState(ShakeState state)
        {
            shakeIntensity = state.shakeIntensity;
            shakeDecay = state.shakeDecay;
            minDuration = state.minDuration;
            maxDuration = state.maxDuration;
            shakeFrequency = state.shakeFrequency;
            xyRatio = state.xyRatio;
            zShakeAmount = state.zShakeAmount;
            randomSeed = state.randomSeed;
            _random = new System.Random(randomSeed);
        }
    }
}
