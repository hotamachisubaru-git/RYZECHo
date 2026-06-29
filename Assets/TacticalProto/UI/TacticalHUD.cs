using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RYZECHo.TacticalProto.UI
{
    /// <summary>繝輔ぉ繝ｼ繧ｺ蛻･HUD・・P/Sield/蠑ｾ謨ｰ/繧ｹ繧ｳ繧｢/繝輔ぉ繝ｼ繧ｺ/繝ｩ繧ｦ繝ｳ繝峨ち繧､繝槭・・・/summary>
    public class TacticalHUD : MonoBehaviour
    {
        [Header("HP繝舌・")]
        public TMP_Text hpText;
        public UnityEngine.UI.Image hpBarFill;
        public UnityEngine.UI.Image hpBarBg;
        [Header("繧ｷ繝ｼ繝ｫ繝峨ヰ繝ｼ")]
        public TMP_Text shieldText;
        public UnityEngine.UI.Image shieldBarFill;
        public UnityEngine.UI.Image shieldBarBg;
        [Header("蠑ｾ謨ｰ")]
        public TMP_Text ammoText;
        [Header("繧ｹ繧ｳ繧｢")]
        public TMP_Text scoreText;
        [Header("繝輔ぉ繝ｼ繧ｺ陦ｨ遉ｺ")]
        public TMP_Text phaseText;
        [Header("繝ｩ繧ｦ繝ｳ繝峨ち繧､繝槭・")]
        public TMP_Text roundTimerText;
        [Header("繝ｩ繧ｦ繝ｳ繝峨せ繧ｳ繧｢")]
        public TMP_Text roundScoreText;

        private float currentHp, maxHp, currentShield, maxShield;
        private int currentAmmo, maxAmmo, score, turn;
        private GamePhase phase;
        private float roundTimer;
        private TacticalGameModel _model;

        private static TacticalHUD _instance;
        public static TacticalHUD Instance => _instance;

        public void SetModel(TacticalGameModel model) => _model = model;

        private void Awake() => _instance = this;

        public void Sync(TacticalGameModel model)
        {
            phase = model.CurrentPhase;
            roundTimer = model.RoundTimer;
            var player = model.Player;

            if (player != null)
            {
                currentHp = player.Health; maxHp = player.MaxHealth;
                currentShield = player.Shield; maxShield = player.MaxShield;
            }
            score = model.PlayerRoundWins; turn = model.CurrentRound;

            UpdateAll();
        }

        private void UpdateAll()
        {
            UpdateHpBar();
            UpdateShieldBar();
            UpdateAmmoDisplay();
            UpdateScoreDisplay();
            UpdatePhaseDisplay();
            UpdateTimerDisplay();
            UpdateRoundScoreDisplay();
            UpdatePhaseVisibility();
        }

        // --- HP ---
        private void UpdateHpBar()
        {
            if (hpBarFill != null) hpBarFill.fillAmount = maxHp > 0 ? currentHp / maxHp : 0f;
            if (hpText != null) hpText.text = $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
            if (hpBarBg != null) hpBarBg.enabled = phase == GamePhase.Hunt;
            if (hpBarFill != null) hpBarFill.enabled = phase == GamePhase.Hunt;
            if (hpText != null) hpText.enabled = phase == GamePhase.Hunt;
        }

        // --- Shield ---
        private void UpdateShieldBar()
        {
            if (shieldBarFill != null) shieldBarFill.fillAmount = maxShield > 0 ? currentShield / maxShield : 0f;
            if (shieldText != null) shieldText.text = $"SH: {Mathf.CeilToInt(currentShield)}/{Mathf.CeilToInt(maxShield)}";
            if (shieldBarBg != null) shieldBarBg.enabled = phase == GamePhase.Hunt;
            if (shieldBarFill != null) shieldBarFill.enabled = phase == GamePhase.Hunt;
            if (shieldText != null) shieldText.enabled = phase == GamePhase.Hunt;
        }

        // --- Ammo ---
        public void SetAmmo(int cur, int max) { currentAmmo = cur; maxAmmo = max; }
        private void UpdateAmmoDisplay()
        {
            if (ammoText != null)
            {
                ammoText.text = $"{currentAmmo}/{maxAmmo}";
                ammoText.enabled = phase == GamePhase.Hunt;
            }
        }

        // --- Score ---
        private void UpdateScoreDisplay()
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {score:N}";
                scoreText.enabled = phase == GamePhase.Hunt;
            }
        }

        // --- Phase ---
        private void UpdatePhaseDisplay()
        {
            if (phaseText != null)
            {
                phaseText.text = phase switch
                {
                    GamePhase.Construct  => "CONSTRUCT PHASE",
                    GamePhase.Bet        => "BET PHASE",
                    GamePhase.Hunt       => "HUNT PHASE",
                    GamePhase.RoundResult => "ROUND RESULT",
                    GamePhase.Victory    => "VICTORY",
                    GamePhase.Defeat     => "DEFEAT",
                    _                    => "UNKNOWN",
                };
                phaseText.enabled = true;
            }
        }

        // --- Timer ---
        private void UpdateTimerDisplay()
        {
            if (roundTimerText != null)
            {
                float remaining = phase == GamePhase.Hunt ? roundTimer :
                        phase == GamePhase.Construct ? 60f :
                        phase == GamePhase.Bet ? 20f : 0f;
                roundTimerText.text = $"TIME: {Mathf.Max(0f, remaining):F1}";
                roundTimerText.enabled = phase == GamePhase.Hunt || phase == GamePhase.Construct || phase == GamePhase.Bet;
            }
        }

        // --- Round Score ---
        private void UpdateRoundScoreDisplay()
        {
            if (roundScoreText != null)
            {
                roundScoreText.text = $"R{turn}{(_model?.PlayerRoundWins ?? 0)}-{(_model?.EnemyRoundWins ?? 0)}";
                roundScoreText.enabled = phase == GamePhase.RoundResult || phase == GamePhase.Victory || phase == GamePhase.Defeat;
            }
        }

        // --- Phase Visibility ---
        private void UpdatePhaseVisibility()
        {
            var hunt = phase == GamePhase.Hunt;
            var result = phase == GamePhase.RoundResult || phase == GamePhase.Victory || phase == GamePhase.Defeat;
            if (hpBarBg != null) hpBarBg.gameObject.SetActive(hunt);
            if (shieldBarBg != null) shieldBarBg.gameObject.SetActive(hunt);
            if (ammoText != null) ammoText.gameObject.SetActive(hunt);
            if (scoreText != null) scoreText.gameObject.SetActive(hunt);
            if (roundScoreText != null) roundScoreText.gameObject.SetActive(result);
        }
    }
}


