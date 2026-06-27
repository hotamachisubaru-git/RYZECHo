namespace RYZECHo;

internal sealed partial class GameModel
{
    private bool IsActorOnHoneyTrap(Actor actor)
    {
        var cell = WorldToCell(actor.Position);
        return _structures.Any(structure => structure.Kind == StructureKind.HoneyTrap && structure.Cell == cell);
    }

    private bool IsActorInStaticField(Actor actor)
    {
        return _structures.Any(structure => structure.Kind == StructureKind.StaticNest && Distance(actor.Position, CellCenter(structure.Cell)) <= 90f);
    }

    private void ApplyDamage(Actor actor, float damage, Actor? attacker = null)
    {
        if (!actor.IsAlive || damage <= 0f)
        {
            return;
        }

        var wasAlive = actor.IsAlive;
        actor.ShieldRegenDelay = 2.4f;
        if (actor.Shield > 0f)
        {
            var absorbed = MathF.Min(actor.Shield, damage);
            actor.Shield -= absorbed;
            damage -= absorbed;
        }

        if (damage > 0f)
        {
            actor.Health = MathF.Max(0f, actor.Health - damage);
        }

        // DamageEvent の Emit（攻撃元がいて、ダメージが発生した場合）
        if (attacker is not null && damage > 0f)
        {
            _eventBus.Emit(new DamageEvent(
                attacker.Name,
                actor.Name,
                damage,
                attacker.Weapon,
                false));
        }

        if (wasAlive && !actor.IsAlive)
        {
            HandleActorEliminated(attacker, actor);
        }
    }

    private static void UpdateShieldRegen(Actor actor, float deltaSeconds)
    {
        if (!actor.IsAlive || actor.MaxShield <= 0f)
        {
            return;
        }

        actor.ShieldRegenDelay = MathF.Max(0f, actor.ShieldRegenDelay - deltaSeconds);
        if (actor.ShieldRegenDelay > 0f || actor.Shield >= actor.MaxShield)
        {
            return;
        }

        actor.Shield = MathF.Min(actor.MaxShield, actor.Shield + (actor.MaxShield * 0.22f * deltaSeconds) + (8f * deltaSeconds));
    }

    private void HandleActorEliminated(Actor? attacker, Actor victim)
    {
        if (attacker is null || ReferenceEquals(attacker, victim))
        {
            PushActivityFeed($"{victim.Name} が戦闘不能。");
            _eventBus.Emit(new ActorDeathEvent(victim.Name, victim.Type, victim.Position, attacker?.Name));
            return;
        }

        // 味方チームによる敵の撃破判定（正常なスコア加算）
        var playerTeamScoredKill = attacker.Type != ActorType.Enemy && victim.Type == ActorType.Enemy;
        if (playerTeamScoredKill)
        {
            _matchTeamEliminations++;
            _credits += KillRewardCredits;
            PushActivityFeed($"{attacker.Name} が {victim.Name} を撃破。+{KillRewardCredits}c。");
            EmitCosmeticEliminationEffect(victim.Position);
            AwardUltPoints(attacker.Name, 1, "撃破");

            if (attacker.IsBoss)
            {
                _roundBossKillCount++;
                var livingAllies = LivePlayerTeam().Count();
                var dividend = BossEconomyRules.CalculateKillDividend(livingAllies, BossKillDividendCredits);
                if (dividend > 0)
                {
                    _credits += dividend;
                    PushActivityFeed($"ボス撃破配当。生存中の味方 {livingAllies} 名へ +{BossKillDividendCredits}c、合計 +{dividend}c。");
                }
            }

            if (victim.IsBoss)
            {
                _credits += BossEliminationBonusCredits;
                PushActivityFeed($"敵ボス {victim.Name} を撃破。+{BossEliminationBonusCredits}c を獲得。");
                AwardUltPoints(attacker.Name, 2, "敵ボス撃破");
            }

            _eventBus.Emit(new ActorDeathEvent(victim.Name, victim.Type, victim.Position, attacker.Name));
            return;
        }

        if (victim.Type == ActorType.Player)
        {
            _matchPlayerDeaths++;
        }

        if (victim.Type != ActorType.Enemy)
        {
            AwardUltPoints(victim.Name, 1, "デス");
        }

        PushActivityFeed($"{attacker.Name} が {victim.Name} を撃破。");
        _eventBus.Emit(new ActorDeathEvent(victim.Name, victim.Type, victim.Position, attacker.Name));

        // 敵ボスが味方を倒した場合の通知
        if (attacker.IsBoss && attacker.Type == ActorType.Enemy && victim.Type != ActorType.Enemy)
        {
            PushActivityFeed($"敵ボス {attacker.Name} がキルを取得。敵側へ生存配当が発生。");
        }

        // 味方ボスが倒された場合の通知
        if (victim.IsBoss && victim.Type != ActorType.Enemy)
        {
            PushActivityFeed($"味方ボス {victim.Name} が撃破されました。投資は没収され、敵に報酬が渡ります。");
        }
    }
}
