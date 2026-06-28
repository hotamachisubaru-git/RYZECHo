using UnityEngine;
using System;

namespace RYZECHo
{
    /// <summary>
    /// ズームイン/アウトの管理。最小/最大ズーム、ズーム速度を制御。
    /// CameraControllerと連携してスムーズなズーム操作を実現。
    /// </summary>
    public class CameraZoomManager : MonoBehaviour
    {
        [Header("Zoom Limits")]
        [Tooltip("最小ズーム（最大拡大）")]
        [SerializeField] private float minZoom = 3f;

        [Tooltip("最大ズーム（最大縮小）")]
        [SerializeField] private float maxZoom = 15f;

        [Header("Zoom Speed")]
        [Tooltip("ズーム速度")]
        [SerializeField] private float zoomSpeed = 8f;

        [Tooltip("ズームの滑らかさ（0=瞬時, 1=無限）")]
        [SerializeField] private float zoomSmoothness = 0.15f;

        [Header("Zoom Steps")]
        [Tooltip("ズームインステップ")]
        [SerializeField] private float zoomInStep = 1f;

        [Tooltip("ズームアウトステップ")]
        [SerializeField] private float zoomOutStep = 1f;

        [Header("Scroll Settings")]
        [Tooltip("スクロール感度")]
        [SerializeField] private float scrollSensitivity = 3f;

        [Tooltip("スクロールの最小閾値")]
        [SerializeField] private float scrollThreshold = 0.1f;

        // 内部状態
        private CameraController _cameraController;
        private float _targetZoom;
        private float _currentZoom;
        private bool _isZooming;

        public float MinZoom => minZoom;
        public float MaxZoom => maxZoom;
        public float CurrentZoom => _currentZoom;
        public float TargetZoom => _targetZoom;
        public bool IsZooming => _isZooming;
        public float ZoomSpeed => zoomSpeed;

        // イベント
        public event Action<float, float> OnZoomStarted;  // (from, to)
        public event Action<float> OnZoomCompleted;       // (finalZoom)
        public event Action<float> OnZoomChanged;         // (currentZoom)

        private void Awake()
        {
            _cameraController = GetComponent<CameraController>();
            _targetZoom = 8f;
            _currentZoom = 8f;
        }

        private void Update()
        {
            HandleSmoothZoom();
            HandleScrollInput();
        }

        /// <summary>
        /// スクロール入力によるズーム
        /// </summary>
        private void HandleScrollInput()
        {
            float scroll = Input.mouseScrollDelta.y;

            if (Mathf.Abs(scroll) < scrollThreshold)
                return;

            float direction = scroll > 0 ? -1f : 1f; // 上=ズームイン, 下=ズームアウト
            float zoomAmount = direction * scroll * scrollSensitivity;

            ZoomBy(zoomAmount);
        }

        /// <summary>
        /// ズーム量を指定してズーム
        /// </summary>
        public void ZoomBy(float amount)
        {
            _targetZoom = Mathf.Clamp(_targetZoom + amount, minZoom, maxZoom);

            if (!_isZooming)
            {
                _isZooming = true;
                OnZoomStarted?.Invoke(_currentZoom, _targetZoom);
            }
        }

        /// <summary>
        /// ズームをターゲット値まで実行
        /// </summary>
        public void ZoomTo(float targetZoom)
        {
            _targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);

            if (Mathf.Abs(_currentZoom - _targetZoom) > 0.001f)
            {
                _isZooming = true;
                OnZoomStarted?.Invoke(_currentZoom, _targetZoom);
            }
        }

        /// <summary>
        /// ズームイン（ステップ単位）
        /// </summary>
        public void ZoomIn()
        {
            ZoomBy(-zoomInStep);
        }

        /// <summary>
        /// ズームアウト（ステップ単位）
        /// </summary>
        public void ZoomOut()
        {
            ZoomBy(zoomOutStep);
        }

        /// <summary>
        /// スムーズズーム更新
        /// </summary>
        private void HandleSmoothZoom()
        {
            if (!_isZooming)
                return;

            float zoomDelta = (_targetZoom - _currentZoom) * zoomSmoothness;

            if (Mathf.Abs(zoomDelta) < 0.001f)
            {
                _currentZoom = _targetZoom;
                _isZooming = false;
                OnZoomCompleted?.Invoke(_currentZoom);
            }
            else
            {
                _currentZoom += zoomDelta;
                OnZoomChanged?.Invoke(_currentZoom);
            }

            // カメラに適用
            if (_cameraController != null)
            {
                _cameraController.ZoomTo(_currentZoom);
            }
        }

        /// <summary>
        /// ズームをリセット
        /// </summary>
        public void ResetZoom()
        {
            ZoomTo(8f); // デフォルトズーム
        }

        /// <summary>
        /// ズーム値を設定（直接）
        /// </summary>
        public void SetZoom(float zoom)
        {
            _currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            _targetZoom = _currentZoom;
            _isZooming = false;

            if (_cameraController != null)
            {
                _cameraController.ZoomTo(_currentZoom);
            }
        }

        /// <summary>
        /// 現在のズームをシリアライズ可能形式で取得
        /// </summary>
        public ZoomState ToZoomState()
        {
            return new ZoomState
            {
                minZoom = minZoom,
                maxZoom = maxZoom,
                currentZoom = _currentZoom,
                targetZoom = _targetZoom,
                zoomSpeed = zoomSpeed,
                zoomSmoothness = zoomSmoothness
            };
        }

        /// <summary>
        /// ズーム状態を適用
        /// </summary>
        public void FromZoomState(ZoomState state)
        {
            minZoom = state.minZoom;
            maxZoom = state.maxZoom;
            zoomSpeed = state.zoomSpeed;
            zoomSmoothness = state.zoomSmoothness;
            SetZoom(state.currentZoom);
        }

        /// <summary>
        /// ズーム状態のシリアライズ用データ構造
        /// </summary>
        [Serializable]
        public struct ZoomState
        {
            public float minZoom;
            public float maxZoom;
            public float currentZoom;
            public float targetZoom;
            public float zoomSpeed;
            public float zoomSmoothness;
        }
    }
}
