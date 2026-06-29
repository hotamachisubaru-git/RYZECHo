using UnityEngine;

namespace RYZECHo.Runtime.Effects
{
    /// <summary>
    /// ダメージ数値のフローティングテキストエフェクト。
    /// ヒットした位置から数値が浮き上がり、フェードアウトする。
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        [Header("Text Settings")]
        [SerializeField] private string damageText = "100";
        [SerializeField] private int fontSize = 24;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.4f);

        [Header("Animation")]
        [SerializeField] private float duration = 1.2f;
        [SerializeField] private float upwardSpeed = 1.5f;
        [SerializeField] private float fadeStart = 0.5f;

        [Header("Style")]
        [SerializeField] private bool isCritical = false;
        [SerializeField] private bool isHeal = false;
        [SerializeField] private float scaleOnSpawn = 1.5f;
        [SerializeField] private float scaleTarget = 0.8f;

        private TextMesh textMesh;
        private float _elapsed;
        private bool _isActive = false;

        /// <summary>
        /// ダメージ数値を表示
        /// </summary>
        public void Show(float damage, bool isCrit = false, Vector3? position = null)
        {
            damageText = Mathf.RoundToInt(Mathf.Abs(damage)).ToString();
            isCritical = isCrit;
            isHeal = damage < 0;

            if (position.HasValue)
                transform.position = position.Value;

            _isActive = true;
            _elapsed = 0f;

            SetupText();
        }

        private void SetupText()
        {
            if (textMesh == null)
            {
                var go = new GameObject("DamageNumberText");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                textMesh = go.AddComponent<TextMesh>();
            }

            textMesh.text = damageText;
            textMesh.fontSize = fontSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;

            if (isCritical)
                textMesh.color = criticalColor;
            else if (isHeal)
                textMesh.color = healColor;
            else
                textMesh.color = normalColor;

            // スケールアニメーション用
            float s = scaleOnSpawn;
            textMesh.transform.localScale = new Vector3(s, s, s);

            textMesh.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive || textMesh == null || !textMesh.gameObject.activeSelf) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / duration);

            // 浮き上がり
            textMesh.transform.position += Vector3.up * upwardSpeed * Time.deltaTime;

            // スケール補間
            float currentScale = Mathf.Lerp(scaleOnSpawn, scaleTarget, t);
            textMesh.transform.localScale = new Vector3(currentScale, currentScale, currentScale);

            // フェード
            if (t > fadeStart)
            {
                float fadeT = (t - fadeStart) / (1f - fadeStart);
                var c = textMesh.color;
                c = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, fadeT));
                textMesh.color = c;
            }

            if (t >= 1f)
            {
                _isActive = false;
                textMesh.gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            _isActive = false;
            if (textMesh != null) textMesh.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (textMesh != null && textMesh.gameObject != null)
                Destroy(textMesh.gameObject);
        }
    }
}
