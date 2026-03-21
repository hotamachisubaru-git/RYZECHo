namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private static WeaponType[] PrimaryWeaponSelectionOrder()
    {
        return
        [
            WeaponType.Blitz,
            WeaponType.Monster,
            WeaponType.Melt,
            WeaponType.Fairy,
            WeaponType.Giant,
            WeaponType.Juggernaut,
            WeaponType.Violet,
            WeaponType.Changer,
            WeaponType.Howl,
        ];
    }

    private static WeaponType[] SidearmSelectionOrder()
    {
        return
        [
            WeaponType.Pulse,
            WeaponType.Shard,
        ];
    }

    private static bool IsSidearmWeapon(WeaponType weaponType)
    {
        return weaponType is WeaponType.Pulse or WeaponType.Shard;
    }

    private bool IsPrimaryWeapon(WeaponType weaponType)
    {
        return !IsSidearmWeapon(weaponType) && _weaponStats.ContainsKey(weaponType);
    }

    private WeaponType SelectedLoadoutWeapon()
    {
        return _selectedLoadoutFocus == LoadoutFocus.Primary ? _selectedWeapon : _selectedSidearmWeapon;
    }

    private void ToggleLoadoutFocus()
    {
        _selectedLoadoutFocus = _selectedLoadoutFocus == LoadoutFocus.Primary ? LoadoutFocus.Sidearm : LoadoutFocus.Primary;
    }

    private void CycleLoadoutWeapon(int direction)
    {
        var order = _selectedLoadoutFocus == LoadoutFocus.Primary ? PrimaryWeaponSelectionOrder() : SidearmSelectionOrder();
        var current = SelectedLoadoutWeapon();
        var index = Array.IndexOf(order, current);
        if (index < 0)
        {
            index = 0;
        }

        var next = order[(index + direction + order.Length) % order.Length];
        if (_selectedLoadoutFocus == LoadoutFocus.Primary)
        {
            _selectedWeapon = next;
        }
        else
        {
            _selectedSidearmWeapon = next;
        }
    }

    private Actor? FriendlyActorByName(string actorName)
    {
        if (actorName == _player.Name)
        {
            return _player;
        }

        return _allies.FirstOrDefault(actor => actor.Name == actorName);
    }

    private void EnsureFriendlyEconomyState()
    {
        foreach (var actorName in BossCandidateNames())
        {
            if (!_bossInvestments.ContainsKey(actorName))
            {
                _bossInvestments[actorName] = actorName == _player.Name ? OptimalBossInvestment : 0;
            }

            if (!_ultPoints.ContainsKey(actorName))
            {
                _ultPoints[actorName] = 0;
            }
        }

        SyncSelectedBetTotal();
    }

    private int GetFriendlyInvestment(string actorName)
    {
        EnsureFriendlyEconomyState();
        return _bossInvestments.TryGetValue(actorName, out var amount) ? amount : 0;
    }

    private void SetFriendlyInvestment(string actorName, int amount)
    {
        EnsureFriendlyEconomyState();
        _bossInvestments[actorName] = Math.Max(0, amount);
        SyncSelectedBetTotal();
    }

    private void AdjustSelectedInvestment(int delta)
    {
        EnsureFriendlyEconomyState();
        var actorName = _selectedBossName;
        var otherInvestments = TotalSelectedInvestment() - GetFriendlyInvestment(actorName);
        var maxInvestment = Math.Max(0, _credits - _weaponStats[_selectedWeapon].Cost - _weaponStats[_selectedSidearmWeapon].Cost - otherInvestments);
        var next = Math.Clamp(GetFriendlyInvestment(actorName) + delta, 0, maxInvestment);
        _bossInvestments[actorName] = next;
        SyncSelectedBetTotal();
    }

    private int TotalSelectedInvestment()
    {
        EnsureFriendlyEconomyState();
        return BossCandidateNames().Sum(GetFriendlyInvestment);
    }

    private int SelectedBossInvestment()
    {
        return GetFriendlyInvestment(_selectedBossName);
    }

    private void SyncSelectedBetTotal()
    {
        _selectedBet = TotalSelectedInvestment();
    }

    private int GetUltPoints(string actorName)
    {
        EnsureFriendlyEconomyState();
        return _ultPoints.TryGetValue(actorName, out var amount) ? amount : 0;
    }

    private void AwardUltPoints(string actorName, int amount, string reason)
    {
        if (amount <= 0 || !_ultPoints.ContainsKey(actorName))
        {
            return;
        }

        var before = _ultPoints[actorName];
        var after = Math.Clamp(before + amount, 0, MaxUltPoints);
        _ultPoints[actorName] = after;
        if (after > before)
        {
            PushActivityFeed($"{actorName} ULT +{after - before} ({reason})。{after}/{MaxUltPoints}");
        }
    }

    private int SelectedBossUltPoints()
    {
        return GetUltPoints(_selectedBossName);
    }

    private void ResetSharedVision()
    {
        _sharedVisionTimers.Clear();
    }

    private void UpdateSharedVision(float deltaSeconds)
    {
        if (_sharedVisionTimers.Count == 0)
        {
            return;
        }

        var expired = new List<string>();
        foreach (var pair in _sharedVisionTimers)
        {
            var remaining = MathF.Max(0f, pair.Value - deltaSeconds);
            _sharedVisionTimers[pair.Key] = remaining;
            if (remaining <= 0f)
            {
                expired.Add(pair.Key);
            }
        }

        foreach (var key in expired)
        {
            _sharedVisionTimers.Remove(key);
        }
    }

    private void RevealEnemyToTeam(Actor enemy, float duration = SharedVisionDurationSeconds)
    {
        if (enemy.Type != ActorType.Enemy)
        {
            return;
        }

        var next = Math.Max(duration, _sharedVisionTimers.GetValueOrDefault(enemy.Name));
        _sharedVisionTimers[enemy.Name] = next;
    }

    private bool IsEnemySharedVisible(Actor enemy)
    {
        return _sharedVisionTimers.TryGetValue(enemy.Name, out var remaining) && remaining > 0f;
    }

    private bool TeamCanPerceive(PointF position, float strength)
    {
        if (_phase != GamePhase.Hunt)
        {
            return true;
        }

        if (PlayerCanPerceive(position, strength))
        {
            return true;
        }

        foreach (var ally in _allies.Where(actor => actor.IsAlive))
        {
            var hearing = ally.HearingRange * _weaponStats[ally.Weapon].HearingMultiplier * 1.8f * strength;
            if (Distance(ally.Position, position) <= hearing)
            {
                return true;
            }
        }

        return false;
    }

    private ObjectiveSite[] GetBombSites()
    {
        var defenseSites = new[]
        {
            new ObjectiveSite(ObjectiveSiteId.Alpha, "A", new Point(14, 4)),
            new ObjectiveSite(ObjectiveSiteId.Bravo, "B", new Point(14, 8)),
        };

        if (!IsPlayerTeamAttacking())
        {
            return defenseSites;
        }

        return defenseSites
            .Select(site => site with { Cell = MirrorCellHorizontally(site.Cell) })
            .ToArray();
    }

    private ObjectiveSite GetBombSite(ObjectiveSiteId siteId)
    {
        return GetBombSites().First(site => site.Id == siteId);
    }

    private ObjectiveSiteId CurrentObjectiveSiteId()
    {
        if (_armedBombSiteId is not null)
        {
            return _armedBombSiteId.Value;
        }

        if (_activePlanter is not null && TryGetBombSiteAt(_activePlanter.Position, out var activeSite, 10f))
        {
            return activeSite.Id;
        }

        return _attackFocusSite;
    }

    private string CurrentObjectiveSiteLabel()
    {
        return GetBombSite(CurrentObjectiveSiteId()).Label;
    }

    private bool TryGetBombSiteAt(PointF position, out ObjectiveSite site, float padding = 0f)
    {
        foreach (var candidate in GetBombSites())
        {
            if (Distance(position, CellCenter(candidate.Cell)) <= BombSiteRadius + padding)
            {
                site = candidate;
                return true;
            }
        }

        site = default;
        return false;
    }

    private ObjectiveSite FindClosestSite(PointF position)
    {
        return GetBombSites()
            .OrderBy(site => Distance(position, CellCenter(site.Cell)))
            .First();
    }

    private ObjectiveSiteId ChooseAttackFocusSite()
    {
        var parity = (_currentRound + _playerRoundWins + _enemyRoundWins) % 2;
        return parity == 0 ? ObjectiveSiteId.Alpha : ObjectiveSiteId.Bravo;
    }
}
