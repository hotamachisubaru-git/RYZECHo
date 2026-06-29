using System.Text;

namespace RYZECHo;

public sealed partial class GameModel
{
    private void ValidateCreditsTransition()
    {
        var delta = _credits - _integrityCreditsSnapshot;
        var allowedGain = MaximumLegitimateCreditGain(_integrityCreditsSnapshot);
        var allowedSpend = MaximumLegitimateCreditSpend(_integrityCreditsSnapshot);

        if (delta <= allowedGain && delta >= -allowedSpend)
        {
            return;
        }

        RegisterIntegrityStrike("所持金整合性", $"不正なクレジット変動 {delta:+#;-#;0}c を巻き戻しました。", severe: true);
        _credits = _integrityCreditsSnapshot;
    }

    private int MaximumLegitimateCreditGain(int baselineCredits)
    {
        var effectiveBet = Math.Clamp(_selectedBet, 0, Math.Max(baselineCredits, 6000));
        var maximumKillSwing = (TeamSize * KillRewardCredits) + (TeamSize * TeamSize * BossKillDividendCredits) + BossEliminationBonusCredits;
        var objectiveSwing = ObjectiveRewardCredits * 2;
        var roundResultSwing = WinRewardCredits + (effectiveBet * BossPayoutMultiplier);

        return maximumKillSwing + objectiveSwing + roundResultSwing + 600;
    }

    private int MaximumLegitimateCreditSpend(int baselineCredits)
    {
        return _integrityPhaseSnapshot switch
        {
            GamePhase.Bet when _phase == GamePhase.Hunt => baselineCredits,
            GamePhase.Victory or GamePhase.Defeat when _phase == GamePhase.Construct => baselineCredits,
            _ => 0,
        };
    }

    private void ValidateActorTravel(float deltaSeconds)
    {
        ValidateActorTravel(_player, deltaSeconds);

        foreach (var ally in _allies)
        {
            ValidateActorTravel(ally, deltaSeconds);
        }

        foreach (var enemy in _enemies)
        {
            ValidateActorTravel(enemy, deltaSeconds);
        }
    }

    private void ValidateActorTravel(Actor actor, float deltaSeconds)
    {
        if (!_integrityActorSnapshots.TryGetValue(actor, out var snapshot))
        {
            return;
        }

        if (!snapshot.WasAlive || !actor.IsAlive || _integrityPhaseSnapshot != _phase)
        {
            return;
        }

        var actualTravel = Distance(actor.Position, snapshot.Position);
        var allowedTravel = MaximumLegitimateTravel(actor, snapshot.Weapon, deltaSeconds);
        if (actualTravel <= allowedTravel)
        {
            return;
        }

        actor.Position = ResolveCollision(snapshot.Position, actor.Radius);
        RegisterIntegrityStrike("移動整合性", $"{actor.Name} の移動量 {actualTravel:0.0}px を巻き戻しました。", severe: actualTravel > allowedTravel * 2.2f);
    }

    private float MaximumLegitimateTravel(Actor actor, WeaponType previousWeapon, float deltaSeconds)
    {
        var effectiveWeapon = _weaponStats[SanitizeWeaponType(previousWeapon, DefaultWeaponFor(actor))];
        var baseSpeed = Math.Max(actor.BaseMoveSpeed, effectiveWeapon.MoveSpeed) * Math.Max(1f, GetActorMoveSpeedMultiplier(actor));
        return (baseSpeed * Math.Max(deltaSeconds, 0.001f) * 1.45f) + actor.Radius + IntegrityMovementSlackPixels;
    }

    private WeaponType SanitizeWeaponType(WeaponType weaponType, WeaponType fallback)
    {
        return _weaponStats.ContainsKey(weaponType) ? weaponType : fallback;
    }

    private static AgentKind SanitizeAgentKind(AgentKind agent)
    {
        return Enum.IsDefined(typeof(AgentKind), agent) ? agent : AgentKind.Veil;
    }

    private WeaponType DefaultWeaponFor(Actor actor)
    {
        return actor.Type switch
        {
            ActorType.Player => RosterCatalog.DefaultFriendlyWeaponFor(actor.Name),
            ActorType.Ally => RosterCatalog.DefaultFriendlyWeaponFor(actor.Name),
            ActorType.Enemy => WeaponType.Blitz,
            _ => WeaponType.Giant,
        };
    }

    private void RegisterIntegrityStrike(string category, string message, bool severe = false)
    {
        _integrityStrikeCount++;
        _integrityStatusLine = $"{category}: {message}";
        _integrityRewardsLocked = true;

        if (severe || _integrityStrikeCount >= IntegrityStrikeLockThreshold)
        {
            ForceTerminateForIntegrityViolation(category, message);
            return;
        }

        RegisterIntegrityAnomaly(category, message);
    }

    private void RegisterIntegrityAnomaly(string category, string message)
    {
        _integrityStatusLine = $"{category}: {message}";

        if (_integrityFeedCooldown > 0f && _activityFeed.Count > 0 && _activityFeed[0] == $"[AC] {_integrityStatusLine}")
        {
            return;
        }

        _integrityFeedCooldown = 2.4f;
        PushActivityFeed($"[AC] {_integrityStatusLine}");
    }

    private static void ForceTerminateForIntegrityViolation(string category, string message)
    {
        var failMessage = $"Anti-cheat violation: {category}: {message}";

        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, IntegrityViolationLogFileName);
            var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}] {failMessage}{Environment.NewLine}";
            File.AppendAllText(logPath, line, Encoding.UTF8);
        }
        catch
        {
        }

        Environment.FailFast(failMessage);
    }

}
