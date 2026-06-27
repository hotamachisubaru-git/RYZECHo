using UnityEngine;
using Color = UnityEngine.Color;
using TMPro;
using UnityEngine.UI;

namespace RYZECHo.UI
{
    /// <summary>
    /// HPバーの表示を管理するコンポーネント。
    /// 背景、充填、シールドバー、テキストを統合制御。
    /// </summary>
    public class HealthBarComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _shield;
        [SerializeField] private TextMeshProUGUI _text;

        [Header("Settings")]
        [SerializeField] private float _damageFlashDuration = 0.3f;
        [SerializeField] private Color _damageFlashColor = new Color(1f, 0.3f, 0.3f, 0.7f);

        private float _flashTimer = 0f;
        private float _lastHealth = 0f;
        private float _maxHealth = 100f;

        public void SetValues(float currentHealth, float maxHealth, float currentShield, float maxShield, string customText = null)
        {
            _maxHealth = maxHealth > 0f ? maxHealth : 100f;
            _lastHealth = currentHealth;

            // 充填率
            float ratio = Mathf.Clamp01(currentHealth / _maxHealth);
            if (_fill != null)
            {
                _fill.fillAmount = ratio;
                _fill.color = GetHealthColor(ratio);
            }

            // シールドバー
            if (_shield != null)
            {
                float shieldRatio = maxShield > 0f ? currentShield / maxShield : 0f;
                _shield.fillAmount = shieldRatio;
                _shield.gameObject.SetActive(currentShield > 0.01f);
            }

            // テキスト
            if (_text != null)
            {
                _text.text = customText ?? $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(_maxHealth)}";
            }

            // ダメージフラッシュ
            if (currentHealth < _lastHealth - 5f && _flashTimer <= 0f)
            {
                _flashTimer = _damageFlashDuration;
            }
        }

        private void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && _fill != null)
                {
                    float ratio = _maxHealth > 0f ? _lastHealth / _maxHealth : 0f;
                    _fill.color = GetHealthColor(ratio);
                }
            }
        }

        private Color GetHealthColor(float ratio)
        {
            if (_flashTimer > 0f) return _damageFlashColor;

            if (ratio > 0.6f) return new Color(0.3f, 0.86f, 0.65f);
            if (ratio > 0.3f) return new Color(0.9f, 0.7f, 0.1f);
            return new Color(0.9f, 0.25f, 0.15f);
        }
    }
}
