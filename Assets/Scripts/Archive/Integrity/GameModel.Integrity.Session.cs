using System.Diagnostics;

namespace RYZECHo;

internal sealed partial class GameModel
{
    private const string ProgressIntegrityVersion = "RYZECHo.Profile.v2";
    private const string IntegrityViolationLogFileName = "integrity-violation.log";
    private const float IntegrityDeltaSkewToleranceSeconds = 0.085f;
    private const float IntegrityTrackedDeltaCapSeconds = 0.25f;
    private const float IntegrityMovementSlackPixels = 18f;
    private const int IntegrityStrikeLockThreshold = 4;
    private const int IntegrityHardCreditsCap = 2_000_000;
    private const int IntegrityMaxRipples = 96;
    private const int IntegrityMaxActivityFeedEntries = 5;
    private const int IntegrityMaxAccountLevel = 99;
    private const int IntegrityMaxCareerStat = 20_000;

    private readonly Stopwatch _integrityClock = Stopwatch.StartNew();
    private readonly Dictionary<Actor, IntegrityActorSnapshot> _integrityActorSnapshots = new();

    private TimeSpan _integrityLastTick;
    private GamePhase _integrityPhaseSnapshot;
    private int _integrityCreditsSnapshot;
    private int _integrityGraceFrames;
    private int _integrityStrikeCount;
    private float _integrityFeedCooldown;
    private bool _integrityRewardsLocked;
    private string _integrityStatusLine = string.Empty;

    private void PrepareIntegrityFrame(float deltaSeconds)
    {
        var clampedDelta = Math.Clamp(deltaSeconds, 0.001f, IntegrityTrackedDeltaCapSeconds);
        _integrityFeedCooldown = MathF.Max(0f, _integrityFeedCooldown - clampedDelta);

        var now = _integrityClock.Elapsed;
        if (_integrityLastTick != TimeSpan.Zero)
        {
            var measuredDelta = Math.Clamp((float)(now - _integrityLastTick).TotalSeconds, 0.001f, IntegrityTrackedDeltaCapSeconds);
            if (MathF.Abs(measuredDelta - clampedDelta) > IntegrityDeltaSkewToleranceSeconds && measuredDelta < 0.2f)
            {
                RegisterIntegrityAnomaly("時間整合性", $"更新間隔の不整合を検知。入力 {clampedDelta:0.000}s / 実測 {measuredDelta:0.000}s。");
            }
        }

        _integrityLastTick = now;
        _integrityPhaseSnapshot = _phase;
        _integrityCreditsSnapshot = _credits;
        CaptureIntegrityActorSnapshots();
    }

    private void FinalizeIntegrityFrame(float deltaSeconds)
    {
        SanitizeGlobalRuntimeState();
        SanitizeStructures();
        SanitizeActors(deltaSeconds);

        if (_integrityGraceFrames > 0)
        {
            _integrityGraceFrames--;
        }
        else
        {
            ValidateCreditsTransition();
            ValidateActorTravel(deltaSeconds);
        }

        CaptureIntegrityActorSnapshots();
    }

    private void ResetIntegritySession()
    {
        _integrityStrikeCount = 0;
        _integrityRewardsLocked = false;
        _integrityStatusLine = string.Empty;
        _integrityLastTick = TimeSpan.Zero;
        ArmIntegrityGrace(3);
        CaptureIntegrityActorSnapshots();
    }

    private void ArmIntegrityGrace(int frames = 2)
    {
        _integrityGraceFrames = Math.Max(_integrityGraceFrames, frames);
    }

    private void CaptureIntegrityActorSnapshots()
    {
        _integrityActorSnapshots.Clear();
        CaptureIntegrityActorSnapshot(_player);

        foreach (var ally in _allies)
        {
            CaptureIntegrityActorSnapshot(ally);
        }

        foreach (var enemy in _enemies)
        {
            CaptureIntegrityActorSnapshot(enemy);
        }
    }

    private void CaptureIntegrityActorSnapshot(Actor actor)
    {
        _integrityActorSnapshots[actor] = new IntegrityActorSnapshot(
            actor.Position,
            actor.Health,
            actor.Shield,
            actor.IsAlive,
            SanitizeWeaponType(actor.Weapon, DefaultWeaponFor(actor)));
    }

}
