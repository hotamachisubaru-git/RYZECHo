using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace RYZECHo.Prototype;

internal readonly record struct IntegrityActorSnapshot(
    PointF Position,
    float Health,
    float Shield,
    bool WasAlive,
    WeaponType Weapon);

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
    private readonly Dictionary<Actor, IntegrityActorSnapshot> _integrityActorSnapshots = [];

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

    private void SanitizeGlobalRuntimeState()
    {
        if (_credits < 0 || _credits > IntegrityHardCreditsCap)
        {
            var clampedCredits = Math.Clamp(_credits, 0, IntegrityHardCreditsCap);
            RegisterIntegrityStrike("所持金整合性", $"クレジット {_credits}c を {clampedCredits}c に補正しました。", severe: true);
            _credits = clampedCredits;
        }

        _buildPoints = Math.Clamp(_buildPoints, 0, 12);
        _currentRound = Math.Clamp(_currentRound, 1, 99);
        _playerRoundWins = Math.Clamp(_playerRoundWins, 0, 99);
        _enemyRoundWins = Math.Clamp(_enemyRoundWins, 0, 99);
        _selectedWeapon = SanitizeWeaponType(_selectedWeapon, WeaponType.Giant);
        _selectedSidearmWeapon = SanitizeWeaponType(_selectedSidearmWeapon, WeaponType.Pulse);
        _playerPrimaryWeapon = SanitizeWeaponType(_playerPrimaryWeapon, WeaponType.Giant);
        _playerSidearmWeapon = SanitizeWeaponType(_playerSidearmWeapon, WeaponType.Pulse);
        _selectedBet = Math.Clamp(_selectedBet, 0, IntegrityHardCreditsCap);
        _enemyBossInvestment = Math.Clamp(_enemyBossInvestment, 0, 1_200);
        _matchTeamEliminations = Math.Clamp(_matchTeamEliminations, 0, 999);
        _matchPlayerDeaths = Math.Clamp(_matchPlayerDeaths, 0, 999);
        _roundBossKillCount = Math.Clamp(_roundBossKillCount, 0, TeamSize);
        _roundTimer = Math.Clamp(_roundTimer, 0f, _bombPlanted ? BombFuseSeconds : RoundDurationSeconds);
        _resultTimer = Math.Clamp(_resultTimer, 0f, 4f);
        _pingCooldown = Math.Clamp(_pingCooldown, -0.25f, 3f);
        _coreHealth = Math.Clamp(_coreHealth, 0f, 180f);
        _bombPlantProgress = Math.Clamp(_bombPlantProgress, 0f, BombPlantSeconds);
        _bombDefuseProgress = Math.Clamp(_bombDefuseProgress, 0f, BombDefuseSeconds);
        _playerIdleSeconds = Math.Clamp(_playerIdleSeconds, 0f, 60f);
        _uiPulseTime = MathF.IEEERemainder(_uiPulseTime, 3600f);

        if (_activityFeed.Count > IntegrityMaxActivityFeedEntries)
        {
            _activityFeed.RemoveRange(IntegrityMaxActivityFeedEntries, _activityFeed.Count - IntegrityMaxActivityFeedEntries);
        }

        for (var index = _activityFeed.Count - 1; index >= 0; index--)
        {
            if (string.IsNullOrWhiteSpace(_activityFeed[index]))
            {
                _activityFeed.RemoveAt(index);
            }
        }

        if (_ripples.Count > IntegrityMaxRipples)
        {
            _ripples.RemoveRange(IntegrityMaxRipples, _ripples.Count - IntegrityMaxRipples);
            RegisterIntegrityAnomaly("音イベント整合性", "過剰なリップルを削減しました。");
        }

        for (var index = _ripples.Count - 1; index >= 0; index--)
        {
            var ripple = _ripples[index];
            if (ripple.Lifetime <= 0f || ripple.Lifetime > 2f || ripple.Strength <= 0f || ripple.Strength > 1.5f)
            {
                _ripples.RemoveAt(index);
            }
        }

        if (_enemies.Count > TeamSize)
        {
            var excess = _enemies.Count - TeamSize;
            _enemies.RemoveRange(TeamSize, excess);
            RegisterIntegrityStrike("敵編成整合性", $"敵ユニットが上限を超過していたため {excess} 体を除外しました。", severe: true);
        }

        EnsureBossSelectionCounterShape();
        _selectedBossName = BossCandidateNames().Contains(_selectedBossName) ? _selectedBossName : "あなた";
        EnsureBossSelectionAvailable();
        RestoreBossFlags();

        if (_activePlanter is not null && (!_activePlanter.IsAlive || !IsInsideBombSite(_activePlanter.Position, 18f)))
        {
            _activePlanter = null;
        }
    }

    private void EnsureBossSelectionCounterShape()
    {
        var expectedNames = BossCandidateNames();
        var invalidKeys = _bossSelectionCounts.Keys
            .Where(name => !expectedNames.Contains(name))
            .ToList();

        foreach (var invalidKey in invalidKeys)
        {
            _bossSelectionCounts.Remove(invalidKey);
        }

        foreach (var actorName in expectedNames)
        {
            var current = GetBossSelectionCount(actorName);
            _bossSelectionCounts[actorName] = Math.Clamp(current, 0, MaxBossSelectionsPerActor);
        }
    }

    private void SanitizeStructures()
    {
        if (_structures.Count == 0)
        {
            return;
        }

        var original = _structures.ToList();
        var acceptedCells = new HashSet<Point>();
        _structures.Clear();

        foreach (var structure in original)
        {
            if (!_buildSlots.Contains(structure.Cell) || acceptedCells.Contains(structure.Cell))
            {
                RegisterIntegrityStrike("構築物整合性", $"無効な構築物 {structure.Label} を {structure.Cell.X},{structure.Cell.Y} から除去しました。", severe: true);
                continue;
            }

            if (!TryCreateCanonicalStructure(structure, out var canonical))
            {
                RegisterIntegrityStrike("構築物整合性", "未定義の構築物を除去しました。", severe: true);
                continue;
            }

            canonical.Health = Math.Clamp(structure.Health, 0f, canonical.MaxHealth);
            canonical.PulseCooldown = Math.Clamp(structure.PulseCooldown, 0f, canonical.Kind == StructureKind.StaticNest ? 1.2f : 0.5f);

            var placementError = ValidateStructurePlacement(canonical);
            if (placementError is not null)
            {
                RegisterIntegrityStrike("構築物整合性", $"{canonical.Label} を検証で棄却: {placementError}", severe: true);
                continue;
            }

            acceptedCells.Add(canonical.Cell);
            _structures.Add(canonical);
        }
    }

    private bool TryCreateCanonicalStructure(Structure structure, out Structure canonical)
    {
        canonical = structure;
        if (!Enum.IsDefined(structure.Kind))
        {
            return false;
        }

        canonical = structure.Kind switch
        {
            StructureKind.BlastDoor => CreateStructure(BuildToolKind.BlastDoor, structure.Cell),
            StructureKind.HoneyTrap => CreateStructure(BuildToolKind.HoneyTrap, structure.Cell),
            StructureKind.StaticNest => CreateStructure(BuildToolKind.StaticNest, structure.Cell),
            StructureKind.ReconBeacon => CreateStructure(BuildToolKind.ReconBeacon, structure.Cell),
            StructureKind.ShieldRelay => CreateStructure(BuildToolKind.ShieldRelay, structure.Cell),
            _ => structure,
        };

        return true;
    }

    private void SanitizeActors(float deltaSeconds)
    {
        SanitizeActor(_player, deltaSeconds);

        foreach (var ally in _allies)
        {
            SanitizeActor(ally, deltaSeconds);
        }

        foreach (var enemy in _enemies)
        {
            SanitizeActor(enemy, deltaSeconds);
        }

        var enemyBosses = _enemies.Where(enemy => enemy.IsBoss).ToList();
        if (enemyBosses.Count > 1)
        {
            foreach (var extraBoss in enemyBosses.Skip(1))
            {
                extraBoss.IsBoss = false;
            }

            RegisterIntegrityStrike("ボス整合性", "敵ボスが複数いたため 1 体に補正しました。", severe: true);
        }
    }

    private void SanitizeActor(Actor actor, float deltaSeconds)
    {
        actor.Weapon = SanitizeWeaponType(actor.Weapon, DefaultWeaponFor(actor));
        actor.Health = Math.Clamp(actor.Health, 0f, actor.MaxHealth);
        actor.Shield = Math.Clamp(actor.Shield, 0f, actor.MaxShield);
        actor.ShieldRegenDelay = Math.Clamp(actor.ShieldRegenDelay, 0f, 6f);
        actor.PathCooldown = Math.Clamp(actor.PathCooldown, -0.25f, 3f);
        actor.FootstepCooldown = Math.Clamp(actor.FootstepCooldown, -0.25f, 2f);
        actor.FootstepPulseIndex = Math.Clamp(actor.FootstepPulseIndex, 0, 2);
        actor.FacingAngle = NormalizeAngle(actor.FacingAngle);

        var weapon = _weaponStats[actor.Weapon];
        var fireCooldownCap = Math.Max(GetActorFireCooldown(actor, weapon.FireCooldown * 1.25f), 1f);
        if (actor.FireCooldown < 0f || actor.FireCooldown > fireCooldownCap)
        {
            actor.FireCooldown = Math.Clamp(actor.FireCooldown, 0f, fireCooldownCap);
        }

        var sanitizedPosition = ResolveCollision(actor.Position, actor.Radius);
        if (Distance(actor.Position, sanitizedPosition) > 0.5f)
        {
            actor.Position = sanitizedPosition;
            RegisterIntegrityAnomaly("位置整合性", $"{actor.Name} の位置をマップ内へ補正しました。");
        }

        if (actor.Path.Count > 32)
        {
            var trimmedPath = actor.Path.Take(32).ToArray();
            actor.Path.Clear();
            foreach (var node in trimmedPath)
            {
                actor.Path.Enqueue(ResolveCollision(node, actor.Radius));
            }

            RegisterIntegrityAnomaly("経路整合性", $"{actor.Name} の経路キューを短縮しました。");
        }

        _ = deltaSeconds;
    }

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
        var roundResultSwing = WinRewardCredits + (effectiveBet * 2);

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

    private WeaponType DefaultWeaponFor(Actor actor)
    {
        return actor.Type switch
        {
            ActorType.Player => WeaponType.Giant,
            ActorType.Ally when actor.Name == "北アンカー" => WeaponType.Violet,
            ActorType.Ally when actor.Name == "南アンカー" => WeaponType.Blitz,
            ActorType.Ally when actor.Name == "中央リンク" => WeaponType.Fairy,
            ActorType.Enemy => WeaponType.Blitz,
            _ => WeaponType.Giant,
        };
    }

    private void RegisterIntegrityStrike(string category, string message, bool severe = false)
    {
        _integrityStrikeCount++;
        _integrityStatusLine = $"{category}: {message}";
        _integrityRewardsLocked = true;
        _ = severe;
        ForceTerminateForIntegrityViolation(category, message);
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

    private static byte[] ProgressIntegrityEntropy()
    {
        return Encoding.UTF8.GetBytes($"{ProgressIntegrityVersion}|{AppContext.BaseDirectory}|RYZECHo::Prototype");
    }

    private static string EnsureProgressSalt(string? salt)
    {
        return string.IsNullOrWhiteSpace(salt) ? Guid.NewGuid().ToString("N") : salt;
    }

    private static string CreateProgressIntegrityPayload(ProgressProfile profile)
    {
        static string JoinDistinct(IEnumerable<string> values)
        {
            return string.Join("|", values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal));
        }

        return string.Join("::",
        [
            ProgressIntegrityVersion,
            EnsureProgressSalt(profile.IntegritySalt),
            profile.AccountLevel.ToString(),
            profile.CurrentXp.ToString(),
            profile.AgentCredits.ToString(),
            profile.RankRating.ToString(),
            profile.MatchesPlayed.ToString(),
            profile.MatchesWon.ToString(),
            profile.ContractsCompleted.ToString(),
            profile.ActiveContract,
            profile.ActiveContractProgress.ToString(),
            JoinDistinct(profile.UnlockedAgents),
            JoinDistinct(profile.UnlockedStructureSkins),
            JoinDistinct(profile.UnlockedAdThemes),
            JoinDistinct(profile.UnlockedBanners),
            profile.SelectedStructureSkin,
            profile.SelectedAdTheme,
        ]);
    }

    private static string CreateProgressIntegrityStamp(ProgressProfile profile)
    {
        profile.IntegritySalt = EnsureProgressSalt(profile.IntegritySalt);
        var payloadBytes = SHA256.HashData(Encoding.UTF8.GetBytes(CreateProgressIntegrityPayload(profile)));
        var protectedHash = ProtectedData.Protect(payloadBytes, ProgressIntegrityEntropy(), DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedHash);
    }

    private static bool HasValidProgressIntegrity(ProgressProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.IntegritySalt) || string.IsNullOrWhiteSpace(profile.IntegrityStamp))
        {
            return true;
        }

        try
        {
            var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(CreateProgressIntegrityPayload(profile)));
            var storedHash = ProtectedData.Unprotect(Convert.FromBase64String(profile.IntegrityStamp), ProgressIntegrityEntropy(), DataProtectionScope.CurrentUser);
            return CryptographicOperations.FixedTimeEquals(expectedHash, storedHash);
        }
        catch
        {
            return false;
        }
    }

    private static void NormalizeProgressList(List<string> source, IEnumerable<string> allowedValues, int maxCount = 16)
    {
        var allowed = new HashSet<string>(allowedValues, StringComparer.Ordinal);
        var normalized = source
            .Where(value => !string.IsNullOrWhiteSpace(value) && allowed.Contains(value))
            .Distinct(StringComparer.Ordinal)
            .Take(maxCount)
            .ToList();

        source.Clear();
        source.AddRange(normalized);
    }

    private static void NormalizeLooseProgressList(List<string> source, int maxCount = 32)
    {
        var normalized = source
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Take(maxCount)
            .ToList();

        source.Clear();
        source.AddRange(normalized);
    }

    private bool IsIntegrityRewardsLocked()
    {
        return _integrityRewardsLocked;
    }

    private string IntegrityRewardLockSummary()
    {
        return string.IsNullOrWhiteSpace(_integrityStatusLine)
            ? "整合性違反検知により報酬と進行保存を凍結"
            : $"整合性違反検知により報酬と進行保存を凍結 ({_integrityStatusLine})";
    }

    private void ResetIntegrityRewardsLock()
    {
        _integrityRewardsLocked = false;
    }
}
