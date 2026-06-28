using UnityEngine;
using System;

namespace RYZECHo
{
    /// <summary>
    /// ターゲット追従カメラ。Lerp/SmoothDampによるスムーズ追従。
    /// プレイヤーや特定のオブジェクトを追従するように設定可能。
    /// </summary>
    public class CameraFollowTarget : MonoBehaviour
    {
        [Header("Follow Target")]
        [Tooltip("追従ターゲット")]
        [SerializeField] private Transform followTarget;

        [Tooltip("追従オフセット")]
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -10f);

        [Header("Follow Settings")]
        [Tooltip("Lerp速度（0=固定, 1=即時）")]
        [SerializeField] private float followLerpSpeed = 12f;

        [Tooltip("SmoothDamp速度")]
        [SerializeField] private float followSmoothSpeed = 8f;

        [Tooltip("SmoothDamp滑らかさ")]
        [SerializeField] private float followSmoothTime = 0.15f;

        [Header("Zoom Follow")]
        [Tooltip("ターゲット追従時のズーム追従")]
        [SerializeField] private bool followZoom = true;

        [Tooltip("ズーム追従速度")]
        [SerializeField] private float zoomFollowSpeed = 5f;

        [Header("Bounds")]
        [Tooltip("追従範囲の上限")]
        [SerializeField] private Vector3 boundsMax = new Vector3(9f, 6f, 0f);

        [Tooltip("追従範囲の下限")]
        [SerializeField] private Vector3 boundsMin = new Vector3(-9f, -6f, 0f);

        [Header("LookAhead")]
        [Tooltip("ターゲットの速度に基づくLookAhead")]
        [SerializeField] private bool enableLookAhead = true;

        [Tooltip("LookAheadの強さ")]
        [SerializeField] private float lookAheadStrength = 0.5f;

        [Tooltip("LookAheadの最大値")]
        [SerializeField] private float lookAheadMax = 3f;

        // 内部状態
        private Vector3 _currentVelocity;
        private Vector3 _currentZoomVelocity;
        private Vector3 _lastTargetPosition;
        private Vector3 _targetVelocity;

        public Transform FollowTarget
        {
            get => followTarget;
            set => followTarget = value;
        }

        public Vector3 FollowOffset
        {
            get => followOffset;
            set => followOffset = value;
        }

        public bool IsFollowing => followTarget != null;
        public Vector3 TargetVelocity => _targetVelocity;

        // イベント
        public event Action<Transform> OnTargetChanged;
        public event Action OnFollowStarted;
        public event Action OnFollowStopped;

        private void Start()
        {
            _lastTargetPosition = followTarget?.position ?? Vector3.zero;
        }

        private void LateUpdate()
        {
            if (followTarget == null)
                return;

            Follow();
        }

        /// <summary>
        /// ターゲット追従カメラの更新
        /// </summary>
        private void Follow()
        {
            // ターゲットの速度を計算
            _targetVelocity = (followTarget.position - _lastTargetPosition) / Time.deltaTime;
            _lastTargetPosition = followTarget.position;

            // LookAheadを計算
            Vector3 lookAhead = Vector3.zero;
            if (enableLookAhead && _targetVelocity.magnitude > 0.1f)
            {
                float speed = Mathf.Clamp01(_targetVelocity.magnitude / 20f);
                lookAhead.x = Mathf.Sign(_targetVelocity.x) * speed * lookAheadStrength;
                lookAhead.y = Mathf.Sign(_targetVelocity.y) * speed * lookAheadStrength;
                lookAhead = Vector3.ClampMagnitude(lookAhead, lookAheadMax);
            }

            // 目標位置を計算
            Vector3 targetPosition = followTarget.position + followOffset + lookAhead;

            // 境界制限
            targetPosition.x = Mathf.Clamp(targetPosition.x, boundsMin.x, boundsMax.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, boundsMin.y, boundsMax.y);

            // Lerpで追従
            Vector3 currentPos = transform.position;
            Vector3 smoothedPos;

            if (followLerpSpeed > 20f)
            {
                // 高速時はSmoothDampを使用
                smoothedPos = Vector3.SmoothDamp(currentPos, targetPosition, ref _currentVelocity, followSmoothTime);
            }
            else
            {
                // Lerpを使用
                float t = Mathf.Min(1f, followLerpSpeed * Time.deltaTime);
                smoothedPos = Vector3.Lerp(currentPos, targetPosition, t);
            }

            transform.position = smoothedPos;

            // ズーム追従
            if (followZoom)
            {
                HandleZoomFollow();
            }
        }

        /// <summary>
        /// ズーム追従の処理
        /// </summary>
        private void HandleZoomFollow()
        {
            // ターゲットの距離に基づくズーム変化（例：ターゲットが遠ざかるとズームアウト）
            // 実装はCameraZoomManagerに委譲
        }

        /// <summary>
        /// ターゲットを設定（追従開始）
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            if (target == followTarget)
                return;

            followTarget = target;
            _lastTargetPosition = target?.position ?? Vector3.zero;
            _currentVelocity = Vector3.zero;

            if (target != null)
            {
                OnFollowStarted?.Invoke();
            }
            else
            {
                OnFollowStopped?.Invoke();
            }

            OnTargetChanged?.Invoke(target);
        }

        /// <summary>
        /// 追従をオフ
        /// </summary>
        public void StopFollow()
        {
            followTarget = null;
            OnFollowStopped?.Invoke();
        }

        /// <summary>
        /// 追従を再開
        /// </summary>
        public void ResumeFollow()
        {
            if (followTarget != null)
            {
                _lastTargetPosition = followTarget.position;
                _currentVelocity = Vector3.zero;
                OnFollowStarted?.Invoke();
            }
        }

        /// <summary>
        /// オフセットを更新
        /// </summary>
        public void SetFollowOffset(Vector3 offset)
        {
            followOffset = offset;
        }

        /// <summary>
        /// 追従速度を更新
        /// </summary>
        public void SetFollowSpeed(float speed)
        {
            followLerpSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// 境界を更新
        /// </summary>
        public void SetBounds(Vector3 min, Vector3 max)
        {
            boundsMin = min;
            boundsMax = max;
        }

        /// <summary>
        /// 追従状態をシリアライズ可能形式で取得
        /// </summary>
        public FollowState ToFollowState()
        {
            return new FollowState
            {
                targetGuid = followTarget != null ? followTarget.name : "",
                offset = followOffset,
                lerpSpeed = followLerpSpeed,
                smoothSpeed = followSmoothSpeed,
                smoothTime = followSmoothTime,
                boundsMin = boundsMin,
                boundsMax = boundsMax,
                enableLookAhead = enableLookAhead,
                lookAheadStrength = lookAheadStrength,
                lookAheadMax = lookAheadMax
            };
        }

        /// <summary>
        /// 追従状態を適用
        /// </summary>
        public void FromFollowState(FollowState state)
        {
            followOffset = state.offset;
            followLerpSpeed = state.lerpSpeed;
            followSmoothSpeed = state.smoothSpeed;
            followSmoothTime = state.smoothTime;
            boundsMin = state.boundsMin;
            boundsMax = state.boundsMax;
            enableLookAhead = state.enableLookAhead;
            lookAheadStrength = state.lookAheadStrength;
            lookAheadMax = state.lookAheadMax;
        }

        /// <summary>
        /// 追従状態のシリアライズ用データ構造
        /// </summary>
        [Serializable]
        public struct FollowState
        {
            public string targetGuid;
            public Vector3 offset;
            public float lerpSpeed;
            public float smoothSpeed;
            public float smoothTime;
            public Vector3 boundsMin;
            public Vector3 boundsMax;
            public bool enableLookAhead;
            public float lookAheadStrength;
            public float lookAheadMax;
        }
    }
}
