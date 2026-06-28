using UnityEngine;
using SysRandom = System.Random;

namespace RYZECHo.Runtime.Effects
{
    /// <summary>
    /// 爆発エフェクト。
    /// 爆発の視覚効果（パーティクル、音、衝撃波）を管理する。
    /// </summary>
    public class ExplosionEffect : MonoBehaviour
    {
        [Header("Explosion Settings")]
        [SerializeField] private float radius = 3f;
        [SerializeField] private float duration = 1f;
        [SerializeField] private Color explosionColor = new Color(1f, 0.6f, 0.1f, 1f);
        [SerializeField] private float smokeColorR = 0.3f;
        [SerializeField] private float smokeColorG = 0.3f;
        [SerializeField] private float smokeColorB = 0.3f;

        [Header("Shockwave")]
        [SerializeField] private bool enableShockwave = true;
        [SerializeField] private float shockwaveSpeed = 10f;
        [SerializeField] private float shockwaveWidth = 0.1f;

        [Header("Particles")]
        [SerializeField] private int particleCount = 20;
        [SerializeField] private float particleSpeed = 5f;
        [SerializeField] private float particleSize = 0.1f;

        [Header("Audio")]
        [SerializeField] private AudioClip explosionClip;
        [SerializeField] private float explosionVolume = 0.8f;
        [SerializeField] private float explosionPitch = 1f;

        private GameObject shockwaveRing;
        private GameObject[] particles;
        private float _elapsed;
        private bool _isComplete = false;
        private AudioSource _audioSource;

        /// <summary>
        /// 爆発エフェクトを発動
        /// </summary>
        public void Explode()
        {
            _elapsed = 0f;
            _isComplete = false;

            // 衝撃波リング
            if (enableShockwave)
            {
                CreateShockwave();
            }

            // パーティクル
            CreateExplosionParticles();

            // 音声
            PlayExplosionSound();
        }

        private void Update()
        {
            if (_isComplete) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / duration);

            // 衝撃波の拡大
            if (shockwaveRing != null && shockwaveRing.activeSelf)
            {
                float scale = 1f + t * (shockwaveSpeed / duration) * 2f;
                shockwaveRing.transform.localScale = new Vector3(scale, scale, 1f);

                var sr = shockwaveRing.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    var c = sr.color;
                    c.a = Mathf.Lerp(0.6f, 0f, t);
                    sr.color = c;
                }

                if (t >= 1f)
                {
                    shockwaveRing.SetActive(false);
                }
            }

            // パーティクルの更新
            UpdateParticles(t);

            if (t >= 1f)
            {
                _isComplete = true;
                Cleanup();
            }
        }

        private void CreateShockwave()
        {
            shockwaveRing = new GameObject("ShockwaveRing");
            shockwaveRing.transform.SetParent(transform);
            shockwaveRing.transform.localPosition = Vector3.forward * 0.01f;

            var sr = shockwaveRing.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRingSprite(radius);
            sr.color = new Color(smokeColorR, smokeColorG, smokeColorB, 0.6f);
            sr.sortingOrder = 100;
            shockwaveRing.SetActive(true);
        }

        private void CreateExplosionParticles()
        {
            particles = new GameObject[particleCount];

            for (int i = 0; i < particleCount; i++)
            {
                var angle = (i / (float)particleCount) * Mathf.PI * 2f;
                var speed = particleSpeed * (0.5f + UnityEngine.Random.value * 0.5f);

                var p = new GameObject($"ExplosionParticle_{i}");
                p.SetActive(false);
                p.transform.SetParent(transform);

                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(particleSize);

                // 炎の色をランダム化
                float r = explosionColor.r * (0.8f + UnityEngine.Random.value * 0.2f);
                float g = explosionColor.g * (0.5f + UnityEngine.Random.value * 0.5f);
                float b = explosionColor.b * (0.2f + UnityEngine.Random.value * 0.3f);
                sr.color = new Color(r, g, b, 1f);
                sr.sortingOrder = 50;

                particles[i] = p;

                // 物理演算風のスローをコルーチンで
                StartCoroutine(SpawnParticle(p, angle, speed));
            }
        }

        private System.Collections.IEnumerator SpawnParticle(GameObject p, float angle, float speed)
        {
            float t = 0f;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            var startPos = transform.position;

            while (t < duration)
            {
                t += Time.deltaTime;
                float gravity = -5f * Time.deltaTime;
                dir.y += gravity;

                p.SetActive(true);
                p.transform.position = startPos + (Vector3)(dir * t);

                var sr = p.GetComponent<SpriteRenderer>();
                var c = sr.color;
                c.a = 1f - t / duration;
                sr.color = c;

                float s = Mathf.Lerp(1f, 0f, t / duration);
                p.transform.localScale = new Vector3(s, s, s);

                yield return null;
            }
            p.SetActive(false);
        }

        private void UpdateParticles(float t)
        {
            if (particles == null) return;
            foreach (var p in particles)
            {
                if (p != null && p.activeSelf)
                {
                    var sr = p.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        var c = sr.color;
                        if (c.a <= 0f) p.SetActive(false);
                    }
                }
            }
        }

        private void PlayExplosionSound()
        {
            if (explosionClip == null) return;

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            _audioSource.clip = explosionClip;
            _audioSource.volume = explosionVolume;
            _audioSource.pitch = explosionPitch;
            _audioSource.spatialBlend = 0.5f;
            _audioSource.PlayOneShot(explosionClip, explosionVolume);
        }

        private void Cleanup()
        {
            if (shockwaveRing != null)
            {
                Destroy(shockwaveRing);
                shockwaveRing = null;
            }

            if (particles != null)
            {
                foreach (var p in particles)
                {
                    if (p != null) Destroy(p);
                }
                particles = null;
            }

            if (_audioSource != null)
            {
                Destroy(_audioSource);
                _audioSource = null;
            }

            Destroy(gameObject);
        }

        private Sprite CreateRingSprite(float outerRadius)
        {
            int res = 64;
            var texture = new Texture2D(res, 1);

            for (int i = 0; i < res; i++)
            {
                float t = i / (float)res;
                float angle = t * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * outerRadius;
                float y = Mathf.Sin(angle) * outerRadius;

                // UV座標として使用
                texture.SetPixel(i, 0, new Color(x * 0.5f + 0.5f, y * 0.5f + 0.5f, 0f, 1f));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, res, 1), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateCircleSprite(float size)
        {
            int res = 8;
            var texture = new Texture2D(res, res);

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx = x - (res - 1) / 2f;
                    float dy = y - (res - 1) / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = dist < res / 2.5f ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 1f / size);
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0.1f, radius);
            duration = Mathf.Max(0.1f, duration);
            particleCount = Mathf.Max(1, Mathf.Min(100, particleCount));
        }
    }
}
