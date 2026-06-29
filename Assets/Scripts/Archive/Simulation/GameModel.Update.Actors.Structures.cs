namespace RYZECHo;

internal sealed partial class GameModel
{
    private void UpdateStructures(float deltaSeconds)
    {
        for (var index = _structures.Count - 1; index >= 0; index--)
        {
            var structure = _structures[index];
            if (structure.RemainingLifetime <= 0f)
            {
                continue;
            }

            structure.RemainingLifetime -= deltaSeconds;
            if (structure.RemainingLifetime <= 0f)
            {
                _structures.RemoveAt(index);
            }
        }

        foreach (var structure in _structures)
        {
            if (structure.Kind is not StructureKind.StaticNest and not StructureKind.ReconBeacon and not StructureKind.ShieldRelay and not StructureKind.VisorWall and not StructureKind.HoloDecoy)
            {
                continue;
            }

            structure.PulseCooldown -= deltaSeconds;
            if (structure.PulseCooldown <= 0f)
            {
                switch (structure.Kind)
                {
                    case StructureKind.StaticNest:
                        structure.PulseCooldown = 1.05f;
                        EmitRipple(CellCenter(structure.Cell), 0.72f, RippleKind.Skill, Color.FromArgb(236, 212, 98));
                        break;
                    case StructureKind.ReconBeacon:
                        structure.PulseCooldown = 1.2f;
                        EmitRipple(CellCenter(structure.Cell), 0.82f, RippleKind.Skill, Color.FromArgb(124, 228, 255));
                        foreach (var enemy in _enemies.Where(actor => actor.IsAlive && Distance(actor.Position, CellCenter(structure.Cell)) <= 150f))
                        {
                            RevealEnemyToTeam(enemy, SharedVisionDurationSeconds + 0.8f);
                        }

                        break;
                    case StructureKind.ShieldRelay:
                        structure.PulseCooldown = 1.5f;
                        EmitRipple(CellCenter(structure.Cell), 0.68f, RippleKind.Skill, Color.FromArgb(124, 255, 204));
                        foreach (var ally in LivePlayerTeam().Where(actor => Distance(actor.Position, CellCenter(structure.Cell)) <= 130f))
                        {
                            ally.Shield = MathF.Min(ally.MaxShield, ally.Shield + 6f);
                        }

                        break;
                    case StructureKind.VisorWall:
                        structure.PulseCooldown = 1.8f;
                        EmitRipple(CellCenter(structure.Cell), 0.42f, RippleKind.Skill, Color.FromArgb(150, 150, 228, 255));
                        break;
                    case StructureKind.HoloDecoy:
                        structure.PulseCooldown = 1.15f;
                        EmitRipple(CellCenter(structure.Cell), 0.94f, _random.Next(0, 2) == 0 ? RippleKind.Footstep : RippleKind.Gunshot, Color.FromArgb(226, 196, 132, 255));
                        break;
                }
            }
        }

        foreach (var door in _structures.Where(structure => IsRouteBlockingStructure(structure.Kind)).ToList())
        {
            var doorCenter = CellCenter(door.Cell);

            foreach (var enemy in _enemies.Where(actor => actor.IsAlive && Distance(actor.Position, doorCenter) <= 30f))
            {
                door.Health = MathF.Max(0f, door.Health - (StructureBreakDamagePerSecond(door.Kind) * deltaSeconds));
                enemy.Path.Clear();
                EmitRipple(doorCenter, 0.68f, RippleKind.Skill, Color.FromArgb(245, 198, 92));
            }

            if (door.Health <= 0f)
            {
                _structures.Remove(door);
            }
        }
    }

    private static float StructureBreakDamagePerSecond(StructureKind kind)
    {
        return kind switch
        {
            StructureKind.PortableCover => 24f,
            StructureKind.VisorWall => 22f,
            _ => 17f,
        };
    }

    private void UpdateRipples(float deltaSeconds)
    {
        for (var index = _ripples.Count - 1; index >= 0; index--)
        {
            _ripples[index].Age += deltaSeconds;
            if (_ripples[index].Age >= _ripples[index].Lifetime)
            {
                _ripples.RemoveAt(index);
            }
        }
    }
}
