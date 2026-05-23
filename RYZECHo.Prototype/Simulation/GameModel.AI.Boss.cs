namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private void RestoreBossFlags()
    {
        _player.IsBoss = _selectedBossName == _player.Name;
        foreach (var ally in _allies)
        {
            ally.IsBoss = ally.Name == _selectedBossName;
        }
    }

    private string[] BossCandidateNames()
    {
        return [_player.Name, .. _allies.Select(actor => actor.Name)];
    }

    private int GetBossSelectionCount(string actorName)
    {
        return _bossSelectionCounts.TryGetValue(actorName, out var count) ? count : 0;
    }

    private bool AllBossSelectionsSpent()
    {
        return BossCandidateNames().All(name => GetBossSelectionCount(name) >= MaxBossSelectionsPerActor);
    }

    private bool CanSelectBoss(string actorName)
    {
        return AllBossSelectionsSpent() || GetBossSelectionCount(actorName) < MaxBossSelectionsPerActor;
    }

    private int BossSelectionsRemaining(string actorName)
    {
        if (AllBossSelectionsSpent())
        {
            return 1;
        }

        return Math.Max(0, MaxBossSelectionsPerActor - GetBossSelectionCount(actorName));
    }

    private bool TrySelectBoss(string actorName)
    {
        if (CanSelectBoss(actorName))
        {
            _selectedBossName = actorName;
            return true;
        }

        var fallback = BossCandidateNames().FirstOrDefault(CanSelectBoss);
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            _selectedBossName = fallback;
            SetResultMessage($"{actorName} は既に 2 回選出済みのため、{fallback} に切り替えました。");
            return false;
        }

        _selectedBossName = actorName;
        return true;
    }

    private void EnsureBossSelectionAvailable()
    {
        if (CanSelectBoss(_selectedBossName))
        {
            return;
        }

        var fallback = BossCandidateNames().FirstOrDefault(CanSelectBoss);
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            _selectedBossName = fallback;
        }
    }

    private float BossInvestmentCoreFactor(int investment)
    {
        if (investment <= 0)
        {
            return 0f;
        }

        var normalized = Math.Clamp(Math.Min(investment, OptimalBossInvestment) / (float)OptimalBossInvestment, 0f, 1f);
        var quadraticPeak = (2f * normalized) - (normalized * normalized);
        if (investment <= OptimalBossInvestment)
        {
            return quadraticPeak;
        }

        var overflow = investment - OptimalBossInvestment;
        var tail = 0.22f * (1f - MathF.Exp(-overflow / 180f));
        return quadraticPeak + tail;
    }

    private float BossMoveBonusPercent(int investment)
    {
        return BossInvestmentCoreFactor(investment) * 0.12f;
    }

    private float BossReloadBonusPercent(int investment)
    {
        return BossInvestmentCoreFactor(investment) * 0.18f;
    }

    private string BossBuffSummary(int investment)
    {
        return $"移動 +{BossMoveBonusPercent(investment) * 100f:0}% / 射撃 +{BossReloadBonusPercent(investment) * 100f:0}%";
    }

    private float BossInvestmentProgress(int investment)
    {
        return Math.Clamp(BossInvestmentCoreFactor(investment) / 1.22f, 0f, 1f);
    }

    private int CurrentBossInvestment(Actor actor)
    {
        if (!actor.IsBoss)
        {
            return 0;
        }

        return actor.Type == ActorType.Enemy ? _enemyBossInvestment : GetFriendlyInvestment(actor.Name);
    }

    private float GetActorMoveSpeedMultiplier(Actor actor)
    {
        var multiplier = 1f + BossMoveBonusPercent(CurrentBossInvestment(actor));
        if (actor.Type == ActorType.Player)
        {
            if (_playerDashTimer > 0f)
            {
                multiplier *= 2.4f;
            }

            if (_playerOverdriveTimer > 0f)
            {
                multiplier *= 1.22f;
            }
        }
        else
        {
            if (actor.DashTimer > 0f)
            {
                multiplier *= 1.85f;
            }

            if (actor.OverdriveTimer > 0f)
            {
                multiplier *= 1.18f;
            }
        }

        return multiplier;
    }

    private float GetActorFireCooldown(Actor actor, float baseCooldown)
    {
        var fireRateBonus = 1f + BossReloadBonusPercent(CurrentBossInvestment(actor));
        if (actor.Type == ActorType.Player && _playerOverdriveTimer > 0f)
        {
            fireRateBonus *= 1.28f;
        }
        else if (actor.Type != ActorType.Player && actor.OverdriveTimer > 0f)
        {
            fireRateBonus *= 1.18f;
        }

        return baseCooldown / Math.Max(1f, fireRateBonus);
    }

    private static bool IsCloseRangeWeapon(WeaponType weapon)
    {
        return weapon is WeaponType.Blitz or WeaponType.Monster or WeaponType.Melt;
    }

    private static bool IsMidRangeWeapon(WeaponType weapon)
    {
        return weapon is WeaponType.Fairy or WeaponType.Giant or WeaponType.Juggernaut;
    }

    private int AffordableCredits()
    {
        return Math.Max(0, _credits - _weaponStats[_selectedWeapon].Cost - _weaponStats[_selectedSidearmWeapon].Cost);
    }

    private Actor? SelectedBoss()
    {
        if (_selectedBossName == _player.Name)
        {
            return _player;
        }

        return _allies.FirstOrDefault(actor => actor.Name == _selectedBossName);
    }

}
