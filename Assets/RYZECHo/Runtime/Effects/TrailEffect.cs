using UnityEngine;

namespace RYZECHo.Runtime.Effects
{
    /// <summary>
    /// 軌跡エフェクト。
    /// オブジェクトの移動経路に軌跡（トレイル）を表示する。
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TrailEffect : MonoBehaviour
    {
        [Header("Trail Settings")]
        [SerializeField] private float trailLength = 1f;
        [SerializeField] private int maxPoints = 32;
        [SerializeField] private float pointInterval = 0.03f;
        [SerializeField] private Color startColor = new Color(0.3f, 0.9f, 1f, 0.8f);
        [SerializeField] private Color endColor = new Color(0.3f, 0.9f, 1f, 0f);
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private float fadeSpeed = 3f;

        [Header("Shape")]
        [SerializeField] private bool useGradient = true;
        [SerializeField] private bool circular = false;
        [SerializeField] private float circularRadius = 0.5f;

        private LineRenderer lineRenderer;
        private Vector3[] positions;
        private Color[] colors;
        private int pointCount;
        private float timer;
        private Vector3 lastPosition;
        private bool _isActive = true;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = maxPoints;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth * 0.3f;
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = endColor;
            lineRenderer.material = CreateTrailMaterial();
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;

            positions = new Vector3[maxPoints];
            colors = new Color[maxPoints];
            pointCount = 0;
            timer = 0f;
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (!_isActive) return;

            // 軌跡ポイントの追加
            float dist = Vector3.Distance(transform.position, lastPosition);
            if (dist > pointInterval)
            {
                AddPoint(transform.position);
                lastPosition = transform.position;
            }

            // 古いポイントをフェード
            timer += Time.deltaTime;
            UpdateColors();

            // 指定長さ以上なら古いポイントを削除
            TrimPoints();
        }

        private void AddPoint(Vector3 position)
        {
            int idx = pointCount % maxPoints;
            positions[idx] = position;
            colors[idx] = startColor;
            pointCount++;
            lineRenderer.positionCount = Mathf.Min(pointCount, maxPoints);
        }

        private void UpdateColors()
        {
            int visibleCount = Mathf.Min(pointCount, maxPoints);
            for (int i = 0; i < visibleCount; i++)
            {
                float age = timer - (i * pointInterval);
                float fade = Mathf.Clamp01(1f - age * fadeSpeed);

                if (useGradient)
                {
                    float t = i / (float)visibleCount;
                    colors[i] = Color.Lerp(startColor, endColor, t);
                    colors[i].a *= fade;
                }
                else
                {
                    colors[i].a = startColor.a * fade;
                }
            }
            lineRenderer.SetColors(colors);
        }

        private void TrimPoints()
        {
            if (pointCount <= maxPoints) return;

            int trimCount = pointCount - maxPoints;
            // 古いポイントをスキップ（リングバッファのため単純な削除は不可）
            // 代わりに全ポイントを更新
            for (int i = 0; i < maxPoints; i++)
            {
                int srcIdx = (pointCount - maxPoints + i) % maxPoints;
                positions[i] = positions[srcIdx];
                colors[i] = colors[srcIdx];
            }
            lineRenderer.SetPositions(positions);
        }

        /// <summary>
        /// 軌跡をリセット
        /// </summary>
        public void ResetTrail()
        {
            pointCount = 0;
            timer = 0f;
            lastPosition = transform.position;
            lineRenderer.positionCount = 0;
        }

        /// <summary>
        /// 軌跡を有効化
        /// </summary>
        public void EnableTrail()
        {
            _isActive = true;
            lineRenderer.enabled = true;
        }

        /// <summary>
        /// 軌跡を無効化
        /// </summary>
        public void DisableTrail()
        {
            _isActive = false;
            lineRenderer.enabled = false;
        }

        private Material CreateTrailMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            return shader != null ? new Material(shader) : null;
        }

        private void OnValidate()
        {
            maxPoints = Mathf.Max(2, maxPoints);
            lineWidth = Mathf.Max(0.01f, lineWidth);
            pointInterval = Mathf.Max(0.001f, pointInterval);
        }

        private void OnDestroy()
        {
            if (positions != null) System.Array.Clear(positions, 0, positions.Length);
            if (colors != null) System.Array.Clear(colors, 0, colors.Length);
        }
    }
}
