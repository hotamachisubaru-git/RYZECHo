namespace RYZECHo;

internal sealed partial class GameModel
{
    private void UpdatePlayer(float deltaSeconds, InputSnapshot input)
    {
        if (!_player.IsAlive)
        {
            UpdatePlayerIdleState(deltaSeconds, true);
            return;
        }

        if (input.PressQ)
        {
            _player.Weapon = _playerPrimaryWeapon;
        }
        else if (input.PressE)
        {
            _player.Weapon = _playerSidearmWeapon;
        }

        UpdateShieldRegen(_player, deltaSeconds);
        UpdateActorAbilityState(_player, deltaSeconds);
        var weapon = _weaponStats[_player.Weapon];
        var worldMousePosition = ScreenToWorldPoint(input.MousePosition);
        var movement = PointF.Empty;
        var acted = false;

        if (input.MoveUp)
        {
            movement.Y -= 1f;
        }

        if (input.MoveDown)
        {
            movement.Y += 1f;
        }

        if (input.MoveLeft)
        {
            movement.X -= 1f;
        }

        if (input.MoveRight)
        {
            movement.X += 1f;
        }

        if (movement != PointF.Empty && !IsActorLockedDown(_player))
        {
            var length = MathF.Sqrt((movement.X * movement.X) + (movement.Y * movement.Y));
            movement = new PointF(movement.X / length, movement.Y / length);

            var next = new PointF(
                _player.Position.X + (movement.X * weapon.MoveSpeed * GetActorMoveSpeedMultiplier(_player) * deltaSeconds),
                _player.Position.Y + (movement.Y * weapon.MoveSpeed * GetActorMoveSpeedMultiplier(_player) * deltaSeconds));

            _player.Position = ResolveCollision(next, _player.Radius);
            acted = true;
        }

        _player.FireCooldown = MathF.Max(0f, _player.FireCooldown - deltaSeconds);

        var aimVector = new PointF(worldMousePosition.X - _player.Position.X, worldMousePosition.Y - _player.Position.Y);
        if (Math.Abs(aimVector.X) > 0.01f || Math.Abs(aimVector.Y) > 0.01f)
        {
            _player.FacingAngle = MathF.Atan2(aimVector.Y, aimVector.X);
        }

        if (input.FireHeld && _player.FireCooldown <= 0f && !IsActorSystemCrashed(_player) && !IsActorLockedDown(_player))
        {
            var target = PickRaycastTarget(_player.Position, worldMousePosition, weapon.ProjectileRange);
            if (target is not null)
            {
                ApplyDamage(target, weapon.Damage * PlayerDamageMultiplier(), _player);
            }

            _player.FireCooldown = GetActorFireCooldown(_player, weapon.FireCooldown);
            acted = true;
        }

        if (input.InteractHeld && IsInsideBombSite(_player.Position, 10f))
        {
            acted = true;
        }

        RevealEnemiesInActorVision(_player);

        UpdatePlayerIdleState(deltaSeconds, acted);
    }
}
