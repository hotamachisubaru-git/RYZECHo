using UnityEngine;
using System;

namespace RYZECHo
{
    /// <summary>
    /// ビューモードの列挙。
    /// トップダウン（デフォルト）、サイドビュー、アングルドビューを定義。
    /// </summary>
    public enum CameraViewMode
    {
        TopDown,       // トップダウン（デフォルト）
        SideView,      // サイドビュー
        AngledView,    // アングルドビュー
        Free           // フリー（自由視点）
    }

    /// <summary>
    /// ビューモードの切り替えを管理。切り替えアニメーション付き。
    /// </summary>
    public class CameraViewSwitcher : MonoBehaviour
    {
        [Header("View Presets")]
        [Tooltip("トップダウン設定")]
        [SerializeField] private ViewPreset topDownPreset = new ViewPreset
        {
            position = new Vector3(0f, 0f, -10f),
            zoom = 8f,
            rotation = 0f
        };

        [Tooltip("サイドビュー設定")]
        [SerializeField] private ViewPreset sideViewPreset = new ViewPreset
        {
            position = new Vector3(10f, 0f, 0f),
            zoom = 10f,
            rotation = 90f
        };

        [Tooltip("アングルドビュー設定")]
        [SerializeField] private ViewPreset angledViewPreset = new ViewPreset
        {
            position = new Vector3(5f, 5f, -8f),
            zoom = 8f,
            rotation = 45f
        };

        [Header("Transition")]
        [Tooltip("切り替えアニメーションの時間")]
        [SerializeField] private float transitionDuration = 0.4f;

        [Tooltip("イージングタイプ")]
        [SerializeField] private EaseType easeType = EaseType.InOutQuad;

        [Header("Input")]
        [Tooltip("ビュー切替キー")]
        [SerializeField] private KeyCode viewSwitchKey = KeyCode.Tab;

        [Tooltip("ビュー切替の修飾キー")]
        [SerializeField] private KeyCode viewSwitchModifier = KeyCode.LeftShift;

        // 内部状態
        private CameraController _cameraController;
        private CameraViewMode _currentView = CameraViewMode.TopDown;
        private CameraViewMode _targetView = CameraViewMode.TopDown;
        private bool _isTransitioning;
        private float _transitionProgress;

        public CameraViewMode CurrentView => _currentView;
        public CameraViewMode TargetView => _targetView;
        public bool IsTransitioning => _isTransitioning;
        public float TransitionProgress => _transitionProgress;

        // イベント
        public event Action<CameraViewMode> OnViewChanged;
        public event Action OnViewTransitionStarted;
        public event Action OnViewTransitionCompleted;

        private void Awake()
        {
            _cameraController = GetComponent<CameraController>();
            _currentView = CameraViewMode.TopDown;
            _targetView = CameraViewMode.TopDown;
        }

        private void Update()
        {
            HandleViewSwitchInput();

            if (_isTransitioning)
            {
                UpdateTransition();
            }
        }

        /// <summary>
        /// ビュー切替入力を処理
        /// </summary>
        private void HandleViewSwitchInput()
        {
            if (Input.GetKeyDown(viewSwitchKey) && Input.GetKey(viewSwitchModifier))
            {
                CycleView();
            }

            // 数字キーで直接ビュー切替
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToView(CameraViewMode.TopDown);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToView(CameraViewMode.SideView);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToView(CameraViewMode.AngledView);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToView(CameraViewMode.Free);
        }

        /// <summary>
        /// ビューをサイクル（順次切り替え）
        /// </summary>
        public void CycleView()
        {
            CameraViewMode next = _currentView switch
            {
                CameraViewMode.TopDown => CameraViewMode.SideView,
                CameraViewMode.SideView => CameraViewMode.AngledView,
                CameraViewMode.AngledView => CameraViewMode.Free,
                CameraViewMode.Free => CameraViewMode.TopDown,
                _ => CameraViewMode.TopDown
            };
            SwitchToView(next);
        }

        /// <summary>
        /// 指定したビューモードに切り替え（アニメーション付き）
        /// </summary>
        public void SwitchToView(CameraViewMode viewMode)
        {
            if (viewMode == _currentView)
                return;

            _targetView = viewMode;

            if (_cameraController != null)
            {
                var preset = GetPreset(viewMode);
                if (preset != null)
                {
                    _isTransitioning = true;
                    _transitionProgress = 0f;

                    OnViewTransitionStarted?.Invoke();

                    // トランジションエフェクトがあれば通知
                    var transitionEffect = GetComponent<CameraTransitionEffect>();
                    if (transitionEffect != null)
                    {
                        transitionEffect.StartTransition(viewMode);
                    }
                }
            }
            else
            {
                // カメラコントローラーがない場合は即適用
                ApplyView(viewMode);
                _currentView = viewMode;
                OnViewChanged?.Invoke(_currentView);
            }
        }

        /// <summary>
        /// トランジション更新
        /// </summary>
        private void UpdateTransition()
        {
            _transitionProgress += Time.deltaTime / transitionDuration;

            switch (easeType)
            {
                case EaseType.Linear:
                    _transitionProgress = Mathf.Clamp01(_transitionProgress);
                    break;
                case EaseType.InOutQuad:
                    _transitionProgress = InOutQuad(_transitionProgress);
                    break;
                case EaseType.InOutCubic:
                    _transitionProgress = InOutCubic(_transitionProgress);
                    break;
                case EaseType.InOutSine:
                    _transitionProgress = InOutSine(_transitionProgress);
                    break;
                case EaseType.EaseOutElastic:
                    _transitionProgress = EaseOutElastic(_transitionProgress);
                    break;
            }

            _transitionProgress = Mathf.Clamp01(_transitionProgress);

            // 現在のビューを補間
            var fromPreset = GetPreset(_currentView);
            var toPreset = GetPreset(_targetView);

            if (fromPreset != null && toPreset != null)
            {
                // 位置を補間
                Vector3 interpolatedPos = Vector3.Lerp(fromPreset.position, toPreset.position, _transitionProgress);

                // ズームを補間
                float interpolatedZoom = Mathf.Lerp(fromPreset.zoom, toPreset.zoom, _transitionProgress);

                // 回転を補間（最短経路）
                float rotationDiff = toPreset.rotation - fromPreset.rotation;
                if (rotationDiff > 180f) rotationDiff -= 360f;
                if (rotationDiff < -180f) rotationDiff += 360f;
                float interpolatedRotation = fromPreset.rotation + rotationDiff * _transitionProgress;

                if (_cameraController != null)
                {
                    _cameraController.MoveTo(interpolatedPos, 0f);
                    _cameraController.ZoomTo(interpolatedZoom);
                    _cameraController.RotateCamera(interpolatedRotation - _cameraController.RotationAngle);
                }
            }

            // 完了判定
            if (_transitionProgress >= 1f)
            {
                _isTransitioning = false;
                _currentView = _targetView;
                ApplyView(_targetView);
                OnViewTransitionCompleted?.Invoke();
                OnViewChanged?.Invoke(_currentView);
            }
        }

        /// <summary>
        /// ビューを即座に適用（アニメーションなし）
        /// </summary>
        public void ApplyViewImmediate(CameraViewMode viewMode)
        {
            var preset = GetPreset(viewMode);
            if (preset != null && _cameraController != null)
            {
                _cameraController.ResetCamera(preset.position, preset.zoom, preset.rotation);
            }
            _currentView = viewMode;
            OnViewChanged?.Invoke(_currentView);
        }

        /// <summary>
        /// 内部のビュー適用
        /// </summary>
        private void ApplyView(CameraViewMode viewMode)
        {
            var preset = GetPreset(viewMode);
            if (preset != null && _cameraController != null)
            {
                _cameraController.ResetCamera(preset.position, preset.zoom, preset.rotation);
            }
        }

        /// <summary>
        /// プセットを取得
        /// </summary>
        private ViewPreset GetPreset(CameraViewMode mode)
        {
            return mode switch
            {
                CameraViewMode.TopDown => topDownPreset,
                CameraViewMode.SideView => sideViewPreset,
                CameraViewMode.AngledView => angledViewPreset,
                _ => topDownPreset
            };
        }

        /// <summary>
        /// イージング関数: InOutQuad
        /// </summary>
        private float InOutQuad(float t) => t < 0.5f
            ? 2f * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;

        /// <summary>
        /// イージング関数: InOutCubic
        /// </summary>
        private float InOutCubic(float t) => t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

        /// <summary>
        /// イージング関数: InOutSine
        /// </summary>
        private float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;

        /// <summary>
        /// イージング関数: EaseOutElastic
        /// </summary>
        private float EaseOutElastic(float t)
        {
            if (t == 0 || t == 1) return t;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - 0.075f) * (2f * Mathf.PI) / 0.3f) + 1f;
        }

        /// <summary>
        /// ビュー設定のシリアライズ用データ構造
        /// </summary>
        [Serializable]
        public struct ViewPreset
        {
            public Vector3 position;
            public float zoom;
            public float rotation;
        }

        /// <summary>
        /// イージングタイプの列挙
        /// </summary>
        public enum EaseType
        {
            Linear,
            InOutQuad,
            InOutCubic,
            InOutSine,
            EaseOutElastic
        }
    }
}
