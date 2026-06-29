namespace RYZECHo;

internal sealed partial class GameModel
{
    // =========================================================================
    // Boss — ボス選択・ボス経済・ボスバフ
    // =========================================================================

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
        return new string[] { _player.Name }.Concat(_allies.Select(actor => actor.Name)).ToArray();
    }

    private int GetBossSelectionCount(string actorName)
    {
        return _bossSelectionCounts.TryGetValue(actorName, out var count) ? count : 0;
    }

    private bool AllBossSelectionsSpent()
    {
        return BossSelectionRules.AllSelectionsSpent(BossCandidateNames(), _bossSelectionCounts, MaxBossSelectionsPerActor);
    }

    private bool CanSelectBoss(string actorName)
    {
        return BossSelectionRules.CanSelect(actorName, BossCandidateNames(), _bossSelectionCounts, MaxBossSelectionsPerActor);
    }

    private int BossSelectionsRemaining(string actorName)
    {
        return BossSelectionRules.SelectionsRemaining(actorName, BossCandidateNames(), _bossSelectionCounts, MaxBossSelectionsPerActor);
    }

    private bool TrySelectBoss(string actorName)
    {
        var resolved = BossSelectionRules.ResolveSelection(actorName, BossCandidateNames(), _bossSelectionCounts, MaxBossSelectionsPerActor);
        if (resolved == actorName)
        {
            _selectedBossName = actorName;
            return true;
        }

        _selectedBossName = resolved;
        SetResultMessage($"{actorName} は既に 2 回選出済みのため、{resolved} に切り替えました。");
        return false;
    }

    private void EnsureBossSelectionAvailable()
    {
        _selectedBossName = BossSelectionRules.ResolveSelection(_selectedBossName, BossCandidateNames(), _bossSelectionCounts, MaxBossSelectionsPerActor);
    }

    private float BossInvestmentCoreFactor(int investment)
    {
        return BossEconomyRules.CalculateBuff(investment, OptimalBossInvestment).CoreFactor;
    }

    private float BossMoveBonusPercent(int investment)
    {
        return BossEconomyRules.CalculateBuff(investment, OptimalBossInvestment).MoveBonusPercent;
    }

    private float BossReloadBonusPercent(int investment)
    {
        return BossEconomyRules.CalculateBuff(investment, OptimalBossInvestment).FireRateBonusPercent;
    }

    private string BossBuffSummary(int investment)
    {
        return $"移動 +{BossMoveBonusPercent(investment) * 100f:0}% / 射撃 +{BossReloadBonusPercent(investment) * 100f:0}%";
    }

    private float BossInvestmentProgress(int investment)
    {
        return Math.Clamp(BossInvestmentCoreFactor(investment), 0f, 1f);
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
