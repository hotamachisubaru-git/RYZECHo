namespace RYZECHo;

internal readonly record struct MapEditPlacementDecision(
    bool Allowed,
    string? Reason);

internal readonly record struct MapEditApTransaction(
    bool Accepted,
    int BeforeAp,
    int DeltaAp,
    int AfterAp,
    string? Reason);

internal static class MapEditApRules
{
    public const float TrapDeadZoneRadiusCells = 2.5f;
    private const int MaxBlastDoorClusterSize = 2;

    public static int ToolApCost(BuildToolKind tool)
    {
        return tool switch
        {
            BuildToolKind.BlastDoor => 2,
            BuildToolKind.HoneyTrap => 3,
            BuildToolKind.StaticNest => 4,
            BuildToolKind.ReconBeacon => 4,
            BuildToolKind.ShieldRelay => 5,
            BuildToolKind.PortableCover => 3,
            BuildToolKind.VisorWall => 4,
            _ => 2,
        };
    }

    public static MapEditPlacementDecision ValidatePlacement(
        Structure candidate,
        IReadOnlyCollection<Structure> existingStructures,
        IReadOnlyCollection<Point> buildSlots,
        IReadOnlyCollection<Point> noBuildZones)
    {
        if (!buildSlots.Contains(candidate.Cell))
        {
            return new MapEditPlacementDecision(false, "そのセルは設置可能スロットではありません。");
        }

        if (noBuildZones.Contains(candidate.Cell))
        {
            return new MapEditPlacementDecision(false, "そのセルはノー・ビルド・ゾーンです。");
        }

        if (existingStructures.Any(structure => structure.Cell == candidate.Cell))
        {
            return new MapEditPlacementDecision(false, "そのセルには既に設置物があります。");
        }

        if (ViolatesDeadZone(candidate, existingStructures))
        {
            return new MapEditPlacementDecision(false, "同カテゴリのトラップが近すぎます。デッドゾーンを空けてください。");
        }

        if (candidate.Kind == StructureKind.BlastDoor && WouldExceedBlastDoorClusterLimit(candidate.Cell, existingStructures))
        {
            return new MapEditPlacementDecision(false, "強化扉は 2 連結までです。");
        }

        return new MapEditPlacementDecision(true, null);
    }

    public static MapEditApTransaction TrySpend(int currentAp, int cost, int maxAp)
    {
        var before = ClampAp(currentAp, maxAp);
        var spend = Math.Max(0, cost);
        if (spend > before)
        {
            return new MapEditApTransaction(false, before, 0, before, $"AP が不足しています。必要 {spend}AP / 残り {before}AP。");
        }

        return new MapEditApTransaction(true, before, -spend, before - spend, null);
    }

    public static MapEditApTransaction RefundForRemoval(int currentAp, Structure structure, int maxAp)
    {
        var before = ClampAp(currentAp, maxAp);
        if (!CanRefundOnRemoval(structure))
        {
            return new MapEditApTransaction(true, before, 0, before, "破壊済みまたは一時設置物のため AP 返還なし。");
        }

        var refund = Math.Min(Math.Max(0, structure.APCost), Math.Max(0, maxAp - before));
        if (refund <= 0)
        {
            return new MapEditApTransaction(true, before, 0, before, "AP が上限のため返還なし。");
        }

        return new MapEditApTransaction(true, before, refund, before + refund, null);
    }

    public static MapEditApTransaction RefillForEditPhase(int currentAp, int targetAp, int maxAp)
    {
        var before = ClampAp(currentAp, maxAp);
        var after = Math.Clamp(Math.Max(before, targetAp), 0, maxAp);
        return new MapEditApTransaction(true, before, after - before, after, null);
    }

    public static bool IsNoBuildCell(Point cell, IReadOnlyCollection<Point> noBuildZones)
    {
        return noBuildZones.Contains(cell);
    }

    private static bool CanRefundOnRemoval(Structure structure)
    {
        return structure.RemainingLifetime <= 0f && structure.Health > 0.01f;
    }

    private static bool ViolatesDeadZone(Structure candidate, IEnumerable<Structure> existingStructures)
    {
        if (!UsesTrapDeadZone(candidate.Kind))
        {
            return false;
        }

        return existingStructures
            .Where(structure => UsesTrapDeadZone(structure.Kind))
            .Any(structure => CellDistance(structure.Cell, candidate.Cell) < TrapDeadZoneRadiusCells);
    }

    private static bool UsesTrapDeadZone(StructureKind kind)
    {
        return kind is StructureKind.HoneyTrap or StructureKind.StaticNest or StructureKind.ReconBeacon or StructureKind.HoloDecoy;
    }

    private static bool WouldExceedBlastDoorClusterLimit(Point newDoorCell, IEnumerable<Structure> existingStructures)
    {
        var doorCells = existingStructures
            .Where(structure => structure.Kind == StructureKind.BlastDoor && structure.Health > 0f)
            .Select(structure => structure.Cell)
            .ToHashSet();
        doorCells.Add(newDoorCell);

        var frontier = new Queue<Point>();
        frontier.Enqueue(newDoorCell);
        var visited = new HashSet<Point> { newDoorCell };

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var neighbor in OrthogonalNeighbors(current))
            {
                if (!doorCells.Contains(neighbor) || !visited.Add(neighbor))
                {
                    continue;
                }

                frontier.Enqueue(neighbor);
            }
        }

        return visited.Count > MaxBlastDoorClusterSize;
    }

    private static IEnumerable<Point> OrthogonalNeighbors(Point cell)
    {
        yield return new Point(cell.X + 1, cell.Y);
        yield return new Point(cell.X - 1, cell.Y);
        yield return new Point(cell.X, cell.Y + 1);
        yield return new Point(cell.X, cell.Y - 1);
    }

    private static float CellDistance(Point left, Point right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    private static int ClampAp(int currentAp, int maxAp)
    {
        return Math.Clamp(currentAp, 0, Math.Max(0, maxAp));
    }
}
