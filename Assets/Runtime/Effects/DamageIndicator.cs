using UnityEngine;

namespace RYZECHo.Runtime.Effects
{
    /// <summary>
    /// ダメージを受けた時に画面にインジケーターを表示するエフェクト。
    /// ダメージの方向と量を視覚的に示す。
    /// </summary>
    public class DamageIndicator : MonoBehaviour
    {
        [Header("Indicator Settings")]
        [SerializeField] private float duration = 1.5f;
        [SerializeField] private float speed = 2f;
        [SerializeField] private Color indicatorColor = new Color(1f, 0.3f, 0.2f, 0.8f);
        [SerializeField] private float lineWidth = 0.08f;
        [SerializeField] private float arrowSize = 0.3f;

        [Header("Damage Amount")]
        [SerializeField] private bool showDamageAmount = true;
        [SerializeField] private float damageScale = 0.02f;

        private LineRenderer line;
        private TextMesh damageText;
        private float _elapsed;
        private Vector3 _startPos;
        private Vector3 _endPos;
        private bool _isActive = false;

        public void Show(Vector3 fromPosition, float damageAmount)
        {
            _isActive = true;
            _elapsed = 0f;

            // 画面中央を基準にダメージ方向を計算
            var screenCenter = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position)
                : new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

            var direction = (fromPosition - transform.position).normalized;
            _startPos = transform.position;
            _endPos = transform.position + direction * 3f;

            if (line == null)
            {
                var go = new GameObject("DamageIndicatorLine");
                go.transform.SetParent(transform);
                line = go.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.startWidth = lineWidth;
                line.endWidth = lineWidth * 0.5f;
                line.startColor = indicatorColor;
                line.endColor = new Color(indicatorColor.r, indicatorColor.g, indicatorColor.b, 0f);
                line.material = CreateArrowMaterial();
                line.useWorldSpace = true;
            }

            line.SetPosition(0, _startPos);
            line.SetPosition(1, _endPos);

            // ダメージ数値表示
            if (showDamageAmount)
            {
                if (damageText == null)
                {
                    var textGO = new GameObject("DamageText");
                    textGO.transform.SetParent(transform);
                    damageText = textGO.AddComponent<TextMesh>();
                    damageText.fontSize = 24;
                    damageText.alignment = TextAlignment.Center;
                    damageText.anchor = TextAnchor.MiddleCenter;
                    damageText.color = indicatorColor;
                }
                damageText.text = Mathf.RoundToInt(damageAmount).ToString();
                float scale = 1f + damageAmount * damageScale;
                damageText.transform.localScale = new Vector3(scale, scale, scale);
                damageText.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            _isActive = false;
            if (line != null) line.enabled = false;
            if (damageText != null) damageText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / duration);

            // フェードアウト
            if (line != null)
            {
                var startColor = line.startColor;
                startColor = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(indicatorColor.a, 0f, t));
                line.startColor = startColor;

                var endColor = line.endColor;
                endColor = new Color(endColor.r, endColor.g, endColor.b, Mathf.Lerp(indicatorColor.a * 0.5f, 0f, t));
                line.endColor = endColor;

                // 縮小アニメーション
                float shrink = 1f - t * 0.3f;
                var currentEnd = _endPos;
                var center = _startPos;
                line.SetPosition(1, Vector3.Lerp(_startPos, currentEnd, shrink));
            }

            // ダメージテキスト
            if (damageText != null && damageText.gameObject.activeSelf)
            {
                damageText.transform.position = Vector3.Lerp(
                    _startPos,
                    _startPos + Vector3.up * 1f,
                    t);
                var tc = damageText.color;
                tc = new Color(tc.r, tc.g, tc.b, Mathf.Lerp(indicatorColor.a, 0f, t));
                damageText.color = tc;

                if (t >= 1f)
                {
                    damageText.gameObject.SetActive(false);
                    _isActive = false;
                }
            }
        }

        private Material CreateArrowMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            return shader != null ? new Material(shader) : null;
        }

        private void OnDestroy()
        {
            if (line != null && line.gameObject != null)
                Destroy(line.gameObject);
            if (damageText != null && damageText.gameObject != null)
                Destroy(damageText.gameObject);
        }
    }
}
