using UnityEngine;

namespace RYZECHo.Runtime.Effects
{
    /// <summary>
    /// 選択リング（円形エフェクト）。
    /// 選択中のオブジェクトの周りに回転する円形エフェクトを表示する。
    /// </summary>
    public class SelectionRing : MonoBehaviour
    {
        [Header("Ring Settings")]
        [Tooltip("リングの半径")]
        [SerializeField] private float radius = 0.8f;

        [Tooltip("リングの太さ")]
        [SerializeField] private float lineWidth = 0.04f;

        [Tooltip("リングの色")]
        [SerializeField] private Color ringColor = new Color(0.3f, 0.95f, 1f, 0.7f);

        [Tooltip("回転速度")]
        [SerializeField] private float rotationSpeed = 60f;

        [Header("Pulse")]
        [Tooltip("パルスエフェクトの有効化")]
        [SerializeField] private bool enablePulse = true;

        [Tooltip("パルスの速さ")]
        [SerializeField] private float pulseRate = 2f;

        [Tooltip("パルスの拡大率")]
        [SerializeField] private float pulseScale = 0.15f;

        [Header("Dots")]
        [Tooltip("リング上のドット数")]
        [SerializeField] private int dotCount = 12;

        [Tooltip("ドットのサイズ")]
        [SerializeField] private float dotSize = 0.06f;

        private Material ringMaterial;
        private GameObject dotGO;
        private float _elapsed;
        private bool _isActive = true;

        /// <summary>
        /// リングを有効化
        /// </summary>
        public void EnableRing()
        {
            _isActive = true;
            if (dotGO != null) dotGO.SetActive(true);
        }

        /// <summary>
        /// リングを無効化
        /// </summary>
        public void DisableRing()
        {
            _isActive = false;
            if (dotGO != null) dotGO.SetActive(false);
        }

        private void Start()
        {
            CreateRing();
            CreateDots();
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsed += Time.deltaTime;

            // 回転
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

            // パルス
            if (enablePulse && ringMaterial != null)
            {
                float pulse = 1f + Mathf.PingPong(_elapsed * pulseRate, 1f) * pulseScale;
                ringMaterial.SetFloat("_PulseScale", pulse);
            }
        }

        private void CreateRing()
        {
            // LineRendererで円形リングを作成
            var ringGO = new GameObject("SelectionRingLine");
            ringGO.transform.SetParent(transform);
            ringGO.transform.localPosition = Vector3.forward * 0.01f;
            ringGO.transform.localRotation = Quaternion.identity;

            var lineRenderer = ringGO.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 64;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.startColor = ringColor;
            lineRenderer.endColor = ringColor;
            lineRenderer.material = CreateRingMaterial();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            // boundingSphereOverride is not available on LineRenderer in this Unity version

            ringMaterial = lineRenderer.material;

            UpdateRingPositions();
        }

        private void UpdateRingPositions()
        {
            // LineRendererは後でUnityエディタで設定
        }

        private void CreateDots()
        {
            dotGO = new GameObject("SelectionDots");
            dotGO.transform.SetParent(transform);
            dotGO.transform.localPosition = Vector3.forward * 0.015f;

            for (int i = 0; i < dotCount; i++)
            {
                float angle = (i / (float)dotCount) * Mathf.PI * 2f;
                var dot = new GameObject($"Dot_{i}");
                dot.transform.SetParent(dotGO.transform);

                var sr = dot.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(dotSize);
                sr.color = ringColor;
                sr.sortingOrder = 10;

                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                dot.transform.localPosition = new Vector3(x, y, 0f);
            }
        }

        private Sprite CreateCircleSprite(float size)
        {
            var texture = new Texture2D(4, 4);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    float dx = x - 1.5f;
                    float dy = y - 1.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = dist < 1.5f ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private Material CreateRingMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;

            var mat = new Material(shader);
            mat.EnableKeyword("_PULSE_");
            mat.SetFloat("_PulseScale", 1f);
            return mat;
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0.1f, radius);
            lineWidth = Mathf.Max(0.01f, lineWidth);
            dotCount = Mathf.Max(3, Mathf.Min(36, dotCount));
        }
    }
}
