namespace RYZECHo;

internal sealed partial class GameModel
{
    // =========================================================================
    // Enemy Squad — 敵スコード生成
    // =========================================================================

    private void CreateEnemySquad()
    {
        var enemyCells = GetEnemySetupCells()
            .OrderBy(_ => _random.Next())
            .Take(TeamSize)
            .ToArray();
        var loadout = EnemyTeamAttacking()
            ? new List<WeaponType> { WeaponType.Blitz, WeaponType.Monster, WeaponType.Fairy, WeaponType.Howl }
            : new List<WeaponType> { WeaponType.Violet, WeaponType.Giant, WeaponType.Fairy, WeaponType.Monster };
        loadout = loadout
            .OrderBy(_ => _random.Next())
            .ToList();
        var agents = AgentCatalog.SelectionOrder
            .OrderBy(_ => _random.Next())
            .Take(TeamSize)
            .ToList();
        _enemyBossInvestment = Math.Clamp(180 + (_enemyRoundWins * 35) + (_currentRound * 20) + _random.Next(-40, 61), 120, 420);

        for (var index = 0; index < enemyCells.Length; index++)
        {
            var enemy = CreateEnemyActor($"{(EnemyTeamAttacking() ? "襲撃者" : "守備者")}-{index + 1}", enemyCells[index], loadout[index], agents[index]);
            _enemies.Add(enemy);
            EmitRipple(enemy.Position, 0.82f, RippleKind.Skill, Color.FromArgb(245, 202, 96));
        }

        var enemyBoss = _enemies
            .OrderByDescending(enemy => _weaponStats[enemy.Weapon].Cost)
            .ThenBy(_ => _random.Next())
            .FirstOrDefault();
        if (enemyBoss is not null)
        {
            enemyBoss.IsBoss = true;
            EmitRipple(enemyBoss.Position, 0.94f, RippleKind.Skill, Color.FromArgb(255, 222, 122));
        }
    }

    private Actor CreateEnemyActor(string name, Point spawnCell, WeaponType weapon, AgentKind agent)
    {
        var stats = _weaponStats[weapon];
        var enemyHealth = IsCloseRangeWeapon(weapon) ? 52f : IsMidRangeWeapon(weapon) ? 60f : 48f;
        var enemyShield = IsCloseRangeWeapon(weapon) ? 18f : IsMidRangeWeapon(weapon) ? 26f : 16f;
        return new Actor
        {
            Name = name,
            Agent = agent,
            Type = ActorType.Enemy,
            HomeCell = spawnCell,
            Weapon = weapon,
            Position = CellCenter(spawnCell),
            Radius = 13f,
            MaxHealth = enemyHealth,
            MaxShield = enemyShield,
            Health = enemyHealth,
            Shield = enemyShield,
            HearingRange = 260f,
            BaseMoveSpeed = stats.MoveSpeed * 0.7f,
        };
    }
}
