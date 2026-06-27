namespace RYZECHo;

internal sealed partial class GameModel
{
    private void UpdateEnemies(float deltaSeconds)
    {
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            UpdateShieldRegen(enemy, deltaSeconds);
            UpdateActorAbilityState(enemy, deltaSeconds);
            var weapon = _weaponStats[enemy.Weapon];
            enemy.FireCooldown = MathF.Max(0f, enemy.FireCooldown - deltaSeconds);
            enemy.PathCooldown -= deltaSeconds;
            enemy.FootstepCooldown -= deltaSeconds;

            if (IsSystemCrashActive() || IsActorLockedDown(enemy))
            {
                enemy.Path.Clear();
                continue;
            }

            var target = PickEnemyTarget(enemy);
            TryUseAutonomousAgentAbility(enemy, target, deltaSeconds);

            if (target is not null && Distance(enemy.Position, target.Position) <= weapon.ProjectileRange && HasLineOfSight(enemy, target.Position))
            {
                enemy.Path.Clear();
                enemy.FacingAngle = MathF.Atan2(target.Position.Y - enemy.Position.Y, target.Position.X - enemy.Position.X);
                if (enemy.FireCooldown <= 0f)
                {
                    ApplyDamage(target, weapon.Damage, enemy);
                    enemy.FireCooldown = GetActorFireCooldown(enemy, weapon.FireCooldown * 1.2f);
                    EmitRipple(enemy.Position, 1f, RippleKind.Gunshot, Color.FromArgb(255, 108, 82));
                }
            }
            else if (enemy.PathCooldown <= 0f)
            {
                RebuildEnemyPath(enemy);
            }

            if (EnemyTeamAttacking() && !_bombPlanted && IsInsideBombSite(enemy.Position, _attackFocusSite, 10f))
            {
                enemy.Path.Clear();
                enemy.FacingAngle = MathF.Atan2(BombSitePosition(_attackFocusSite).Y - enemy.Position.Y, BombSitePosition(_attackFocusSite).X - enemy.Position.X);
                continue;
            }

            FollowPathActor(enemy, deltaSeconds, true);
        }
    }
}
