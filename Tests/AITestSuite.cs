using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace RYZECHo.Tests;

/// <summary>
/// AI ロジックのテストスイート。ターゲット選択・行動・ゴール・視認をテスト。
/// </summary>
internal sealed class AITestSuite
{
    private readonly TestRunner _runner;
    private readonly Random _random = new(42);

    public AITestSuite(TestRunner runner) => _runner = runner;

    public void RunAll()
    {
        _runner.StartRun();
        RunTargetingTests();
        RunMovementTests();
        RunAbilityTests();
        RunGoalTests();
        RunVisionTests();
        _runner.StopRun();
    }

    private void RunTargetingTests()
    {
        _runner.RunTest("PickBestTarget returns null when no candidates", () =>
        {
            var r = SimPickBestTarget(new PointF(100, 100), 50, new List<Actor>(), ActorType.Enemy);
            if (r is not null) throw new Exception("Expected null");
        }, "Targeting");

        _runner.RunTest("PickBestTarget returns closest valid target", () =>
        {
            var o = new PointF(200, 200);
            var ts = new List<Actor> { CreateA(o, 50f), CreateA(o, 100f), CreateA(o, 200f) };
            var r = SimPickBestTarget(o, 150f, ts, ActorType.Enemy);
            if (r is null) throw new Exception("Expected target");
            if (Dist(o, r.Position) > 150f) throw new Exception("Out of range");
        }, "Targeting");

        _runner.RunTest("PickEnemyTarget returns null with no defenders", () =>
        {
            var e = CreateA(new PointF(200, 200), 10f, ActorType.Enemy);
            var r = SimPickEnemyTarget(e, new List<Actor>());
            if (r is not null) throw new Exception("Expected null");
        }, "Targeting");

        _runner.RunTest("PickRaycastTarget returns null for zero-length direction", () =>
        {
            var o = new PointF(100, 100);
            var r = SimPickRaycastTarget(o, o, 100f);
            if (r is not null) throw new Exception("Expected null");
        }, "Targeting");
    }

    private void RunMovementTests()
    {
        _runner.RunTest("ResolveCollision clamps within world bounds", () =>
        {
            var r = SimResolveCollision(new PointF(-50, -50), 14f, new HashSet<Point>(), new List<Structure>());
            var min = GameLayout.WorldMargin + 16f;
            if (r.X < min || r.Y < min) throw new Exception($"Out of bounds: ({r.X},{r.Y})");
        }, "Movement");

        _runner.RunTest("ResolveCollision avoids structure cells", () =>
        {
            var s = new List<Structure> { new() { Kind = StructureKind.BlastDoor, Cell = new(5, 5), Health = 100f, APCost = 0, Label = "t" } };
            var r = SimResolveCollision(new PointF(320, 320), 14f, new HashSet<Point>(), s);
            var c = new Point((int)(r.X / GameLayout.CellSize), (int)(r.Y / GameLayout.CellSize));
            if (c.X == 5 && c.Y == 5) throw new Exception("Not resolved");
        }, "Movement");

        _runner.RunTest("DashTimer decreases over time", () =>
        {
            var a = CreateA(new PointF(100, 100), 10f);
            a.DashTimer = 0.65f;
            SimAbilityState(a, 0.3f);
            if (a.DashTimer <= 0f) throw new Exception("Should be positive");
            if (a.DashTimer >= 0.65f) throw new Exception("Did not decrease");
        }, "Movement");
    }

    private void RunAbilityTests()
    {
        _runner.RunTest("Nitro SkillOne when target far", () =>
        {
            var a = CreateA(new PointF(100, 100), 10f, ActorType.Enemy);
            a.Agent = AgentKind.Nitro;
            var t = CreateA(new PointF(300, 300), 10f);
            if (!SimSkillOne(a, t, Dist(a.Position, t.Position))) throw new Exception("Should use");
        }, "AbilityDecision");

        _runner.RunTest("Nitro SkillOne when no target", () =>
        {
            var a = CreateA(new PointF(100, 100), 10f, ActorType.Enemy);
            a.Agent = AgentKind.Nitro;
            if (!SimSkillOne(a, null, float.MaxValue)) throw new Exception("Should use");
        }, "AbilityDecision");

        _runner.RunTest("Oasis SkillTwo when low health", () =>
        {
            var a = CreateA(new PointF(100, 100), 10f);
            a.Agent = AgentKind.Oasis;
            a.Health = a.MaxHealth * 0.6f;
            var t = CreateA(new PointF(200, 200), 10f);
            if (!SimSkillTwo(a, t, Dist(a.Position, t.Position))) throw new Exception("Should use");
        }, "AbilityDecision");

        _runner.RunTest("UltimateCharge increases over time", () =>
        {
            var a = CreateA(new PointF(100, 100), 10f, ActorType.Enemy);
            a.UltimateCharge = 0f;
            SimAbilityState(a, 1f);
            if (a.UltimateCharge <= 0f) throw new Exception("Should increase");
            if (a.UltimateCharge > GameRules.MaxUltPoints) throw new Exception("Exceeds max");
        }, "AbilityDecision");

        _runner.RunTest("HealingTimer heals correctly", () =>
        {
            var a = CreateA(new PointF(100, 100), 10f);
            a.Health = 50f;
            a.HealingTimer = 2f;
            SimAbilityState(a, 1f);
            if (a.Health <= 50f) throw new Exception("Health should increase");
            if (a.Health > a.MaxHealth) throw new Exception("Exceeds max");
        }, "AbilityDecision");
    }

    private void RunGoalTests()
    {
        _runner.RunTest("PickPathGoal returns valid cell", () =>
        {
            var e = CreateA(new PointF(500, 500), 10f, ActorType.Enemy);
            var r = SimPickPathGoal(e);
            if (r == default) throw new Exception("Invalid goal");
        }, "GoalPicking");

        _runner.RunTest("PickAllyAttackGoal within grid bounds", () =>
        {
            var a = CreateA(new PointF(200, 200), 10f, ActorType.Ally);
            var r = SimPickAllyGoal(a);
            if (r.X < 1 || r.X >= GameLayout.GridColumns - 1 || r.Y < 1 || r.Y >= GameLayout.GridRows - 1)
                throw new Exception($"Invalid: ({r.X},{r.Y})");
        }, "GoalPicking");
    }

    private void RunVisionTests()
    {
        _runner.RunTest("HasLineOfSight blocked by wall", () =>
        {
            var w = new HashSet<Point> { new(10, 10), new(11, 11) };
            if (SimLOS(new PointF(100, 100), new PointF(500, 500), w))
                throw new Exception("Should be blocked");
        }, "Vision");

        _runner.RunTest("HasLineOfSight clear without walls", () =>
        {
            if (!SimLOS(new PointF(100, 100), new PointF(200, 200), new HashSet<Point>()))
                throw new Exception("Should be clear");
        }, "Vision");

        _runner.RunTest("DirectSightTo respects range", () =>
        {
            var a = CreateA(new PointF(100, 100), 10f);
            a.Weapon = WeaponType.Giant;
            var ws = new Dictionary<WeaponType, WeaponStats> { [WeaponType.Giant] = new() { VisionRange = 400f, ScopedFov = false } };
            if (SimSight(a, new PointF(1000, 1000), ws)) throw new Exception("Beyond range");
        }, "Vision");
    }

    // --- Helpers ---
    private Actor CreateA(PointF pos, float radius, ActorType type = ActorType.Player) => new()
    {
        Name = type.ToString(), Agent = AgentKind.Veil, Type = type, HomeCell = new(13, 6),
        Weapon = WeaponType.Giant, Position = pos, Radius = radius,
        MaxHealth = 100f, MaxShield = 60f, Health = 100f, Shield = 60f,
        HearingRange = 350f, BaseMoveSpeed = 210f,
    };

    private static float Dist(PointF a, PointF b) => MathF.Sqrt((a.X - b.X) ** 2 + (a.Y - b.Y) ** 2);

    private static Actor? SimPickBestTarget(PointF o, float r, List<Actor> t, ActorType _) =>
        t.Where(a => Dist(o, a.Position) <= r).OrderBy(a => Dist(o, a.Position)).FirstOrDefault();

    private static Actor? SimPickEnemyTarget(Actor e, List<Actor> d) =>
        d.Where(a => Dist(e.Position, a.Position) <= 630f).OrderBy(a => Dist(e.Position, a.Position)).FirstOrDefault();

    private static Actor? SimPickRaycastTarget(PointF o, PointF t, float _) => Dist(o, t) <= 1f ? null : (Actor?)null;

    private static void SimAbilityState(Actor a, float d)
    {
        a.SkillOneCooldown = MathF.Max(0f, a.SkillOneCooldown - d);
        a.SkillTwoCooldown = MathF.Max(0f, a.SkillTwoCooldown - d);
        a.DashTimer = MathF.Max(0f, a.DashTimer - d);
        a.OverdriveTimer = MathF.Max(0f, a.OverdriveTimer - d);
        a.GhostTimer = MathF.Max(0f, a.GhostTimer - d);
        if (a.HealingTimer > 0f && a.IsAlive) { a.Health = MathF.Min(a.MaxHealth, a.Health + 7f * d); a.HealingTimer = MathF.Max(0f, a.HealingTimer - d); }
        a.UltimateCharge = Math.Clamp(a.UltimateCharge + (a.IsBoss ? 0.065f : 0.045f) * d, 0f, GameRules.MaxUltPoints);
    }

    private static bool SimSkillOne(Actor a, Actor? t, float d) => a.Agent switch
    { AgentKind.Nitro => t is null || d > 150f, AgentKind.Oasis => t is not null && d <= 220f, AgentKind.Divide => t is not null && d <= 170f, AgentKind.Glitch => t is not null && d <= 260f, _ => t is not null && d <= 230f, };

    private static bool SimSkillTwo(Actor a, Actor? t, float d) => a.Agent switch
    { AgentKind.Veil => t is not null && d <= 180f, AgentKind.Vine => t is not null && d <= 190f, AgentKind.Nitro => t is not null && d <= 112f, AgentKind.Oasis => a.Health <= a.MaxHealth * 0.72f, AgentKind.Divide => t is null || d <= 220f, AgentKind.Glitch => t is not null && d <= 190f, _ => false, };

    private static Point SimPickPathGoal(Actor e) => e.IsBoss ? e.HomeCell : new(GameLayout.GridColumns / 2, GameLayout.GridRows / 2);
    private static Point SimPickAllyGoal(Actor _) => new(GameLayout.GridColumns / 2 + 3, GameLayout.GridRows / 2);

    private static PointF SimResolveCollision(PointF d, float r, HashSet<Point> w, List<Structure> _)
    {
        var c = new PointF(Math.Clamp(d.X, GameLayout.WorldMargin + r + 2f, GameLayout.DefaultClientWidth - 2 * GameLayout.WorldMargin - r - 2f), Math.Clamp(d.Y, GameLayout.WorldMargin + r + 2f, GameLayout.DefaultClientHeight - 2 * GameLayout.WorldMargin - r - 2f));
        foreach (var cell in w)
        {
            var rect = new RectangleF(cell.X * GameLayout.CellSize, cell.Y * GameLayout.CellSize, GameLayout.CellSize, GameLayout.CellSize);
            if (!RectangleF.Inflate(rect, r, r).Contains(c)) continue;
            var center = new PointF(cell.X * GameLayout.CellSize + GameLayout.CellSize / 2f, cell.Y * GameLayout.CellSize + GameLayout.CellSize / 2f);
            var push = new PointF(c.X - center.X, c.Y - center.Y);
            var len = MathF.Max(1f, MathF.Sqrt(push.X * push.X + push.Y * push.Y));
            c = new PointF(center.X + push.X / len * (GameLayout.CellSize / 2f + r + 2f), center.Y + push.Y / len * (GameLayout.CellSize / 2f + r + 2f));
        }
        return c;
    }

    private static bool SimLOS(PointF s, PointF e, HashSet<Point> w)
    {
        var dist = Dist(s, e);
        var steps = Math.Max(2, (int)(dist / 8f));
        for (var i = 1; i < steps; i++)
        {
            var p = i / (float)steps;
            var cell = new Point((int)((s.X + (e.X - s.X) * p) / GameLayout.CellSize), (int)((s.Y + (e.Y - s.Y) * p) / GameLayout.CellSize));
            if (w.Contains(cell)) return false;
        }
        return true;
    }

    private static bool SimSight(Actor a, PointF pos, Dictionary<WeaponType, WeaponStats> ws)
    {
        var v = new PointF(pos.X - a.Position.X, pos.Y - a.Position.Y);
        return MathF.Sqrt(v.X * v.X + v.Y * v.Y) <= ws[a.Weapon].VisionRange;
    }
}
