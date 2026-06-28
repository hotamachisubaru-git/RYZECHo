namespace RYZECHo;

public sealed partial class GameModel
{
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
            var hearing = AudioRippleVisualRules.CalculateHearingRange(
                ally.HearingRange,
                _weaponStats[ally.Weapon].HearingMultiplier,
                strength,
                GetAudioOcclusionProfile(ally.Position, position));
            if (Distance(ally.Position, position) <= hearing)
            {
                return true;
            }
        }

        return false;
    }
}
