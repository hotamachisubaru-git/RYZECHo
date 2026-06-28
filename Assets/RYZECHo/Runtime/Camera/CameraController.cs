using UnityEngine;
using System;
using RYZECHo;

namespace RYZECHo
{
    /// <summary>
    /// カメラのメインコントローラー。移動、ズーム、回転を管理する。
    /// カメラの境界制限（マップ外に出ない）も実装。
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [Tooltip("移動速度")]
        [SerializeField] private float moveSpeed = 20f;

        [Tooltip("移動加速")]
        [SerializeField] private float moveAcceleration = 50f;

        [Tooltip("移動減速")]
        [SerializeField] private float moveDeceleration = 15f;

        [Header("Zoom Settings")]
        [Tooltip("最小ズーム")]
        [SerializeField] private float minZoom = 3f;

        [Tooltip("最大ズーム")]
        [SerializeField] private float maxZoom = 15f;

        [Tooltip("ズーム速度")]
        [SerializeField] private float zoomSpeed = 5f;

        [Header("Rotation Settings")]
        [Tooltip("回転速度")]
        [SerializeField] private float rotationSpeed = 90f;

        [Header("Bounds")]
        [Tooltip("マップ幅")]
        [SerializeField] private float mapWidth = 18f;

        [Tooltip("マップ高さ")]
        [SerializeField] private float mapHeight = 12f;

        [Tooltip("境界マージン")]
        [SerializeField] private float boundaryMargin = 2f;

        [Header("Input Sensitivity")]
        [Tooltip("マウス感度")]
        [SerializeField] private float mouseSensitivity = 0.3f;

        [Tooltip("スクロール感度")]
        [SerializeField] private float scrollSensitivity = 3f;

        // 内部状態
        private Camera _camera;
        private Vector2 _currentVelocity = Vector2.zero;
        private float _currentZoom = 8f;
        private float _rotationAngle = 0f;

        // ビューモード
        private CameraViewSwitcher _viewSwitcher;
        private CameraZoomManager _zoomManager;

        public Camera Camera => _camera;
        public Vector3 Position => transform.position;
        public float Zoom => _currentZoom;
        public float RotationAngle => _rotationAngle;
        public CameraViewSwitcher ViewSwitcher => _viewSwitcher;
        public CameraZoomManager ZoomManager => _zoomManager;

        // イベント: ズーム変更時
        public event Action<float> OnZoomChanged;
        // イベント: 回転変更時
        public event Action<float> OnRotationChanged;
        // イベント: 境界到達時
        public event Action<Vector2> OnBoundaryReached;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _viewSwitcher = GetComponent<CameraViewSwitcher>();
            _zoomManager = GetComponent<CameraZoomManager>();
        }

        private void Start()
        {
            // デフォルトズームを適用
            ApplyZoom(_currentZoom);
        }

        private void Update()
        {
            HandleInput();
            ClampToBoundary();
        }

        /// <summary>
        /// 入力処理（移動、ズーム、回転）
        /// </summary>
        private void HandleInput()
        {
            // マウスドラッグでカメラ移動
            if (Input.mouseScrollDelta.y != 0)
            {
                float scrollDelta = Input.mouseScrollDelta.y * scrollSensitivity;
                ZoomBy(scrollDelta);
            }

            // 右クリックで回転
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                RotateCamera(mouseX * rotationSpeed * Time.deltaTime);
            }

            // WASD/矢印キーで移動
            float moveX = 0f;
            float moveY = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveY += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveY -= 1f;

            // Shiftで加速
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                ? moveAcceleration : moveSpeed;

            if (moveX != 0 || moveY != 0)
            {
                MoveBy(moveX * currentSpeed * Time.deltaTime, moveY * currentSpeed * Time.deltaTime);
            }
            else
            {
                // 移動なし時は減速
                _currentVelocity = Vector2.Scale(_currentVelocity,
                    new Vector2(Mathf.Max(0, 1f - moveDeceleration * Time.deltaTime),
                                Mathf.Max(0, 1f - moveDeceleration * Time.deltaTime)));
            }
        }

        /// <summary>
        /// カメラを移動
        /// </summary>
        public void MoveBy(float deltaX, float deltaY)
        {
            _currentVelocity += new Vector2(deltaX, deltaY);
        }

        /// <summary>
        /// ズームを実行
        /// </summary>
        public void ZoomBy(float delta)
        {
            float newZoom = _currentZoom - delta;
            ZoomTo(newZoom);
        }

        /// <summary>
        /// ズームをターゲット値まで実行
        /// </summary>
        public void ZoomTo(float targetZoom)
        {
            _currentZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            ApplyZoom(_currentZoom);
            OnZoomChanged?.Invoke(_currentZoom);
        }

        /// <summary>
        /// ズーム値を適用（正射投影カメラ用）
        /// </summary>
        private void ApplyZoom(float zoom)
        {
            if (_camera != null && _camera.orthographic)
            {
                _camera.orthographicSize = zoom;
            }
        }

        /// <summary>
        /// カメラを回転
        /// </summary>
        public void RotateCamera(float deltaAngle)
        {
            _rotationAngle += deltaAngle;
            // -180〜180に正規化
            if (_rotationAngle > 180f) _rotationAngle -= 360f;
            if (_rotationAngle < -180f) _rotationAngle += 360f;

            transform.rotation = Quaternion.Euler(0f, 0f, _rotationAngle);
            OnRotationChanged?.Invoke(_rotationAngle);
        }

        /// <summary>
        /// 境界制限を適用（マップ外に出ない）
        /// </summary>
        private void ClampToBoundary()
        {
            float halfMapWidth = mapWidth * 0.5f + boundaryMargin;
            float halfMapHeight = mapHeight * 0.5f + boundaryMargin;

            float minX = -halfMapWidth;
            float maxX = halfMapWidth;
            float minY = -halfMapHeight;
            float maxY = halfMapHeight;

            bool clampedX = false;
            bool clampedY = false;

            if (transform.position.x < minX)
            {
                transform.position = new Vector3(minX, transform.position.y, transform.position.z);
                clampedX = true;
            }
            else if (transform.position.x > maxX)
            {
                transform.position = new Vector3(maxX, transform.position.y, transform.position.z);
                clampedX = true;
            }

            if (transform.position.y < minY)
            {
                transform.position = new Vector3(transform.position.x, minY, transform.position.z);
                clampedY = true;
            }
            else if (transform.position.y > maxY)
            {
                transform.position = new Vector3(transform.position.x, maxY, transform.position.z);
                clampedY = true;
            }

            if (clampedX || clampedY)
            {
                OnBoundaryReached?.Invoke(new Vector2(
                    clampedX ? (transform.position.x == minX || transform.position.x == maxX ? 1f : 0f) : 0f,
                    clampedY ? (transform.position.y == minY || transform.position.y == maxY ? 1f : 0f) : 0f));
            }
        }

        /// <summary>
        /// カメラを指定位置にリセット
        /// </summary>
        public void ResetCamera(Vector3 position, float zoom, float rotation)
        {
            transform.position = position;
            _currentZoom = zoom;
            _rotationAngle = rotation;
            ApplyZoom(zoom);
            transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        }

        /// <summary>
        /// カメラを特定の位置にスムーズ移動
        /// </summary>
        public void MoveTo(Vector3 targetPosition, float duration = 0.3f)
        {
            StartCoroutine(MoveToCoroutine(targetPosition, duration));
        }

        private System.Collections.IEnumerator MoveToCoroutine(Vector3 target, float duration)
        {
            Vector3 startPos = transform.position;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                transform.position = Vector3.Lerp(startPos, target, t);
                yield return null;
            }

            transform.position = target;
        }

        /// <summary>
        /// カメラをターゲット位置にロック
        /// </summary>
        public void LockToPosition(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// 移動をクリア
        /// </summary>
        public void ClearMovement()
        {
            _currentVelocity = Vector2.zero;
        }

        /// <summary>
        /// 境界設定を更新
        /// </summary>
        public void UpdateBounds(float width, float height)
        {
            mapWidth = width;
            mapHeight = height;
        }

        /// <summary>
        /// 現在のカメラの状態をシリアライズ可能形式で取得
        /// </summary>
        public CameraState ToCameraState()
        {
            return new CameraState
            {
                position = transform.position,
                zoom = _currentZoom,
                rotation = _rotationAngle,
                minZoom = minZoom,
                maxZoom = maxZoom
            };
        }

        /// <summary>
        /// カメラの状態を適用
        /// </summary>
        public void FromCameraState(CameraState state)
        {
            transform.position = state.position;
            _currentZoom = state.zoom;
            _rotationAngle = state.rotation;
            ApplyZoom(_currentZoom);
            transform.rotation = Quaternion.Euler(0f, 0f, state.rotation);
        }

        /// <summary>
        /// カメラ状態のシリアライズ用データ構造
        /// </summary>
        [Serializable]
        public struct CameraState
        {
            public Vector3 position;
            public float zoom;
            public float rotation;
            public float minZoom;
            public float maxZoom;
        }
    }
}
