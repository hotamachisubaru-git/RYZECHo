namespace RYZECHo;

public sealed partial class GameModel
{
    private float AgentSkillCooldown(AgentAbilitySlot slot)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => _agentSkillOneCooldown,
            AgentAbilitySlot.SkillTwo => _agentSkillTwoCooldown,
            _ => 0f,
        };
    }

    private float AgentAbilityProgress(AgentAbilitySlot slot)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => 1f - (_agentSkillOneCooldown / AgentSkillOneCooldownSeconds),
            AgentAbilitySlot.SkillTwo => 1f - (_agentSkillTwoCooldown / AgentSkillTwoCooldownSeconds),
            _ => GetUltPoints(_player.Name) / (float)MaxUltPoints,
        };
    }

    private bool AgentAbilityReady(AgentAbilitySlot slot)
    {
        return slot switch
        {
            AgentAbilitySlot.SkillOne => _agentSkillOneCooldown <= 0f,
            AgentAbilitySlot.SkillTwo => _agentSkillTwoCooldown <= 0f,
            _ => GetUltPoints(_player.Name) >= MaxUltPoints,
        };
    }

    private string AgentAbilityName(AgentAbilitySlot slot)
    {
        var profile = PlayerAgentProfile();
        return slot switch
        {
            AgentAbilitySlot.SkillOne => profile.SkillOne,
            AgentAbilitySlot.SkillTwo => profile.SkillTwo,
            _ => profile.Ultimate,
        };
    }

    private string AgentRuntimeSummary()
    {
        var profile = PlayerAgentProfile();
        return $"{profile.Name} / 1:{profile.SkillOne} 2:{profile.SkillTwo} 3:{profile.Ultimate} ULT {GetUltPoints(_player.Name)}/{MaxUltPoints}";
    }

    private bool IsSystemCrashActive()
    {
        return _systemCrashTimer > 0f || _worldEffects.Any(effect => effect.Kind == WorldEffectKind.SystemCrash && effect.OwnerType != ActorType.Enemy);
    }

    private float PlayerDamageMultiplier()
    {
        return _playerOverdriveTimer > 0f ? 1.2f : 1f;
    }

    private IEnumerable<Actor> OpponentsOf(ActorType ownerType)
    {
        return IsFriendlyActorType(ownerType)
            ? _enemies.Where(actor => actor.IsAlive)
            : LivePlayerTeam();
    }

    private Actor? EffectOwnerActor(ActorType ownerType)
    {
        if (ownerType == ActorType.Enemy)
        {
            return _enemies.FirstOrDefault(actor => actor.IsAlive);
        }

        return _player.IsAlive ? _player : _allies.FirstOrDefault(actor => actor.IsAlive);
    }
}
