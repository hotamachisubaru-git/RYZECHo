using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections;

namespace RYZECHo
{
    /// <summary>
    /// ビュー切り替え時のトランジションエフェクト。
    /// フェード、ズームイン/アウト、ワープなどのエフェクトをサポート。
    /// </summary>
    public class CameraTransitionEffect : MonoBehaviour
    {
        [Header("Fade Settings")]
        [Tooltip("フェード用Canvas")]
        [SerializeField] private GameObject fadeCanvas;

        [Tooltip("フェード用Image")]
        [SerializeField] private UnityEngine.UI.Image fadeImage;

        [Tooltip("フェード色")]
        [SerializeField] private Color fadeColor = new Color(0f, 0f, 0f, 1f);

        [Tooltip("フェード時間")]
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("Zoom Transition")]
        [Tooltip("ズームトランジションを有効化")]
        [SerializeField] private bool enableZoomTransition = true;

        [Tooltip("ズームトランジション時間")]
        [SerializeField] private float zoomTransitionDuration = 0.4f;

        [Header("Warp Effect")]
        [Tooltip("ワープエフェクトを有効化")]
        [SerializeField] private bool enableWarpEffect = false;

        [Tooltip("ワープ時間")]
        [SerializeField] private float warpDuration = 0.2f;

        [Header("Blur Effect")]
        [Tooltip("ブloeエフェクトを有効化")]
        [SerializeField] private bool enableBlurEffect = false;

        [Tooltip("ブロー強度")]
        [SerializeField] private float blurIntensity = 0.5f;

        // 内部状態
        private CameraController _cameraController;
        private CameraZoomManager _zoomManager;
        private CameraViewSwitcher _viewSwitcher;
        private Coroutine _activeCoroutine;

        public bool IsTransitioning { get; private set; }

        // イベント
        public event Action OnTransitionStarted;
        public event Action OnTransitionCompleted;

        private void Awake()
        {
            _cameraController = GetComponent<CameraController>();
            _zoomManager = GetComponent<CameraZoomManager>();
            _viewSwitcher = GetComponent<CameraViewSwitcher>();

            // フェード用Imageがない場合は生成
            if (fadeImage == null && fadeCanvas != null)
            {
                var imageGO = new GameObject("FadeImage");
                imageGO.transform.SetParent(fadeCanvas.transform, false);
                fadeImage = imageGO.AddComponent<UnityEngine.UI.Image>();
                fadeImage.color = fadeColor;
                fadeImage.rectTransform.anchorMin = Vector2.zero;
                fadeImage.rectTransform.anchorMax = Vector2.one;
                fadeImage.rectTransform.sizeDelta = Vector2.zero;
                fadeImage.fillMethod = UnityEngine.UI.FillMethod.Horizontal;
                fadeImage.raycastTarget = false;
            }
        }

        /// <summary>
        /// ビュー切り替え時のトランジションを開始
        /// </summary>
        public void StartTransition(CameraViewSwitcher.CameraViewMode targetView)
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
            }

            _activeCoroutine = StartCoroutine(TransitionCoroutine(targetView));
        }

        /// <summary>
        /// フェードイン/アウトトランジション
        /// </summary>
        public void StartFadeTransition(float fadeTime = -1f, bool fadeIn = true)
        {
            if (_activeCoroutine != null)
                StopCoroutine(_activeCoroutine);

            _activeCoroutine = StartCoroutine(FadeCoroutine(fadeIn, fadeTime > 0 ? fadeTime : fadeDuration));
        }

        /// <summary>
        /// ズームインエフェクト
        /// </summary>
        public void StartZoomIn(float duration = -1f)
        {
            if (_activeCoroutine != null)
                StopCoroutine(_activeCoroutine);

            _activeCoroutine = StartCoroutine(ZoomInCoroutine(duration > 0 ? duration : zoomTransitionDuration));
        }

        /// <summary>
        /// ズームアウトエフェクト
        /// </summary>
        public void StartZoomOut(float duration = -1f)
        {
            if (_activeCoroutine != null)
                StopCoroutine(_activeCoroutine);

            _activeCoroutine = StartCoroutine(ZoomOutCoroutine(duration > 0 ? duration : zoomTransitionDuration));
        }

        /// <summary>
        /// ワープエフェクト
        /// </summary>
        public void StartWarpEffect(float duration = -1f)
        {
            if (_activeCoroutine != null)
                StopCoroutine(_activeCoroutine);

            _activeCoroutine = StartCoroutine(WarpCoroutine(duration > 0 ? duration : warpDuration));
        }

        /// <summary>
        /// トランジションを強制終了
        /// </summary>
        public void AbortTransition()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
            IsTransitioning = false;

            // フェードをリセット
            if (fadeImage != null)
            {
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            }
        }

        /// <summary>
        /// メイントランジションコルーチン
        /// </summary>
        private IEnumerator TransitionCoroutine(CameraViewSwitcher.CameraViewMode targetView)
        {
            IsTransitioning = true;
            OnTransitionStarted?.Invoke();

            // フェードアウト
            yield return StartCoroutine(FadeOutCoroutine(fadeDuration * 0.6f));

            // ビュー切り替え（ViewSwitcherがアニメーション処理）
            if (_viewSwitcher != null)
            {
                _viewSwitcher.SwitchToView(targetView);
            }

            // フェードイン
            yield return StartCoroutine(FadeInCoroutine(fadeDuration * 0.6f));

            IsTransitioning = false;
            OnTransitionCompleted?.Invoke();
        }

        /// <summary>
        /// フェードアウトコルーチン
        /// </summary>
        private IEnumerator FadeOutCoroutine(float duration)
        {
            if (fadeImage == null) yield break;

            float t = 0f;
            Color startColor = fadeImage.color;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float alpha = Mathf.Lerp(startColor.a, 1f, t);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        }

        /// <summary>
        /// フェードインコルーチン
        /// </summary>
        private IEnumerator FadeInCoroutine(float duration)
        {
            if (fadeImage == null) yield break;

            float t = 0f;
            Color startColor = fadeImage.color;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float alpha = Mathf.Lerp(startColor.a, 0f, t);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        }

        /// <summary>
        /// ズームインコルーチン
        /// </summary>
        private IEnumerator ZoomInCoroutine(float duration)
        {
            if (_zoomManager == null) yield break;

            float startZoom = _zoomManager.CurrentZoom;
            float endZoom = Mathf.Max(startZoom * 0.6f, _zoomManager.MinZoom);
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float zoom = Mathf.Lerp(startZoom, endZoom, t);
                _zoomManager.ZoomTo(zoom);
                yield return null;
            }

            _zoomManager.ZoomTo(endZoom);
        }

        /// <summary>
        /// ズームアウトコルーチン
        /// </summary>
        private IEnumerator ZoomOutCoroutine(float duration)
        {
            if (_zoomManager == null) yield break;

            float startZoom = _zoomManager.CurrentZoom;
            float endZoom = Mathf.Min(startZoom * 1.4f, _zoomManager.MaxZoom);
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float zoom = Mathf.Lerp(startZoom, endZoom, t);
                _zoomManager.ZoomTo(zoom);
                yield return null;
            }

            _zoomManager.ZoomTo(endZoom);
        }

        /// <summary>
        /// ワープコルーチン
        /// </summary>
        private IEnumerator WarpCoroutine(float duration)
        {
            if (_cameraController == null) yield break;

            Vector3 originalPos = transform.position;
            float t = 0f;

            // 1. 急速に縮小（フェード）
            yield return StartCoroutine(FadeOutCoroutine(duration * 0.3f));

            // 2. 位置をジャンプ
            float jumpX = (float)(UnityEngine.Random.value - 0.5f) * 4f;
            float jumpY = (float)(UnityEngine.Random.value - 0.5f) * 4f;
            transform.position = originalPos + new Vector3(jumpX, jumpY, 0f);

            // 3. 徐々に戻す
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / (duration * 0.7f);
                float eased = t * t * (3f - 2f * t); // smoothstep
                transform.position = Vector3.Lerp(
                    originalPos + new Vector3(jumpX, jumpY, 0f),
                    originalPos,
                    eased);
                yield return null;
            }

            transform.position = originalPos;

            // 4. フェードイン
            yield return StartCoroutine(FadeInCoroutine(duration * 0.3f));
        }

        /// <summary>
        /// トランジション効果の設定
        /// </summary>
        public void SetTransitionEffects(bool fade, bool zoom, bool warp)
        {
            enableZoomTransition = zoom;
            enableWarpEffect = warp;
        }

        /// <summary>
        /// トランジション状態のシリアライズ用データ構造
        /// </summary>
        [Serializable]
        public struct TransitionState
        {
            public bool enableZoomTransition;
            public float zoomTransitionDuration;
            public bool enableWarpEffect;
            public float warpDuration;
            public bool enableBlurEffect;
            public float blurIntensity;
            public float fadeDuration;
            public Color fadeColor;
        }

        /// <summary>
        /// トランジション状態を取得
        /// </summary>
        public TransitionState ToTransitionState()
        {
            return new TransitionState
            {
                enableZoomTransition = enableZoomTransition,
                zoomTransitionDuration = zoomTransitionDuration,
                enableWarpEffect = enableWarpEffect,
                warpDuration = warpDuration,
                enableBlurEffect = enableBlurEffect,
                blurIntensity = blurIntensity,
                fadeDuration = fadeDuration,
                fadeColor = fadeColor
            };
        }

        /// <summary>
        /// トランジション状態を適用
        /// </summary>
        public void FromTransitionState(TransitionState state)
        {
            enableZoomTransition = state.enableZoomTransition;
            zoomTransitionDuration = state.zoomTransitionDuration;
            enableWarpEffect = state.enableWarpEffect;
            warpDuration = state.warpDuration;
            enableBlurEffect = state.enableBlurEffect;
            blurIntensity = state.blurIntensity;
            fadeDuration = state.fadeDuration;
            fadeColor = state.fadeColor;
        }
    }
}
