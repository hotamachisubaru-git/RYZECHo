using UnityEngine;
using Color = UnityEngine.Color;
using TMPro;
using UnityEngine.UI;

namespace RYZECHo.UI
{
    /// <summary>
    /// 敵/味方ユニットの個別表示コンポーネント。
    /// ユニット名、HPバー、シールドバー、ステータスアイコンを表示。
    /// </summary>
    public class EnemyUnitDisplay : MonoBehaviour
    {
        [Header("Display Elements")]
        [Tooltip("ユニット名テキスト")]
        public TextMeshProUGUI nameText;

        [Tooltip("HPバーの背景")]
        public Image hpBarBackground;

        [Tooltip("HPバーの充填")]
        public Image hpBarFill;

        [Tooltip("HPテキスト")]
        public TextMeshProUGUI hpText;

        [Tooltip("シールドバーの充填")]
        public Image shieldBarFill;

        [Tooltip("ステータスアイコンの親")]
        public Transform statusIconParent;

        [Header("Visual Settings")]
        [Tooltip("敵ユニットの色")]
        public Color enemyColor = new Color(0.9f, 0.35f, 0.3f);

        [Tooltip("味方ユニットの色")]
        public Color allyColor = new Color(0.35f, 0.85f, 0.65f);

        [Tooltip("HPバーの最小幅")]
        public float minBarWidth = 60f;

        [Tooltip("HPバーの最大幅")]
        public float maxBarWidth = 120f;

        private string _actorName = "";
        private bool _isEnemy = true;
        private float _currentHealth = 0f;
        private float _maxHealth = 100f;
        private float _currentShield = 0f;
        private float _maxShield = 0f;
        private float _flashTimer = 0f;
        private Color _flashColor = Color.clear;

        /// <summary>
        /// ユニットを初期化。
        /// </summary>
        public void Initialize(Actor actor, bool isEnemy)
        {
            _actorName = actor.Name;
            _isEnemy = isEnemy;
            _currentHealth = actor.Health;
            _maxHealth = actor.MaxHealth;
            _currentShield = actor.Shield;
            _maxShield = actor.MaxShield;

            // 名前設定
            if (nameText != null)
            {
                nameText.text = actor.Name;
                nameText.color = isEnemy ? enemyColor : allyColor;
            }

            // HPバー初期化
            if (hpBarFill != null)
            {
                hpBarFill.fillAmount = _maxHealth > 0f ? _currentHealth / _maxHealth : 0f;
                hpBarFill.color = GetHealthColor(_currentHealth, _maxHealth);
            }

            // HPテキスト初期化
            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt(_currentHealth)} / {Mathf.CeilToInt(_maxHealth)}";
            }

            // シールドバー初期化
            if (shieldBarFill != null)
            {
                shieldBarFill.fillAmount = _maxShield > 0f ? _currentShield / _maxShield : 0f;
                shieldBarFill.gameObject.SetActive(_currentShield > 0.01f);
            }

            // ボスフラグに応じた表示調整
            if (actor.IsBoss && nameText != null)
            {
                nameText.fontStyle = FontStyles.Bold;
                nameText.color = new Color(1f, 0.85f, 0.3f);
            }
        }

        /// <summary>
        /// アクターの最新状態を更新。
        /// </summary>
        public void UpdateActor(Actor actor, bool isEnemy)
        {
            // 死亡状態の変化
            bool wasAlive = _currentHealth > 0.01f;
            bool isAlive = actor.IsAlive;

            if (!isAlive)
            {
                // 死亡時はHPバーを暗く
                if (hpBarFill != null)
                {
                    hpBarFill.fillAmount = 0f;
                    hpBarFill.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
                if (hpText != null)
                {
                    hpText.text = "DEAD";
                    hpText.color = new Color(0.5f, 0.5f, 0.5f);
                }
                return;
            }

            // HPの更新
            float healthDiff = actor.Health - _currentHealth;
            _currentHealth = actor.Health;
            _maxHealth = actor.MaxHealth;
            _currentShield = actor.Shield;
            _maxShield = actor.MaxShield;

            // HPバー更新
            if (hpBarFill != null)
            {
                float ratio = _maxHealth > 0f ? _currentHealth / _maxHealth : 0f;
                hpBarFill.fillAmount = Mathf.Clamp01(ratio);
                hpBarFill.color = GetHealthColor(_currentHealth, _maxHealth);

                // ダメージフラッシュ
                if (healthDiff < -5f && _flashTimer <= 0f)
                {
                    _flashColor = new Color(1f, 0.3f, 0.3f, 0.6f);
                    _flashTimer = 0.3f;
                }
            }

            // HPテキスト更新
            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt(_currentHealth)} / {Mathf.CeilToInt(_maxHealth)}";
            }

            // シールドバー更新
            if (shieldBarFill != null)
            {
                float shieldRatio = _maxShield > 0f ? _currentShield / _maxShield : 0f;
                shieldBarFill.fillAmount = Mathf.Clamp01(shieldRatio);
                shieldBarFill.gameObject.SetActive(_currentShield > 0.01f);
            }

            // ステータスエフェクトの更新
            UpdateStatusEffects(actor);
        }

        /// <summary>
        /// HPの色を状態に応じて返す。
        /// </summary>
        private Color GetHealthColor(float health, float maxHealth)
        {
            float ratio = maxHealth > 0f ? health / maxHealth : 0f;

            // フラッシュが有効なら白フラッシュ
            if (_flashTimer > 0f)
            {
                return _flashColor;
            }

            if (ratio > 0.6f)
            {
                return _isEnemy
                    ? new Color(0.85f, 0.3f, 0.25f)
                    : new Color(0.3f, 0.85f, 0.6f);
            }
            else if (ratio > 0.3f)
            {
                return new Color(0.9f, 0.7f, 0.1f);
            }
            else
            {
                return new Color(0.9f, 0.2f, 0.15f);
            }
        }

        /// <summary>
        /// ステータスエフェクトアイコンを更新。
        /// </summary>
        private void UpdateStatusEffects(Actor actor)
        {
            if (statusIconParent == null) return;

            // 既存のアイコンを整理
            foreach (Transform child in statusIconParent)
            {
                Destroy(child.gameObject);
            }

            // ダメージエフェクトの表示（簡易）
            // TODO: 実際のステータスエフェクトに応じてアイコンを追加
        }

        private void Update()
        {
            // フラッシュタイマーの更新
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && hpBarFill != null)
                {
                    hpBarFill.color = GetHealthColor(_currentHealth, _maxHealth);
                }
            }
        }
    }
}
