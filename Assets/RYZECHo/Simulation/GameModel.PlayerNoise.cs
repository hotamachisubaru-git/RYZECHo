namespace RYZECHo;

public sealed partial class GameModel
{
    private void UpdatePlayerIdleState(float deltaSeconds, bool acted)
    {
        if (_phase != GamePhase.Hunt || !_player.IsAlive)
        {
            _playerIdleSeconds = 0f;
            _breathingRippleCooldown = 0f;
            return;
        }

        var wasExposed = IsPlayerBreathingExposed();
        if (acted)
        {
            _playerIdleSeconds = 0f;
            _breathingRippleCooldown = 0f;
            return;
        }

        _playerIdleSeconds += deltaSeconds;
        if (!wasExposed && IsPlayerBreathingExposed())
        {
            PushActivityFeed("10 秒以上静止したため呼吸音が増幅。近距離の敵に位置が漏れやすくなります。");
        }

        if (IsPlayerBreathingExposed() && !IsPlayerSilenced())
        {
            _breathingRippleCooldown -= deltaSeconds;
            if (_breathingRippleCooldown <= 0f)
            {
                EmitRipple(_player.Position, 0.42f, RippleKind.Breathing, Color.FromArgb(255, 255, 132, 108));
                _breathingRippleCooldown = BreathingRippleIntervalSeconds;
            }
        }
    }

    private bool IsPlayerBreathingExposed()
    {
        return _phase == GamePhase.Hunt && _player.IsAlive && _playerIdleSeconds >= BreathingRippleIntervalSeconds;
    }
}
