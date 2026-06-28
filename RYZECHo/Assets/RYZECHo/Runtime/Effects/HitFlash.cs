using UnityEngine;

namespace RYZECHo.Runtime.Effects
{
    /// <summary>
    /// ヒット時のフラッシュエフェクト。
    /// ヒットした瞬間に白フラッシュや画面全体のフラッシュを表示する。
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        [Header("Flash Settings")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private float intensity = 1f;

        [Header("Screen Flash")]
        [SerializeField] private bool enableScreenFlash = true;
        [SerializeField] private float screenFlashDuration = 0.2f;
        [SerializeField] private float screenFlashIntensity = 0.3f;

        [Header("Hit Particle")]
        [SerializeField] private bool enableHitParticles = true;
        [SerializeField] private int hitParticleCount = 8;
        [SerializeField] private float hitParticleSpeed = 3f;
        [SerializeField] private float hitParticleSize = 0.05f;

        private GameObject flashQuad;
        private GameObject[] hitParticles;
        private float _elapsed;
        private bool _isActive = false;

        /// <summary>
        /// ヒットフラッシュを発動
        /// </summary>
        public void Trigger(Vector3 hitPosition, Color? customColor = null)
        {
            _isActive = true;
            _elapsed = 0f;

            var color = customColor ?? flashColor;

            // フラッシュクアド
            if (flashQuad == null)
            {
                flashQuad = new GameObject("HitFlashQuad");
                var sr = flashQuad.AddComponent<SpriteRenderer>();
                sr.sprite = CreateQuadSprite();
                sr.color = color;
                sr.sortingOrder = 9999;
                flashQuad.SetActive(false);
            }

            flashQuad.SetActive(true);
            flashQuad.transform.position = hitPosition;
            flashQuad.transform.localScale = Vector3.one * 2f * intensity;
            var fc = flashQuad.GetComponent<SpriteRenderer>().color;
            fc = color;
            fc.a = intensity;
            flashQuad.GetComponent<SpriteRenderer>().color = fc;

            // ヒットパーティクル
            if (enableHitParticles)
            {
                SpawnHitParticles(hitPosition, color);
            }

            // スクリーンフラッシュ
            if (enableScreenFlash)
            {
                TriggerScreenFlash(color);
            }
        }

        private void Update()
        {
            if (!_isActive || flashQuad == null) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / duration);

            // フラッシュフェード
            var sr = flashQuad.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var c = sr.color;
                c.a = Mathf.Lerp(intensity, 0f, t);
                sr.color = c;

                float scale = Mathf.Lerp(2f * intensity, 3f * intensity, t);
                flashQuad.transform.localScale = new Vector3(scale, scale, scale);
            }

            if (t >= 1f)
            {
                flashQuad.SetActive(false);
                _isActive = false;
            }
        }

        private void SpawnHitParticles(Vector3 position, Color color)
        {
            if (hitParticles == null)
            {
                hitParticles = new GameObject[hitParticleCount];
                for (int i = 0; i < hitParticleCount; i++)
                {
                    var go = new GameObject($"HitParticle_{i}");
                    go.SetActive(false);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateCircleSprite(hitParticleSize);
                    hitParticles[i] = go;
                }
            }

            for (int i = 0; i < hitParticleCount; i++)
            {
                var angle = (i / (float)hitParticleCount) * Mathf.PI * 2f;
                var p = hitParticles[i];
                p.SetActive(true);
                p.transform.position = position;
                var sr = p.GetComponent<SpriteRenderer>();
                sr.color = color;

                // 物理演算風のスロー
                StartCoroutine(ParticleFly(p, angle, hitParticleSpeed, duration));
            }
        }

        private System.Collections.IEnumerator ParticleFly(GameObject p, float angle, float speed, float dur)
        {
            float t = 0f;
            var startPos = p.transform.position;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

            while (t < dur)
            {
                t += Time.deltaTime;
                p.transform.position = startPos + (Vector3)(dir * t);
                var sr = p.GetComponent<SpriteRenderer>();
                var c = sr.color;
                c.a = 1f - t / dur;
                sr.color = c;
                yield return null;
            }
            p.SetActive(false);
        }

        private void TriggerScreenFlash(Color color)
        {
            // シーン内の全SpriteRendererを一時的にフラッシュ
            // 簡易実装: Canvas内のImageをフラッシュ
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                var images = canvas.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var img in images)
                {
                    if (img.name.Contains("Flash") || img.name.Contains("Overlay"))
                    {
                        StartCoroutine(ScreenFlashImage(img, color, screenFlashDuration, screenFlashIntensity));
                        break;
                    }
                }
            }
        }

        private System.Collections.IEnumerator ScreenFlashImage(UnityEngine.UI.Image img, Color color, float dur, float intensity)
        {
            float t = 0f;
            var origColor = img.color;
            img.color = new Color(color.r, color.g, color.b, intensity);

            while (t < dur)
            {
                t += Time.deltaTime;
                float fade = 1f - t / dur;
                img.color = new Color(color.r, color.g, color.b, intensity * fade);
                yield return null;
            }
            img.color = origColor;
        }

        private Sprite CreateQuadSprite()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixel(0, 0, Color.white);
            tex.SetPixel(1, 0, Color.white);
            tex.SetPixel(0, 1, Color.white);
            tex.SetPixel(1, 1, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateCircleSprite(float size)
        {
            var tex = new Texture2D(4, 4);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    float dx = x - 1.5f;
                    float dy = y - 1.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = dist < 1.5f ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private void OnDestroy()
        {
            if (flashQuad != null && flashQuad.gameObject != null)
                Destroy(flashQuad.gameObject);
            if (hitParticles != null)
            {
                foreach (var p in hitParticles)
                {
                    if (p != null && p.gameObject != null)
                        Destroy(p.gameObject);
                }
            }
        }
    }
}
