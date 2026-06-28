using UnityEngine;

namespace RYZECHo.Runtime.Effects
{
    /// <summary>
    /// 選択中のオブジェクトを点滅ハイライトするエフェクト。
    /// Unity URP対応のSpriteRendererベースの点滅実装。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SelectionHighlight : MonoBehaviour
    {
        [Header("Highlight Settings")]
        [Tooltip("点滅の色")]
        [SerializeField] private Color highlightColor = new Color(0.2f, 0.9f, 1f, 0.6f);

        [Tooltip("点滅の速さ（Hz）")]
        [SerializeField] private float blinkRate = 3f;

        [Tooltip("最小アルファ")]
        [SerializeField] private float minAlpha = 0.15f;

        [Tooltip("最大アルファ")]
        [SerializeField] private float maxAlpha = 0.8f;

        [Header("Outline")]
        [Tooltip("アウトラインの有効化")]
        [SerializeField] private bool enableOutline = true;

        [Tooltip("アウトラインの色")]
        [SerializeField] private Color outlineColor = Color.white;

        [Tooltip("アウトラインの太さ")]
        [SerializeField] private float outlineWidth = 0.05f;

        private SpriteRenderer _renderer;
        private float _elapsed;
        private bool _isHighlighted = true;
        private bool _isActive = true;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                enabled = false;
                return;
            }

            // アウトライン用の追加SpriteRendererを作成
            if (enableOutline && _renderer.sprite != null)
            {
                CreateOutlineSprite();
            }
        }

        private void Start()
        {
            _renderer.color = highlightColor;
        }

        private void Update()
        {
            if (!_isActive || _renderer == null) return;

            _elapsed += Time.deltaTime * blinkRate;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, Mathf.PingPong(_elapsed, 1f));

            var c = _renderer.color;
            c.a = alpha;
            _renderer.color = c;
        }

        /// <summary>
        /// ハイライトを有効化
        /// </summary>
        public void EnableHighlight()
        {
            _isActive = true;
            _renderer.enabled = true;
            if (outlineRenderer != null) outlineRenderer.enabled = true;
        }

        /// <summary>
        /// ハイライトを無効化
        /// </summary>
        public void DisableHighlight()
        {
            _isActive = false;
            _renderer.enabled = false;
            if (outlineRenderer != null) outlineRenderer.enabled = false;
        }

        /// <summary>
        /// 点滅を一時停止
        /// </summary>
        public void PauseBlink(bool pause)
        {
            if (pause)
            {
                var c = _renderer.color;
                c.a = maxAlpha;
                _renderer.color = c;
            }
            else
            {
                _elapsed = 0f;
            }
        }

        private SpriteRenderer outlineRenderer;

        private void CreateOutlineSprite()
        {
            if (_renderer.sprite == null) return;

            var outlineGO = new GameObject("SelectionOutline");
            outlineGO.transform.SetParent(transform);
            outlineGO.transform.localPosition = Vector3.zero;
            outlineGO.transform.localScale = Vector3.one;

            outlineRenderer = outlineGO.AddComponent<SpriteRenderer>();
            outlineRenderer.sprite = _renderer.sprite;
            outlineRenderer.sortingOrder = _renderer.sortingOrder + 1;
            outlineRenderer.color = outlineColor;
            outlineRenderer.enabled = false;
        }

        private void OnValidate()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (highlightColor.a > 1f) highlightColor.a = Mathf.Clamp01(highlightColor.a);
            if (minAlpha > maxAlpha) (minAlpha, maxAlpha) = (maxAlpha, minAlpha);
        }
    }
}
