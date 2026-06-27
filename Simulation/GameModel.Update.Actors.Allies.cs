namespace RYZECHo;

internal sealed partial class GameModel
{
    private void UpdateAllies(float deltaSeconds)
    {
        foreach (var ally in _allies)
        {
            if (!ally.IsAlive)
            {
                continue;
            }

            UpdateShieldRegen(ally, deltaSeconds);
            UpdateActorAbilityState(ally, deltaSeconds);
            ally.FireCooldown = MathF.Max(0f, ally.FireCooldown - deltaSeconds);
            var weapon = _weaponStats[ally.Weapon];
            var target = PickBestTarget(ally.Position, weapon.VisionRange, ActorType.Ally);
            TryUseAutonomousAgentAbility(ally, target, deltaSeconds);

            if (IsActorSystemCrashed(ally) || IsActorLockedDown(ally))
            {
                ally.Path.Clear();
                RevealEnemiesInActorVision(ally);
                continue;
            }

            if (target is null)
            {
                if (IsPlayerTeamAttacking())
                {
                    UpdateAttackingAllyMovement(ally, deltaSeconds);
                }

                RevealEnemiesInActorVision(ally);
                continue;
            }

            ally.Path.Clear();
            ally.FacingAngle = MathF.Atan2(target.Position.Y - ally.Position.Y, target.Position.X - ally.Position.X);

            if (ally.FireCooldown <= 0f)
            {
                ApplyDamage(target, weapon.Damage, ally);
                ally.FireCooldown = GetActorFireCooldown(ally, weapon.FireCooldown);
                EmitRipple(ally.Position, 0.95f, RippleKind.Gunshot, Color.FromArgb(235, 85, 80));
            }

            if (_pingCooldown <= 0f)
            {
                EmitRipple(target.Position, 0.7f, RippleKind.Skill, Color.FromArgb(245, 208, 96));
                _pingCooldown = 0.7f;
            }

            RevealEnemiesInActorVision(ally);
        }
    }
}
