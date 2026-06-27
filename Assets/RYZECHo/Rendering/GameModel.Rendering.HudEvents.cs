#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER
namespace RYZECHo;

internal sealed partial class GameModel
{
    // ---------- HUD イベント購読 ----------

    private void SubscribeHudEvents()
    {
        GameEventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        GameEventBus.Subscribe<ActorDeathEvent>(OnActorDeath);
        GameEventBus.Subscribe<DamageEvent>(OnDamage);
        GameEventBus.Subscribe<AudioCueEvent>(OnAudioCue);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent evt)
    {
        _hudPhaseFlash = 0.9f;
        _hudPhaseLabel = evt.NewPhase.ToString();
        _hudPhaseLabelTimer = 2.4f;
    }

    private void OnActorDeath(ActorDeathEvent evt)
    {
        _hudKillFeed.Add(new HudKillEntry(
            evt.KillerName ?? "不明",
            evt.VictimName,
            evt.VictimType == ActorType.Enemy ? KillFeedSide.Attacker : KillFeedSide.Defender,
            evt.KillerName is null ? evt.VictimName : evt.KillerName));
        _hudKillFeedTimer = 4.2f;
    }

    private void OnDamage(DamageEvent evt)
    {
        if (evt.TargetName == _player.Name && evt.Damage > 12f)
        {
            _hudDamageFlashTimer = 0.6f;
            _hudDamageFlashColor = Color.FromArgb(255, 255, 132, 108);
        }
    }

    private void OnAudioCue(AudioCueEvent evt)
    {
        // オーディオキュー発生を外部システムへ通知
        AudioCueEmitted?.Invoke(evt.Kind, evt.Position, evt.Strength);
    }

    // ---------- HUD 状態フィールド ----------

    private float _hudPhaseFlash;
    private string _hudPhaseLabel = "HUNT";
    private float _hudPhaseLabelTimer = 3f;

    private readonly List<HudKillEntry> _hudKillFeed = new();
    private float _hudKillFeedTimer = 0f;

    private float _hudDamageFlashTimer;
    private Color _hudDamageFlashColor = Color.FromArgb(255, 255, 132, 108);

    // ---------- HudKillEntry 構造体 ----------

    private readonly record struct HudKillEntry(
        string KillerName,
        string VictimName,
        KillFeedSide Side,
        string SideLabel);

    private enum KillFeedSide
    {
        Attacker,
        Defender,
    }
}
#endif

