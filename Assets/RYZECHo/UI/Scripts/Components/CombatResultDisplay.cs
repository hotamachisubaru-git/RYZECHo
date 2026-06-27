using UnityEngine;
using Color = UnityEngine.Color;
using TMPro;
using UnityEngine.UI;

namespace RYZECHo.UI
{
    /// <summary>
    /// 戦闘結果テキスト（ダメージ、クリティカル、キル通知等）の表示を管理。
    /// フェードイン/アウトアニメーション付き。
    /// </summary>
    public class CombatResultDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private Image _background;

        [Header("Settings")]
        [Tooltip("表示時間（秒）")]
        [SerializeField] private float _displayDuration = 2.0f;

        [Tooltip("文字サイズ")]
        [SerializeField] private float _fontSize = 24f;

        [Tooltip("ダメージ時の色")]
        [SerializeField] private Color _damageColor = new Color(1f, 0.3f, 0.25f, 1f);

        [Tooltip("クリティカル時の色")]
        [SerializeField] private Color _criticalColor = new Color(1f, 0.85f, 0.1f, 1f);

        [Tooltip("キル時の色")]
        [SerializeField] private Color _killColor = new Color(0.4f, 0.9f, 0.7f, 1f);

        [Tooltip("味方時色")]
        [SerializeField] private Color _allyColor = new Color(0.35f, 0.85f, 0.65f, 1f);

        [Tooltip("敵時色")]
        [SerializeField] private Color _enemyColor = new Color(0.9f, 0.35f, 0.3f, 1f);

        private float _timer = 0f;
        private float _fadeInTime = 0.15f;
        private CombatResultType _type = CombatResultType.Normal;

        public enum CombatResultType
        {
            Normal,
            Damage,
            Critical,
            Kill,
            ShieldHit,
            Heal,
            PhaseChange,
        }

        /// <summary>
        /// 戦闘結果を表示。
        /// </summary>
        public void Show(string message, CombatResultType type = CombatResultType.Normal)
        {
            if (_text == null) return;

            _type = type;
            _text.text = message;
            _timer = _displayDuration;

            // タイプに応じた色設定
            switch (type)
            {
                case CombatResultType.Damage:
                    _text.color = _damageColor;
                    break;
                case CombatResultType.Critical:
                    _text.color = _criticalColor;
                    break;
                case CombatResultType.Kill:
                    _text.color = _killColor;
                    break;
                case CombatResultType.ShieldHit:
                    _text.color = new Color(0.3f, 0.6f, 1f, 1f);
                    break;
                case CombatResultType.Heal:
                    _text.color = new Color(0.3f, 0.9f, 0.5f, 1f);
                    break;
                case CombatResultType.PhaseChange:
                    _text.color = new Color(0.9f, 0.85f, 0.5f, 1f);
                    break;
                default:
                    _text.color = new Color(1f, 1f, 1f, 1f);
                    break;
            }

            // サイズ調整（クリティカルは大文字）
            _text.fontSize = type == CombatResultType.Critical ? _fontSize * 1.3f : _fontSize;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// ダメージ結果を表示。
        /// </summary>
        public void ShowDamage(float damage, bool isCritical, string targetName)
        {
            string text = isCritical
                ? $"クリティカル！{targetName} に {Mathf.CeilToInt(damage)} ダメージ！"
                : $"{targetName} に {Mathf.CeilToInt(damage)} ダメージ";

            Show(text, isCritical ? CombatResultType.Critical : CombatResultType.Damage);
        }

        /// <summary>
        /// キル通知を表示。
        /// </summary>
        public void ShowKill(string killerName, string victimName, bool isEnemyVictim)
        {
            string text = isEnemyVictim
                ? $"{killerName} が {victimName} を撃破！"
                : $"{killerName} が撃破されました";

            Show(text, CombatResultType.Kill);
        }

        /// <summary>
        /// シールドヒットを表示。
        /// </summary>
        public void ShowShieldHit(float absorbed)
        {
            Show($"シールドで {Mathf.CeilToInt(absorbed)} ダメージを吸収", CombatResultType.ShieldHit);
        }

        private void Update()
        {
            if (_text == null || !gameObject.activeSelf) return;

            _timer -= Time.deltaTime;

            // フェードアウト
            float fadeRatio = Mathf.Clamp01(_timer / _fadeInTime);
            float alpha = _timer > _displayDuration - _fadeInTime
                ? Mathf.Clamp01((_displayDuration - _timer) / _fadeInTime)
                : 1f;

            _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, alpha * 255f / 255f);

            if (_timer <= 0f)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
