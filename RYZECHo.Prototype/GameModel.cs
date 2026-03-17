using System.Drawing.Drawing2D;

namespace RYZECHo.Prototype;

public readonly record struct InputSnapshot(
    bool MoveUp,
    bool MoveLeft,
    bool MoveDown,
    bool MoveRight,
    bool AdjustBetLeft,
    bool AdjustBetRight,
    bool Confirm,
    bool Press1,
    bool Press2,
    bool Press3,
    bool PressQ,
    bool PressE,
    bool PressR,
    bool FireHeld,
    Point MousePosition);

internal enum GamePhase
{
    Construct,
    Bet,
    Hunt,
    RoundResult,
    Victory,
    Defeat,
}

internal enum BuildToolKind
{
    BlastDoor,
    HoneyTrap,
    StaticNest,
}

internal enum WeaponType
{
    SMG,
    Rifle,
    Sniper,
}

internal enum ActorType
{
    Player,
    Ally,
    Enemy,
}

internal enum StructureKind
{
    BlastDoor,
    HoneyTrap,
    StaticNest,
}

internal sealed class WeaponStats
{
    public required WeaponType Type { get; init; }
    public required string Label { get; init; }
    public required int Cost { get; init; }
    public required float VisionRange { get; init; }
    public required float HearingMultiplier { get; init; }
    public required float FireCooldown { get; init; }
    public required float Damage { get; init; }
    public required float MoveSpeed { get; init; }
    public required float ProjectileRange { get; init; }
}

internal sealed class Structure
{
    public required StructureKind Kind { get; init; }
    public required Point Cell { get; init; }
    public required int APCost { get; init; }
    public required string Label { get; init; }
    public float Health { get; set; }
    public float MaxHealth { get; init; }
    public float PulseCooldown { get; set; }
}

internal sealed class Ripple
{
    public required PointF Position { get; init; }
    public required float Strength { get; init; }
    public required float Lifetime { get; init; }
    public required Color Color { get; init; }
    public float Age { get; set; }
}

internal sealed class Actor
{
    public required string Name { get; init; }
    public required ActorType Type { get; init; }
    public required Point HomeCell { get; init; }
    public required WeaponType Weapon { get; set; }
    public required PointF Position { get; set; }
    public required float Radius { get; init; }
    public required float MaxHealth { get; init; }
    public required float HearingRange { get; init; }
    public required float BaseMoveSpeed { get; init; }
    public float Health { get; set; }
    public float FireCooldown { get; set; }
    public float PathCooldown { get; set; }
    public float FootstepCooldown { get; set; }
    public float FacingAngle { get; set; }
    public bool IsBoss { get; set; }
    public Queue<PointF> Path { get; } = new();
    public bool IsAlive => Health > 0.01f;
}

internal sealed class GameModel
{
    private const string UiFontFamily = "Yu Gothic UI";
    private const int DefaultClientWidth = 1440;
    private const int DefaultClientHeight = 960;
    private const int WorldMargin = 24;
    private const int TopBarHeight = 52;
    private const int SidePanelGap = 20;
    private const int SidePanelWidth = 364;
    private const int BottomHudHeight = 208;
    private const int GridColumns = 18;
    private const int GridRows = 12;
    private const int CellSize = 56;
    private const int TotalRounds = 3;
    private const float FovDegrees = 120f;
    private const float WorldPerspectiveScaleX = 0.84f;
    private const float WorldPerspectiveScaleY = 0.78f;
    private const float WorldPerspectiveShearX = 0.22f;
    private const float WorldPerspectiveTopInset = 10f;

    private readonly Random _random = new();
    private readonly Dictionary<WeaponType, WeaponStats> _weaponStats = CreateWeaponStats();
    private readonly List<Structure> _structures = [];
    private readonly List<Ripple> _ripples = [];
    private readonly List<Actor> _allies = [];
    private readonly List<Actor> _enemies = [];
    private readonly HashSet<Point> _permanentWalls = [];
    private readonly HashSet<Point> _buildSlots = [];
    private readonly List<Point> _spawnCells = [];
    private Size _layoutSize = new(DefaultClientWidth, DefaultClientHeight);

    private GamePhase _phase = GamePhase.Construct;
    private BuildToolKind _selectedBuildTool = BuildToolKind.BlastDoor;
    private WeaponType _selectedWeapon = WeaponType.Rifle;
    private int _buildPoints = 12;
    private int _credits = 425;
    private int _currentRound = 1;
    private int _selectedBet = 100;
    private int _pendingEnemies;
    private float _spawnCooldown;
    private float _roundTimer;
    private float _pingCooldown;
    private float _resultTimer;
    private float _coreHealth;
    private GamePhase _resultDestination = GamePhase.Bet;
    private string _selectedBossName = "あなた";
    private string _resultMessage = "最初の構築が、全ラウンドを支配する。";
    private bool _showBriefing = true;

    private readonly Actor _player;

    public GameModel()
    {
        BuildMapGeometry();

        _player = new Actor
        {
            Name = "あなた",
            Type = ActorType.Player,
            HomeCell = new Point(13, 6),
            Weapon = WeaponType.Rifle,
            Position = CellCenter(new Point(13, 6)),
            Radius = 14f,
            MaxHealth = 100f,
            Health = 100f,
            HearingRange = 350f,
            BaseMoveSpeed = 210f,
        };

        _allies.Add(new Actor
        {
            Name = "北アンカー",
            Type = ActorType.Ally,
            HomeCell = new Point(13, 4),
            Weapon = WeaponType.Sniper,
            Position = CellCenter(new Point(13, 4)),
            Radius = 13f,
            MaxHealth = 95f,
            Health = 95f,
            HearingRange = 300f,
            BaseMoveSpeed = 0f,
        });

        _allies.Add(new Actor
        {
            Name = "南アンカー",
            Type = ActorType.Ally,
            HomeCell = new Point(13, 8),
            Weapon = WeaponType.SMG,
            Position = CellCenter(new Point(13, 8)),
            Radius = 13f,
            MaxHealth = 95f,
            Health = 95f,
            HearingRange = 420f,
            BaseMoveSpeed = 0f,
        });

        ResetCampaign();
    }

    private Rectangle WorldBounds => new((((_layoutSize.Width - (GridColumns * CellSize)) / 2) - 20), 88, GridColumns * CellSize, GridRows * CellSize);

    private Rectangle TopBarBounds => new((_layoutSize.Width - 560) / 2, 16, 560, TopBarHeight);

    private Rectangle BottomHudBounds => new((_layoutSize.Width - 900) / 2, (int)MathF.Round(WorldVisualBounds.Bottom) + 42, 900, BottomHudHeight);

    private Rectangle SidePanelBounds => new(_layoutSize.Width - WorldMargin - SidePanelWidth, WorldBounds.Top, SidePanelWidth, WorldBounds.Height);

    private Rectangle RosterBounds => new(Math.Max(WorldMargin, WorldBounds.Left - 172), WorldBounds.Top + 10, 168, 166);

    private Rectangle IntelBounds => new(
        Math.Min(_layoutSize.Width - WorldMargin - 210, (int)MathF.Round(WorldVisualBounds.Right) + SidePanelGap),
        WorldBounds.Top + 10,
        210,
        104);

    private Rectangle MinimapBounds => new(_layoutSize.Width - WorldMargin - 222, BottomHudBounds.Top - 8, 222, 222);

    private RectangleF WorldVisualBounds => new(
        WorldBounds.Left + ((WorldBounds.Width - ((WorldBounds.Width * WorldPerspectiveScaleX) + (WorldBounds.Height * WorldPerspectiveShearX))) / 2f),
        WorldBounds.Top + WorldPerspectiveTopInset,
        (WorldBounds.Width * WorldPerspectiveScaleX) + (WorldBounds.Height * WorldPerspectiveShearX),
        WorldBounds.Height * WorldPerspectiveScaleY);

    public void CycleBuildTool()
    {
        _selectedBuildTool = _selectedBuildTool switch
        {
            BuildToolKind.BlastDoor => BuildToolKind.HoneyTrap,
            BuildToolKind.HoneyTrap => BuildToolKind.StaticNest,
            _ => BuildToolKind.BlastDoor,
        };
    }

    public void ToggleBriefing()
    {
        _showBriefing = !_showBriefing;
    }

    public void HandleLeftClick(Point location)
    {
        if (_phase == GamePhase.Construct)
        {
            TryPlaceStructure(location);
        }
    }

    public void HandleRightClick(Point location)
    {
        if (_phase == GamePhase.Construct)
        {
            TryRemoveStructure(location);
        }
    }

    public void Update(float deltaSeconds, InputSnapshot input)
    {
        UpdateRipples(deltaSeconds);

        switch (_phase)
        {
            case GamePhase.Construct:
                UpdateConstructPhase(input);
                break;
            case GamePhase.Bet:
                UpdateBetPhase(input);
                break;
            case GamePhase.Hunt:
                UpdateHuntPhase(deltaSeconds, input);
                break;
            case GamePhase.RoundResult:
                UpdateRoundResult(deltaSeconds, input);
                break;
            case GamePhase.Victory:
            case GamePhase.Defeat:
                UpdateEndState(input);
                break;
        }
    }

    public void UpdateConstructPhase(InputSnapshot input)
    {
        if (input.Press1)
        {
            _selectedBuildTool = BuildToolKind.BlastDoor;
        }
        else if (input.Press2)
        {
            _selectedBuildTool = BuildToolKind.HoneyTrap;
        }
        else if (input.Press3)
        {
            _selectedBuildTool = BuildToolKind.StaticNest;
        }

        if (input.Confirm)
        {
            BeginBetPhase();
        }
    }

    public void UpdateBetPhase(InputSnapshot input)
    {
        if (input.Press1)
        {
            _selectedBossName = "あなた";
        }
        else if (input.Press2)
        {
            _selectedBossName = "北アンカー";
        }
        else if (input.Press3)
        {
            _selectedBossName = "南アンカー";
        }

        if (input.PressQ)
        {
            CycleWeapon(-1);
        }
        else if (input.PressE)
        {
            CycleWeapon(1);
        }

        if (input.AdjustBetLeft)
        {
            _selectedBet = Math.Max(25, _selectedBet - 25);
        }
        else if (input.AdjustBetRight)
        {
            _selectedBet = Math.Min(Math.Max(25, AffordableCredits()), _selectedBet + 25);
        }

        _selectedBet = Math.Min(_selectedBet, Math.Max(25, AffordableCredits()));

        if (input.Confirm)
        {
            StartRound();
        }
    }

    private void UpdateHuntPhase(float deltaSeconds, InputSnapshot input)
    {
        _roundTimer -= deltaSeconds;
        _spawnCooldown -= deltaSeconds;
        _pingCooldown -= deltaSeconds;

        RestoreBossFlags();
        UpdatePlayer(deltaSeconds, input);
        UpdateAllies(deltaSeconds);
        UpdateEnemies(deltaSeconds);
        UpdateStructures(deltaSeconds);
        SpawnEnemiesIfNeeded();

        if (_coreHealth <= 0f || (!_player.IsAlive && _allies.All(actor => !actor.IsAlive)))
        {
            EndRound(false);
            return;
        }

        if ((_pendingEnemies <= 0 && _enemies.All(enemy => !enemy.IsAlive)) || _roundTimer <= 0f)
        {
            EndRound(true);
        }
    }

    private void UpdateRoundResult(float deltaSeconds, InputSnapshot input)
    {
        _resultTimer -= deltaSeconds;

        if (input.Confirm || _resultTimer <= 0f)
        {
            if (_resultDestination == GamePhase.Bet)
            {
                BeginBetPhase();
            }
            else
            {
                _phase = _resultDestination;
            }
        }
    }

    private void UpdateEndState(InputSnapshot input)
    {
        if (input.PressR)
        {
            ResetCampaign();
        }
    }

    private void UpdatePlayer(float deltaSeconds, InputSnapshot input)
    {
        if (!_player.IsAlive)
        {
            return;
        }

        var weapon = _weaponStats[_player.Weapon];
        var worldMousePosition = ScreenToWorldPoint(input.MousePosition);
        var movement = PointF.Empty;

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

        if (movement != PointF.Empty)
        {
            var length = MathF.Sqrt((movement.X * movement.X) + (movement.Y * movement.Y));
            movement = new PointF(movement.X / length, movement.Y / length);

            var next = new PointF(
                _player.Position.X + (movement.X * weapon.MoveSpeed * deltaSeconds),
                _player.Position.Y + (movement.Y * weapon.MoveSpeed * deltaSeconds));

            _player.Position = ResolveCollision(next, _player.Radius);
        }

        _player.FireCooldown = MathF.Max(0f, _player.FireCooldown - deltaSeconds);

        var aimVector = new PointF(worldMousePosition.X - _player.Position.X, worldMousePosition.Y - _player.Position.Y);
        if (Math.Abs(aimVector.X) > 0.01f || Math.Abs(aimVector.Y) > 0.01f)
        {
            _player.FacingAngle = MathF.Atan2(aimVector.Y, aimVector.X);
        }

        if (input.FireHeld && _player.FireCooldown <= 0f)
        {
            var target = PickRaycastTarget(_player.Position, worldMousePosition, weapon.ProjectileRange);
            if (target is not null)
            {
                target.Health = MathF.Max(0f, target.Health - weapon.Damage);
                EmitRipple(target.Position, 0.95f, Color.FromArgb(235, 85, 80));
            }

            _player.FireCooldown = weapon.FireCooldown;
            EmitRipple(_player.Position, 0.55f, Color.FromArgb(80, 215, 240));
        }
    }

    private void UpdateAllies(float deltaSeconds)
    {
        foreach (var ally in _allies)
        {
            if (!ally.IsAlive)
            {
                continue;
            }

            ally.FireCooldown = MathF.Max(0f, ally.FireCooldown - deltaSeconds);
            var weapon = _weaponStats[ally.Weapon];
            var target = PickBestTarget(ally.Position, weapon.VisionRange, ActorType.Ally);

            if (target is null)
            {
                continue;
            }

            ally.FacingAngle = MathF.Atan2(target.Position.Y - ally.Position.Y, target.Position.X - ally.Position.X);

            if (ally.FireCooldown <= 0f)
            {
                target.Health = MathF.Max(0f, target.Health - weapon.Damage);
                ally.FireCooldown = weapon.FireCooldown;
                EmitRipple(ally.Position, 0.6f, Color.FromArgb(60, 190, 230));
            }

            if (_pingCooldown <= 0f)
            {
                EmitRipple(target.Position, 0.72f, Color.FromArgb(65, 225, 185));
                _pingCooldown = 0.7f;
            }
        }
    }

    private void UpdateEnemies(float deltaSeconds)
    {
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            var weapon = _weaponStats[enemy.Weapon];
            enemy.FireCooldown = MathF.Max(0f, enemy.FireCooldown - deltaSeconds);
            enemy.PathCooldown -= deltaSeconds;
            enemy.FootstepCooldown -= deltaSeconds;

            var target = PickEnemyTarget(enemy);

            if (target is not null && Distance(enemy.Position, target.Position) <= weapon.ProjectileRange && HasLineOfSight(enemy.Position, target.Position))
            {
                enemy.FacingAngle = MathF.Atan2(target.Position.Y - enemy.Position.Y, target.Position.X - enemy.Position.X);
                if (enemy.FireCooldown <= 0f)
                {
                    target.Health = MathF.Max(0f, target.Health - weapon.Damage);
                    enemy.FireCooldown = weapon.FireCooldown * 1.2f;
                    EmitRipple(enemy.Position, 0.72f, Color.FromArgb(255, 120, 85));
                }
            }
            else if (enemy.PathCooldown <= 0f)
            {
                RebuildEnemyPath(enemy);
            }

            FollowPath(enemy, deltaSeconds);

            if (Distance(enemy.Position, CorePosition()) <= 34f)
            {
                _coreHealth = MathF.Max(0f, _coreHealth - (15f * deltaSeconds));
            }
        }
    }

    private void UpdateStructures(float deltaSeconds)
    {
        foreach (var structure in _structures)
        {
            if (structure.Kind != StructureKind.StaticNest)
            {
                continue;
            }

            structure.PulseCooldown -= deltaSeconds;
            if (structure.PulseCooldown <= 0f)
            {
                structure.PulseCooldown = 1.05f;
                EmitRipple(CellCenter(structure.Cell), 0.68f, Color.FromArgb(165, 225, 90));
            }
        }

        foreach (var door in _structures.Where(structure => structure.Kind == StructureKind.BlastDoor).ToList())
        {
            var doorCenter = CellCenter(door.Cell);

            foreach (var enemy in _enemies.Where(actor => actor.IsAlive && Distance(actor.Position, doorCenter) <= 30f))
            {
                door.Health = MathF.Max(0f, door.Health - (17f * deltaSeconds));
                enemy.Path.Clear();
                EmitRipple(doorCenter, 0.55f, Color.FromArgb(255, 180, 75));
            }

            if (door.Health <= 0f)
            {
                _structures.Remove(door);
            }
        }
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

    public void Render(Graphics graphics, Rectangle clientBounds, Point mousePosition)
    {
        _layoutSize = clientBounds.Size;

        using var background = new LinearGradientBrush(clientBounds, Color.FromArgb(7, 14, 22), Color.FromArgb(3, 8, 14), 90f);
        graphics.FillRectangle(background, clientBounds);
        using var vignette = new LinearGradientBrush(clientBounds, Color.FromArgb(0, 86, 229, 247), Color.FromArgb(26, 20, 54, 84), 22f);
        graphics.FillRectangle(vignette, clientBounds);

        DrawWorldDropShadow(graphics);

        var worldMousePosition = ScreenToWorldPoint(mousePosition);
        var graphicsState = graphics.Save();
        using (var worldTransform = CreateWorldProjectionMatrix())
        {
            graphics.MultiplyTransform(worldTransform);
            DrawWorldPanel(graphics);
            DrawStructures(graphics);
            DrawCore(graphics);
            DrawRipples(graphics);
            DrawActors(graphics, worldMousePosition);
        }

        graphics.Restore(graphicsState);
        DrawHud(graphics);

        if (_showBriefing)
        {
            DrawBriefingOverlay(graphics);
        }
    }

    private void DrawWorldDropShadow(Graphics graphics)
    {
        var corners = GetProjectedWorldCorners();
        var shadow = corners.Select(point => new PointF(point.X + 18f, point.Y + 20f)).ToArray();
        using var shadowBrush = new SolidBrush(Color.FromArgb(64, 0, 0, 0));
        graphics.FillPolygon(shadowBrush, shadow);
    }

    private void DrawWorldPanel(Graphics graphics)
    {
        using var worldBrush = new LinearGradientBrush(WorldBounds, Color.FromArgb(10, 22, 32), Color.FromArgb(4, 11, 18), 90f);
        graphics.FillRectangle(worldBrush, WorldBounds);

        DrawTacticalSurface(graphics);
        DrawSignalLane(graphics, new[]
        {
            new PointF(WorldBounds.Left + 46f, WorldBounds.Top + 392f),
            new PointF(WorldBounds.Left + 246f, WorldBounds.Top + 318f),
            new PointF(WorldBounds.Left + 536f, WorldBounds.Top + 276f),
            new PointF(WorldBounds.Right - 56f, WorldBounds.Top + 308f),
            new PointF(WorldBounds.Right - 84f, WorldBounds.Top + 382f),
            new PointF(WorldBounds.Left + 572f, WorldBounds.Top + 344f),
            new PointF(WorldBounds.Left + 258f, WorldBounds.Top + 382f),
            new PointF(WorldBounds.Left + 30f, WorldBounds.Top + 460f),
        }, Color.FromArgb(54, 234, 220, 182), Color.FromArgb(122, 246, 236, 214));
        DrawSignalLane(graphics, new[]
        {
            new PointF(WorldBounds.Left + 118f, WorldBounds.Top + 562f),
            new PointF(WorldBounds.Left + 328f, WorldBounds.Top + 440f),
            new PointF(WorldBounds.Left + 628f, WorldBounds.Top + 418f),
            new PointF(WorldBounds.Right - 112f, WorldBounds.Top + 468f),
            new PointF(WorldBounds.Right - 136f, WorldBounds.Top + 516f),
            new PointF(WorldBounds.Left + 620f, WorldBounds.Top + 472f),
            new PointF(WorldBounds.Left + 346f, WorldBounds.Top + 490f),
            new PointF(WorldBounds.Left + 140f, WorldBounds.Top + 610f),
        }, Color.FromArgb(36, 194, 230, 214), Color.FromArgb(98, 224, 240, 236));

        DrawCornerRelay(graphics, new PointF(WorldBounds.Left + 126f, WorldBounds.Top + 126f), 60f, Color.FromArgb(84, 104, 216, 104));
        DrawCornerRelay(graphics, new PointF(WorldBounds.Left + 142f, WorldBounds.Bottom - 108f), 72f, Color.FromArgb(84, 94, 214, 122));
        DrawCornerRelay(graphics, new PointF(WorldBounds.Right - 126f, WorldBounds.Top + 118f), 66f, Color.FromArgb(84, 104, 216, 104));
        DrawCornerRelay(graphics, new PointF(WorldBounds.Right - 154f, WorldBounds.Bottom - 96f), 78f, Color.FromArgb(84, 94, 214, 122));

        DrawBoardScanner(graphics, new PointF(WorldBounds.Left + (WorldBounds.Width * 0.52f), WorldBounds.Top + (WorldBounds.Height * 0.52f)), 186f, Color.FromArgb(88, 86, 229, 247));

        foreach (var cell in _permanentWalls)
        {
            var rectangle = CellRectangle(cell);
            var tile = Rectangle.Inflate(rectangle, -2, -2);
            if (IsPerimeterCell(cell))
            {
                DrawBoardTile(graphics, tile, Color.FromArgb(82, 70, 126, 150), Color.FromArgb(176, 118, 232, 246), true);
            }
            else
            {
                DrawRaisedBlock(graphics, tile, Color.FromArgb(90, 82, 132, 160), Color.FromArgb(42, 16, 36, 52), Color.FromArgb(210, 138, 228, 246), 14f);
            }
        }

        if (_phase == GamePhase.Construct)
        {
            foreach (var slot in _buildSlots)
            {
                DrawBuildSlotMarker(graphics, slot);
            }
        }

        using var borderPen = new Pen(Color.FromArgb(110, 116, 220, 236), 2.2f);
        graphics.DrawRectangle(borderPen, WorldBounds);
        using var innerPen = new Pen(Color.FromArgb(70, 84, 152, 172), 1f);
        graphics.DrawRectangle(innerPen, Rectangle.Inflate(WorldBounds, -10, -10));
    }

    private void DrawTacticalSurface(Graphics graphics)
    {
        var inner = Rectangle.Inflate(WorldBounds, -12, -12);
        using var boardBrush = new LinearGradientBrush(inner, Color.FromArgb(18, 32, 42), Color.FromArgb(6, 16, 22), 90f);
        graphics.FillRectangle(boardBrush, inner);

        using var sweepBrush = new LinearGradientBrush(inner, Color.FromArgb(0, 86, 229, 247), Color.FromArgb(36, 86, 229, 247), 24f);
        graphics.FillRectangle(sweepBrush, inner);

        using var gridMinor = new Pen(Color.FromArgb(24, 86, 229, 247), 1f);
        using var gridMajor = new Pen(Color.FromArgb(62, 86, 229, 247), 1.6f);
        for (var x = 0; x <= GridColumns; x++)
        {
            var xPos = WorldBounds.Left + (x * CellSize);
            graphics.DrawLine(x % 3 == 0 ? gridMajor : gridMinor, xPos, WorldBounds.Top, xPos, WorldBounds.Bottom);
        }

        for (var y = 0; y <= GridRows; y++)
        {
            var yPos = WorldBounds.Top + (y * CellSize);
            graphics.DrawLine(y % 3 == 0 ? gridMajor : gridMinor, WorldBounds.Left, yPos, WorldBounds.Right, yPos);
        }

        using var pathPen = new Pen(Color.FromArgb(40, 200, 245, 255), 2f);
        graphics.DrawBezier(pathPen,
            new PointF(WorldBounds.Left + 118f, WorldBounds.Top + 188f),
            new PointF(WorldBounds.Left + 312f, WorldBounds.Top + 234f),
            new PointF(WorldBounds.Left + 612f, WorldBounds.Top + 204f),
            new PointF(WorldBounds.Right - 142f, WorldBounds.Top + 286f));
        graphics.DrawBezier(pathPen,
            new PointF(WorldBounds.Left + 138f, WorldBounds.Bottom - 118f),
            new PointF(WorldBounds.Left + 366f, WorldBounds.Top + 492f),
            new PointF(WorldBounds.Left + 654f, WorldBounds.Top + 470f),
            new PointF(WorldBounds.Right - 166f, WorldBounds.Bottom - 104f));
    }

    private void DrawSignalLane(Graphics graphics, PointF[] points, Color fillColor, Color outlineColor)
    {
        using var lanePath = new GraphicsPath();
        lanePath.AddPolygon(points);

        using var laneBrush = new SolidBrush(fillColor);
        using var edgePen = new Pen(outlineColor, 2.4f)
        {
            LineJoin = LineJoin.Round,
        };

        graphics.FillPath(laneBrush, lanePath);
        graphics.DrawPath(edgePen, lanePath);
    }

    private void DrawCornerRelay(Graphics graphics, PointF center, float radius, Color accent)
    {
        using var shadow = new SolidBrush(Color.FromArgb(38, 0, 0, 0));
        graphics.FillEllipse(shadow, center.X - radius + 10f, center.Y - (radius * 0.45f) + 10f, radius * 2f, radius * 0.9f);

        using var glow = new SolidBrush(accent);
        using var rim = new Pen(Color.FromArgb(148, 146, 244, 146), 2f);
        graphics.FillEllipse(glow, center.X - radius, center.Y - (radius * 0.56f), radius * 2f, radius * 1.12f);
        graphics.DrawEllipse(rim, center.X - radius, center.Y - (radius * 0.56f), radius * 2f, radius * 1.12f);
    }

    private void DrawBoardScanner(Graphics graphics, PointF center, float radius, Color accent)
    {
        using var ringPen = new Pen(accent, 2f);
        using var outerPen = new Pen(Color.FromArgb(48, accent), 1.2f);
        using var crossPen = new Pen(Color.FromArgb(120, accent), 1.2f);
        graphics.DrawEllipse(outerPen, center.X - radius - 42f, center.Y - radius - 42f, (radius + 42f) * 2f, (radius + 42f) * 2f);
        graphics.DrawEllipse(ringPen, center.X - radius, center.Y - radius, radius * 2f, radius * 2f);
        graphics.DrawEllipse(outerPen, center.X - (radius * 0.48f), center.Y - (radius * 0.48f), radius * 0.96f, radius * 0.96f);
        graphics.DrawLine(crossPen, center.X - radius - 18f, center.Y, center.X + radius + 18f, center.Y);
        graphics.DrawLine(crossPen, center.X, center.Y - radius - 18f, center.X, center.Y + radius + 18f);
    }

    private void DrawBoardTile(Graphics graphics, Rectangle rectangle, Color fillColor, Color outlineColor, bool diagonalCut)
    {
        using var fill = new LinearGradientBrush(rectangle, fillColor, Color.FromArgb(34, 18, 44, 62), 90f);
        using var border = new Pen(outlineColor, 1.6f);
        using var detail = new Pen(Color.FromArgb(122, outlineColor), 1f);
        graphics.FillRectangle(fill, rectangle);
        graphics.DrawRectangle(border, rectangle);
        if (diagonalCut)
        {
            graphics.DrawLine(detail, rectangle.Left + 8, rectangle.Bottom - 8, rectangle.Right - 8, rectangle.Top + 8);
        }
    }

    private void DrawRaisedBlock(Graphics graphics, Rectangle rectangle, Color topColor, Color sideColor, Color outlineColor, float height)
    {
        var sideFace = new[]
        {
            new PointF(rectangle.Left, rectangle.Bottom),
            new PointF(rectangle.Right, rectangle.Bottom),
            new PointF(rectangle.Right + 8f, rectangle.Bottom + height),
            new PointF(rectangle.Left + 8f, rectangle.Bottom + height),
        };

        using var sideBrush = new SolidBrush(sideColor);
        using var topBrush = new SolidBrush(topColor);
        using var outlinePen = new Pen(outlineColor, 1.8f);
        graphics.FillPolygon(sideBrush, sideFace);
        graphics.FillRectangle(topBrush, rectangle);
        graphics.DrawPolygon(outlinePen, sideFace);
        graphics.DrawRectangle(outlinePen, rectangle);
        using var detailPen = new Pen(Color.FromArgb(120, 214, 240, 248), 1f);
        graphics.DrawLine(detailPen, rectangle.Left + 8f, rectangle.Bottom - 8f, rectangle.Right - 8f, rectangle.Top + 8f);
    }

    private void DrawBuildSlotMarker(Graphics graphics, Point slot)
    {
        var center = CellCenter(slot);
        using var outerPen = new Pen(Color.FromArgb(148, 84, 214, 228), 2f)
        {
            DashStyle = DashStyle.Dash,
        };
        using var innerBrush = new SolidBrush(Color.FromArgb(74, 70, 220, 205));
        using var haloPen = new Pen(Color.FromArgb(52, 116, 236, 248), 1.2f);
        graphics.FillEllipse(innerBrush, center.X - 10f, center.Y - 10f, 20f, 20f);
        graphics.DrawEllipse(outerPen, center.X - 18f, center.Y - 18f, 36f, 36f);
        graphics.DrawEllipse(haloPen, center.X - 30f, center.Y - 30f, 60f, 60f);
    }

    private void DrawStructures(Graphics graphics)
    {
        foreach (var structure in _structures)
        {
            var rectangle = Rectangle.Inflate(CellRectangle(structure.Cell), -6, -6);

            switch (structure.Kind)
            {
                case StructureKind.BlastDoor:
                    DrawRaisedBlock(graphics, rectangle, Color.FromArgb(128, 90, 162, 204), Color.FromArgb(48, 14, 44, 70), Color.FromArgb(232, 156, 238, 248), 18f);

                    var ratio = structure.Health / structure.MaxHealth;
                    using (var hpBack = new SolidBrush(Color.FromArgb(36, 0, 0, 0)))
                    using (var hpFill = new SolidBrush(Color.FromArgb(220, 88, 228, 220)))
                    {
                        var hpRect = new RectangleF(rectangle.Left, rectangle.Bottom + 3f, rectangle.Width, 5f);
                        graphics.FillRectangle(hpBack, hpRect);
                        graphics.FillRectangle(hpFill, hpRect.Left, hpRect.Top, hpRect.Width * ratio, hpRect.Height);
                    }
                    break;
                case StructureKind.HoneyTrap:
                    using (var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        graphics.FillEllipse(shadow, rectangle.Left + 6, rectangle.Top + 12, rectangle.Width, rectangle.Height);
                    }
                    using (var fill = new SolidBrush(Color.FromArgb(178, 238, 168, 62)))
                    using (var pen = new Pen(Color.FromArgb(255, 255, 230, 164), 2.2f))
                    using (var ring = new Pen(Color.FromArgb(88, 255, 210, 116), 1.4f))
                    {
                        graphics.FillEllipse(fill, rectangle);
                        graphics.DrawEllipse(pen, rectangle);
                        graphics.DrawEllipse(ring, rectangle.Left - 12, rectangle.Top - 12, rectangle.Width + 24, rectangle.Height + 24);
                    }
                    break;
                case StructureKind.StaticNest:
                    using (var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        graphics.FillEllipse(shadow, rectangle.Left + 6, rectangle.Top + 12, rectangle.Width, rectangle.Height);
                    }
                    using (var fill = new SolidBrush(Color.FromArgb(116, 92, 214, 128)))
                    using (var pen = new Pen(Color.FromArgb(225, 214, 255, 180), 2f))
                    {
                        graphics.FillEllipse(fill, rectangle);
                        graphics.DrawEllipse(pen, rectangle);
                    }

                    using (var auraPen = new Pen(Color.FromArgb(92, 132, 232, 154), 1.5f))
                    {
                        graphics.DrawEllipse(auraPen, rectangle.Left - 20, rectangle.Top - 20, rectangle.Width + 40, rectangle.Height + 40);
                        graphics.DrawEllipse(auraPen, rectangle.Left - 34, rectangle.Top - 34, rectangle.Width + 68, rectangle.Height + 68);
                    }
                    break;
            }
        }
    }

    private void DrawCore(Graphics graphics)
    {
        var coreCenter = CorePosition();
        var coreRect = new RectangleF(coreCenter.X - 24f, coreCenter.Y - 24f, 48f, 48f);

        using var glow = new SolidBrush(Color.FromArgb(56, 76, 228, 242));
        graphics.FillEllipse(glow, coreCenter.X - 64f, coreCenter.Y - 64f, 128f, 128f);

        using var fill = new SolidBrush(Color.FromArgb(220, 48, 168, 198));
        using var border = new Pen(Color.FromArgb(238, 214, 255, 255), 2.5f);
        using var outerRing = new Pen(Color.FromArgb(88, 118, 236, 246), 1.8f);
        graphics.FillEllipse(fill, coreRect);
        graphics.DrawEllipse(border, coreRect);
        graphics.DrawEllipse(outerRing, coreCenter.X - 42f, coreCenter.Y - 42f, 84f, 84f);
        graphics.DrawEllipse(outerRing, coreCenter.X - 78f, coreCenter.Y - 78f, 156f, 156f);

        var diamond = new[]
        {
            new PointF(coreCenter.X, coreCenter.Y - 14f),
            new PointF(coreCenter.X + 14f, coreCenter.Y),
            new PointF(coreCenter.X, coreCenter.Y + 14f),
            new PointF(coreCenter.X - 14f, coreCenter.Y),
        };
        using var diamondPen = new Pen(Color.FromArgb(248, 232, 255, 255), 1.8f);
        graphics.DrawPolygon(diamondPen, diamond);

        var ratio = _coreHealth / 180f;
        using var hpBack = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        using var hpFill = new SolidBrush(Color.FromArgb(225, 70, 220, 210));
        var hpRect = new RectangleF(coreCenter.X - 50f, coreCenter.Y + 30f, 100f, 8f);
        graphics.FillRectangle(hpBack, hpRect);
        graphics.FillRectangle(hpFill, hpRect.Left, hpRect.Top, hpRect.Width * ratio, hpRect.Height);
    }

    private void DrawRipples(Graphics graphics)
    {
        foreach (var ripple in _ripples)
        {
            if (_phase == GamePhase.Hunt && !_player.IsAlive)
            {
                continue;
            }

            if (_phase == GamePhase.Hunt && !PlayerCanPerceive(ripple.Position, ripple.Strength))
            {
                continue;
            }

            var progress = ripple.Age / ripple.Lifetime;
            var radius = 18f + (progress * 120f * ripple.Strength);
            var alpha = (int)(160f * (1f - progress));
            var color = Color.FromArgb(Math.Clamp(alpha, 10, 180), ripple.Color);

            using var pen = new Pen(color, 2.2f);
            using var halo = new Pen(Color.FromArgb(Math.Clamp(alpha / 2, 10, 90), ripple.Color), 1.2f);
            graphics.DrawEllipse(pen, ripple.Position.X - radius, ripple.Position.Y - radius, radius * 2f, radius * 2f);
            graphics.DrawEllipse(halo, ripple.Position.X - radius - 10f, ripple.Position.Y - radius - 10f, (radius * 2f) + 20f, (radius * 2f) + 20f);
        }
    }

    private void DrawActors(Graphics graphics, PointF mousePosition)
    {
        if (_player.IsAlive)
        {
            DrawPlayerFov(graphics);
        }

        DrawActor(graphics, _player, mousePosition);

        foreach (var ally in _allies)
        {
            DrawActor(graphics, ally, mousePosition);
        }

        foreach (var enemy in _enemies)
        {
            DrawEnemy(graphics, enemy);
        }
    }

    private void DrawActor(Graphics graphics, Actor actor, PointF mousePosition)
    {
        var center = actor.Position;
        var isPlayer = actor.Type == ActorType.Player;
        var color = actor.IsBoss ? Color.FromArgb(255, 245, 210, 110) : actor.Type == ActorType.Ally ? Color.FromArgb(255, 95, 225, 200) : Color.FromArgb(255, 75, 220, 245);

        if (isPlayer)
        {
            DrawBoardScanner(graphics, center, 68f, Color.FromArgb(96, 86, 229, 247));
        }

        using var shadow = new SolidBrush(Color.FromArgb(72, 0, 0, 0));
        graphics.FillEllipse(shadow, center.X - actor.Radius - 4f, center.Y - (actor.Radius * 0.25f), (actor.Radius * 2f) + 8f, actor.Radius + 12f);

        using var ringPen = new Pen(Color.FromArgb(isPlayer ? 240 : 180, color), isPlayer ? 2.4f : 1.8f);
        graphics.DrawEllipse(ringPen, center.X - actor.Radius - 8f, center.Y - 8f, (actor.Radius * 2f) + 16f, (actor.Radius * 1.25f) + 16f);

        using var fill = new SolidBrush(actor.IsAlive ? color : Color.FromArgb(80, 80, 80, 90));
        using var outline = new Pen(Color.FromArgb(220, 240, 250, 250), actor.IsBoss ? 2.8f : 2f);

        graphics.FillEllipse(fill, center.X - actor.Radius, center.Y - actor.Radius, actor.Radius * 2f, actor.Radius * 2f);
        graphics.DrawEllipse(outline, center.X - actor.Radius, center.Y - actor.Radius, actor.Radius * 2f, actor.Radius * 2f);

        var facing = new PointF(center.X + (MathF.Cos(actor.FacingAngle) * 22f), center.Y + (MathF.Sin(actor.FacingAngle) * 22f));
        using var directionPen = new Pen(Color.FromArgb(220, 240, 250, 250), 2f);
        graphics.DrawLine(directionPen, center, facing);

        using var textBrush = new SolidBrush(Color.FromArgb(230, 235, 245, 245));
        using var nameFont = new Font(UiFontFamily, isPlayer ? 11f : 9f, FontStyle.Bold);
        graphics.DrawString(actor.Name, nameFont, textBrush, center.X - 34f, center.Y - actor.Radius - 28f);

        var hpRatio = actor.Health / actor.MaxHealth;
        using var hpBack = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        using var hpFill = new SolidBrush(Color.FromArgb(220, 70, 220, 165));
        graphics.FillRectangle(hpBack, center.X - 28f, center.Y + actor.Radius + 6f, 56f, 5f);
        graphics.FillRectangle(hpFill, center.X - 28f, center.Y + actor.Radius + 6f, 56f * Math.Clamp(hpRatio, 0f, 1f), 5f);

        if (isPlayer && _phase == GamePhase.Hunt && actor.IsAlive)
        {
            using var aimPen = new Pen(Color.FromArgb(180, 110, 235, 255), 1.6f);
            graphics.DrawLine(aimPen, center, mousePosition);
        }
    }

    private void DrawEnemy(Graphics graphics, Actor enemy)
    {
        if (!enemy.IsAlive)
        {
            return;
        }

        if (!PlayerCanSee(enemy))
        {
            if (!PlayerCanPerceive(enemy.Position, 0.72f))
            {
                return;
            }

            DrawAudioCue(graphics, enemy);
            return;
        }

        var points = new[]
        {
            new PointF(enemy.Position.X, enemy.Position.Y - enemy.Radius - 2f),
            new PointF(enemy.Position.X + enemy.Radius + 2f, enemy.Position.Y),
            new PointF(enemy.Position.X, enemy.Position.Y + enemy.Radius + 2f),
            new PointF(enemy.Position.X - enemy.Radius - 2f, enemy.Position.Y),
        };

        using var shadow = new SolidBrush(Color.FromArgb(82, 0, 0, 0));
        graphics.FillEllipse(shadow, enemy.Position.X - enemy.Radius - 5f, enemy.Position.Y - (enemy.Radius * 0.1f), (enemy.Radius * 2f) + 10f, enemy.Radius + 12f);
        using var ringPen = new Pen(Color.FromArgb(205, 236, 105, 90), 2f);
        using var glowPen = new Pen(Color.FromArgb(92, 255, 164, 112), 1.2f);
        graphics.DrawEllipse(ringPen, enemy.Position.X - enemy.Radius - 8f, enemy.Position.Y - 6f, (enemy.Radius * 2f) + 16f, (enemy.Radius * 1.18f) + 14f);
        graphics.DrawEllipse(glowPen, enemy.Position.X - enemy.Radius - 18f, enemy.Position.Y - 16f, (enemy.Radius * 2f) + 36f, (enemy.Radius * 2f) + 24f);

        using var fill = new SolidBrush(Color.FromArgb(235, 230, 95, 85));
        using var border = new Pen(Color.FromArgb(255, 255, 220, 210), 2f);
        graphics.FillPolygon(fill, points);
        graphics.DrawPolygon(border, points);

        using var hpBack = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        using var hpFill = new SolidBrush(Color.FromArgb(220, 255, 140, 120));
        var ratio = enemy.Health / enemy.MaxHealth;
        graphics.FillRectangle(hpBack, enemy.Position.X - 24f, enemy.Position.Y + enemy.Radius + 8f, 48f, 5f);
        graphics.FillRectangle(hpFill, enemy.Position.X - 24f, enemy.Position.Y + enemy.Radius + 8f, 48f * ratio, 5f);
    }

    private void DrawPlayerFov(Graphics graphics)
    {
        var weapon = _weaponStats[_player.Weapon];
        var diameter = weapon.VisionRange * 2f;
        var startAngle = RadiansToDegrees(_player.FacingAngle) - (FovDegrees / 2f);

        using var path = new GraphicsPath();
        path.AddPie(_player.Position.X - weapon.VisionRange, _player.Position.Y - weapon.VisionRange, diameter, diameter, startAngle, FovDegrees);

        using var coneBrush = new PathGradientBrush(path)
        {
            CenterColor = Color.FromArgb(78, 248, 244, 214),
            SurroundColors = [Color.FromArgb(0, 120, 240, 255)],
        };

        graphics.FillPath(coneBrush, path);
    }

    private void DrawAudioCue(Graphics graphics, Actor enemy)
    {
        if (!_player.IsAlive)
        {
            return;
        }

        var direction = new PointF(enemy.Position.X - _player.Position.X, enemy.Position.Y - _player.Position.Y);
        var length = MathF.Max(1f, MathF.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y)));
        direction = new PointF(direction.X / length, direction.Y / length);

        var anchor = new PointF(_player.Position.X + (direction.X * 48f), _player.Position.Y + (direction.Y * 48f));
        var hearing = _player.HearingRange * _weaponStats[_player.Weapon].HearingMultiplier * 1.6f;
        var strength = Math.Clamp(1f - (length / hearing), 0.2f, 1f);
        var ring = 12f + (14f * strength);

        using var cuePen = new Pen(Color.FromArgb((int)(205 * strength), 255, 180, 110), 2.6f)
        {
            DashStyle = DashStyle.Dash,
        };
        using var centerPen = new Pen(Color.FromArgb((int)(180 * strength), 255, 224, 188), 1.2f);
        graphics.DrawEllipse(cuePen, anchor.X - ring, anchor.Y - ring, ring * 2f, ring * 2f);
        graphics.DrawEllipse(cuePen, anchor.X - ring - 7f, anchor.Y - ring - 7f, (ring * 2f) + 14f, (ring * 2f) + 14f);
        graphics.DrawLine(centerPen, anchor.X - (ring * 0.8f), anchor.Y, anchor.X + (ring * 0.8f), anchor.Y);
        graphics.DrawLine(centerPen, anchor.X, anchor.Y - (ring * 0.8f), anchor.X, anchor.Y + (ring * 0.8f));
    }

    private void DrawHud(Graphics graphics)
    {
        DrawPanelFrame(graphics, TopBarBounds);
        DrawPanelFrame(graphics, RosterBounds);
        DrawPanelFrame(graphics, IntelBounds);
        DrawPanelFrame(graphics, MinimapBounds);
        DrawPanelFrame(graphics, BottomHudBounds);

        DrawTopBar(graphics);
        DrawRosterPanel(graphics);
        DrawIntelPanel(graphics);
        DrawMiniMap(graphics);
        DrawBottomBar(graphics);
    }

    private void DrawBriefingOverlay(Graphics graphics)
    {
        var box = new Rectangle(WorldBounds.Left + 34, WorldBounds.Top + 32, WorldBounds.Width - 68, 224);

        using var backdrop = new SolidBrush(Color.FromArgb(205, 7, 14, 18));
        using var border = new Pen(Color.FromArgb(120, 90, 215, 230), 2f);
        graphics.FillRectangle(backdrop, box);
        graphics.DrawRectangle(border, box);

        DrawHudText(graphics, "死角240度を、音で視る。", 22f, FontStyle.Bold, Color.FromArgb(255, 225, 245, 250), box.Left + 22, box.Top + 20);
        DrawHudText(graphics, "ホログラム戦術 HUD に合わせて、上段で戦況、左右で索敵情報、下段で装備とコマンドを確認できます。", 11f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 22, box.Top + 66);
        DrawHudText(graphics, "構築は最初の一度だけ。以降は毎ラウンドごとにボスを指名し、賭け金を積み、120 度の視界でコアを守り抜いてください。", 11f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 22, box.Top + 94);
        DrawHudText(graphics, "ハチミツトラップは足音を増幅し、スタティックネストは偽の波紋を撒いて視界を乱します。", 11f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 22, box.Top + 122);
        DrawHudText(graphics, "スペースキーでこのパネルを閉じます。", 10f, FontStyle.Bold, Color.FromArgb(255, 255, 215, 135), box.Left + 22, box.Bottom - 36);
    }

    private void DrawTopBar(Graphics graphics)
    {
        var enemiesLeft = _pendingEnemies + _enemies.Count(enemy => enemy.IsAlive);
        var defendersLeft = LiveDefenders().Count();
        var leftBlock = new RectangleF(TopBarBounds.Left + 8f, TopBarBounds.Top + 4f, 136f, TopBarBounds.Height - 8f);
        var centerBlock = new RectangleF(leftBlock.Right + 6f, TopBarBounds.Top + 4f, TopBarBounds.Width - 300f, TopBarBounds.Height - 8f);
        var rightBlock = new RectangleF(TopBarBounds.Right - 144f, TopBarBounds.Top + 4f, 136f, TopBarBounds.Height - 8f);

        DrawCenteredHudText(graphics, "襲撃班", 10.2f, FontStyle.Bold, Color.FromArgb(220, 236, 122, 108), new RectangleF(leftBlock.Left, leftBlock.Top + 2f, leftBlock.Width, 14f));
        DrawCenteredHudText(graphics, enemiesLeft.ToString(), 19f, FontStyle.Bold, Color.FromArgb(255, 240, 128, 112), new RectangleF(leftBlock.Left, leftBlock.Top + 14f, leftBlock.Width, 24f));

        DrawCenteredHudText(graphics, $"第{Math.Min(_currentRound, TotalRounds)}/{TotalRounds}ラウンド", 14f, FontStyle.Bold, Color.FromArgb(246, 238, 224, 188), new RectangleF(centerBlock.Left, centerBlock.Top + 1f, centerBlock.Width, 18f));
        DrawCenteredHudText(graphics, $"フェーズ {PhaseLabel()}  |  残り {_roundTimer:0.0} 秒  |  資金 {_credits}", 9.2f, FontStyle.Bold, PhaseColor(), new RectangleF(centerBlock.Left, centerBlock.Top + 22f, centerBlock.Width, 16f));

        DrawCenteredHudText(graphics, "防衛班", 10.2f, FontStyle.Bold, Color.FromArgb(220, 125, 230, 214), new RectangleF(rightBlock.Left, rightBlock.Top + 2f, rightBlock.Width, 14f));
        DrawCenteredHudText(graphics, defendersLeft.ToString(), 19f, FontStyle.Bold, Color.FromArgb(255, 120, 236, 218), new RectangleF(rightBlock.Left, rightBlock.Top + 14f, rightBlock.Width, 24f));
    }

    private void DrawRosterPanel(Graphics graphics)
    {
        DrawPanelTitle(graphics, RosterBounds, "味方");

        var y = RosterBounds.Top + 38;
        foreach (var actor in new[] { _player }.Concat(_allies))
        {
            var row = new Rectangle(RosterBounds.Left + 10, y, RosterBounds.Width - 20, 38);
            var accent = actor.IsBoss ? Color.FromArgb(255, 245, 210, 110) : actor.Type == ActorType.Player ? Color.FromArgb(255, 95, 225, 245) : Color.FromArgb(255, 95, 225, 200);
            using var fill = new SolidBrush(Color.FromArgb(96, 16, 24, 30));
            using var border = new Pen(Color.FromArgb(160, accent), actor.IsBoss ? 2.2f : 1.4f);
            graphics.FillRectangle(fill, row);
            graphics.DrawRectangle(border, row);

            using var orbBrush = new SolidBrush(accent);
            graphics.FillEllipse(orbBrush, row.Left + 8, row.Top + 6, 26, 26);

            DrawHudText(graphics, actor.Name, 8.9f, FontStyle.Bold, Color.FromArgb(240, 238, 244, 248), row.Left + 42, row.Top + 6);
            DrawHudText(graphics, actor.IsBoss ? "ボス" : "防衛", 7.8f, FontStyle.Bold, Color.FromArgb(220, accent), row.Left + 42, row.Top + 20);

            var hpRatio = actor.Health / actor.MaxHealth;
            using var hpBack = new SolidBrush(Color.FromArgb(55, 0, 0, 0));
            using var hpFill = new SolidBrush(Color.FromArgb(235, 82, 220, 170));
            var hpRect = new RectangleF(row.Left + 90, row.Top + 14, row.Width - 102, 8f);
            graphics.FillRectangle(hpBack, hpRect);
            graphics.FillRectangle(hpFill, hpRect.Left, hpRect.Top, hpRect.Width * Math.Clamp(hpRatio, 0f, 1f), hpRect.Height);

            y += 44;
        }
    }

    private void DrawIntelPanel(Graphics graphics)
    {
        DrawPanelTitle(graphics, IntelBounds, "ターゲット");

        var body = _phase switch
        {
            GamePhase.Construct => $"構築候補\n{BuildToolLabel(_selectedBuildTool)}",
            GamePhase.Bet => $"指名ボス\n{_selectedBossName}",
            GamePhase.Hunt => $"防衛目標\nデータコア {(int)_coreHealth}/180",
            GamePhase.RoundResult => "ラウンド精算",
            GamePhase.Victory => "制圧完了",
            _ => "防衛失敗",
        };

        using var font = new Font(UiFontFamily, 10.2f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.FromArgb(228, 208, 220, 228));
        var bodyRect = new RectangleF(IntelBounds.Left + 16, IntelBounds.Top + 38, IntelBounds.Width - 32, 36);
        graphics.DrawString(body, font, brush, bodyRect);

        using var feedFont = new Font(UiFontFamily, 8.8f, FontStyle.Regular);
        using var feedBrush = new SolidBrush(Color.FromArgb(235, 248, 214, 130));
        graphics.DrawString(_resultMessage, feedFont, feedBrush, new RectangleF(IntelBounds.Left + 16, IntelBounds.Top + 70, IntelBounds.Width - 32, 28));
    }

    private void DrawMiniMap(Graphics graphics)
    {
        DrawPanelTitle(graphics, MinimapBounds, "ミニマップ");

        var inner = Rectangle.Inflate(MinimapBounds, -12, -12);
        inner = new Rectangle(inner.Left, inner.Top + 14, inner.Width, inner.Height - 14);

        using var mapBrush = new SolidBrush(Color.FromArgb(188, 10, 16, 22));
        graphics.FillRectangle(mapBrush, inner);

        using (var gridPen = new Pen(Color.FromArgb(26, 98, 228, 242), 1f))
        {
            for (var x = 1; x < 10; x++)
            {
                var xPos = inner.Left + ((inner.Width / 10f) * x);
                graphics.DrawLine(gridPen, xPos, inner.Top, xPos, inner.Bottom);
            }

            for (var y = 1; y < 10; y++)
            {
                var yPos = inner.Top + ((inner.Height / 10f) * y);
                graphics.DrawLine(gridPen, inner.Left, yPos, inner.Right, yPos);
            }
        }

        var scaleX = inner.Width / (float)WorldBounds.Width;
        var scaleY = inner.Height / (float)WorldBounds.Height;

        foreach (var cell in _permanentWalls)
        {
            var rect = CellRectangle(cell);
            var miniRect = new RectangleF(
                inner.Left + ((rect.Left - WorldBounds.Left) * scaleX),
                inner.Top + ((rect.Top - WorldBounds.Top) * scaleY),
                rect.Width * scaleX,
                rect.Height * scaleY);
            using var wallBrush = new SolidBrush(Color.FromArgb(160, 46, 62, 74));
            graphics.FillRectangle(wallBrush, miniRect);
        }

        foreach (var structure in _structures)
        {
            var center = CellCenter(structure.Cell);
            var color = structure.Kind switch
            {
                StructureKind.BlastDoor => Color.FromArgb(255, 105, 235, 240),
                StructureKind.HoneyTrap => Color.FromArgb(255, 255, 196, 82),
                _ => Color.FromArgb(255, 180, 235, 120),
            };

            using var brush = new SolidBrush(color);
            var point = new PointF(inner.Left + ((center.X - WorldBounds.Left) * scaleX), inner.Top + ((center.Y - WorldBounds.Top) * scaleY));
            graphics.FillEllipse(brush, point.X - 3.5f, point.Y - 3.5f, 7f, 7f);
        }

        DrawMiniMapActor(graphics, inner, scaleX, scaleY, _player, Color.FromArgb(255, 90, 225, 245));
        foreach (var ally in _allies)
        {
            DrawMiniMapActor(graphics, inner, scaleX, scaleY, ally, Color.FromArgb(255, 95, 225, 200));
        }

        foreach (var enemy in _enemies.Where(actor => actor.IsAlive && PlayerCanPerceive(actor.Position, 0.72f)))
        {
            DrawMiniMapActor(graphics, inner, scaleX, scaleY, enemy, Color.FromArgb(255, 235, 105, 90));
        }

        var playerPoint = new PointF(inner.Left + ((_player.Position.X - WorldBounds.Left) * scaleX), inner.Top + ((_player.Position.Y - WorldBounds.Top) * scaleY));
        using (var pingPen = new Pen(Color.FromArgb(112, 98, 228, 242), 1.2f))
        {
            graphics.DrawEllipse(pingPen, playerPoint.X - 18f, playerPoint.Y - 18f, 36f, 36f);
            graphics.DrawEllipse(pingPen, playerPoint.X - 32f, playerPoint.Y - 32f, 64f, 64f);
        }

        var core = CorePosition();
        using var coreBrush = new SolidBrush(Color.FromArgb(255, 78, 220, 195));
        var corePoint = new PointF(inner.Left + ((core.X - WorldBounds.Left) * scaleX), inner.Top + ((core.Y - WorldBounds.Top) * scaleY));
        graphics.FillEllipse(coreBrush, corePoint.X - 5f, corePoint.Y - 5f, 10f, 10f);

        using var border = new Pen(Color.FromArgb(146, 194, 170, 110), 2.2f);
        graphics.DrawRectangle(border, inner);
        DrawHudText(graphics, "水色=味方  赤=敵  緑=コア", 8.2f, FontStyle.Regular, Color.FromArgb(220, 190, 210, 220), MinimapBounds.Left + 14, MinimapBounds.Bottom - 18);
    }

    private void DrawBottomBar(Graphics graphics)
    {
        var portraitCenter = new PointF(BottomHudBounds.Left + 96f, BottomHudBounds.Top + 84f);
        var portraitDiameter = 126f;
        var commandRect = new Rectangle(BottomHudBounds.Left + 150, BottomHudBounds.Top + 18, 328, 150);
        var skillsRect = new Rectangle(commandRect.Right + 14, BottomHudBounds.Top + 24, 270, 118);
        var itemRect = new Rectangle(skillsRect.Right + 14, BottomHudBounds.Top + 24, 116, 118);
        var weaponRect = new Rectangle(BottomHudBounds.Left + 18, BottomHudBounds.Bottom - 46, 214, 28);
        var statusRect = new Rectangle(commandRect.Left, BottomHudBounds.Bottom - 46, BottomHudBounds.Right - commandRect.Left - 18, 28);

        DrawChampionHudFrame(graphics, BottomHudBounds);
        DrawInsetPanel(graphics, commandRect);
        DrawInsetPanel(graphics, skillsRect);
        DrawInsetPanel(graphics, itemRect);
        DrawInsetPanel(graphics, weaponRect);
        DrawInsetPanel(graphics, statusRect);

        DrawPortraitOrb(graphics, portraitCenter, portraitDiameter, Color.FromArgb(255, 88, 220, 245));
        DrawCenteredHudText(graphics, "あなた", 12f, FontStyle.Bold, Color.FromArgb(242, 238, 244, 248), new RectangleF(BottomHudBounds.Left + 14, BottomHudBounds.Top + 132, 164, 18));
        DrawCenteredHudText(graphics, _weaponStats[_player.Weapon].Label, 8.4f, FontStyle.Bold, Color.FromArgb(236, 200, 214, 224), new RectangleF(BottomHudBounds.Left + 14, BottomHudBounds.Top + 150, 164, 16));

        var hpBar = new RectangleF(commandRect.Left + 14, commandRect.Top + 16, commandRect.Width - 28, 16);
        var energyBar = new RectangleF(commandRect.Left + 14, commandRect.Top + 40, commandRect.Width - 28, 10);
        DrawLabeledBar(graphics, hpBar, "HP", _player.Health / _player.MaxHealth, Color.FromArgb(255, 98, 196, 98), Color.FromArgb(50, 14, 34, 18), $"{(int)_player.Health}/{(int)_player.MaxHealth}");
        DrawLabeledBar(graphics, energyBar, "SONIC", _weaponStats[_player.Weapon].HearingMultiplier / 1.35f, Color.FromArgb(255, 62, 180, 220), Color.FromArgb(42, 8, 26, 32), $"{_weaponStats[_player.Weapon].HearingMultiplier:0.0}x");

        DrawHudText(graphics, CurrentModeTitle(), 10.2f, FontStyle.Bold, PhaseColor(), commandRect.Left + 14, commandRect.Top + 62);
        using (var bodyFont = new Font(UiFontFamily, 9.5f, FontStyle.Regular))
        using (var bodyBrush = new SolidBrush(Color.FromArgb(230, 210, 224, 232)))
        {
            graphics.DrawString(CurrentModeBody(), bodyFont, bodyBrush, new RectangleF(commandRect.Left + 14, commandRect.Top + 82, commandRect.Width - 28, 38));
        }
        DrawHudText(graphics, CurrentControlsHint(), 7.4f, FontStyle.Bold, Color.FromArgb(250, 214, 196, 134), commandRect.Left + 14, commandRect.Bottom - 17);

        var abilityRects = new[]
        {
            new Rectangle(skillsRect.Left + 10, skillsRect.Top + 18, 58, 58),
            new Rectangle(skillsRect.Left + 74, skillsRect.Top + 18, 58, 58),
            new Rectangle(skillsRect.Left + 138, skillsRect.Top + 18, 58, 58),
            new Rectangle(skillsRect.Left + 202, skillsRect.Top + 18, 58, 58),
        };

        if (_phase == GamePhase.Construct)
        {
            DrawAbilitySlot(graphics, abilityRects[0], "1", "防壁", "封鎖", _selectedBuildTool == BuildToolKind.BlastDoor, Color.FromArgb(255, 116, 212, 230));
            DrawAbilitySlot(graphics, abilityRects[1], "2", "蜜罠", "鈍足", _selectedBuildTool == BuildToolKind.HoneyTrap, Color.FromArgb(255, 230, 194, 88));
            DrawAbilitySlot(graphics, abilityRects[2], "3", "巣", "偽波", _selectedBuildTool == BuildToolKind.StaticNest, Color.FromArgb(255, 164, 220, 116));
            DrawAbilitySlot(graphics, abilityRects[3], "Enter", "確定", "構築", false, Color.FromArgb(255, 208, 170, 104));
        }
        else if (_phase == GamePhase.Bet)
        {
            DrawAbilitySlot(graphics, abilityRects[0], "1", "ボス", "あなた", _selectedBossName == "あなた", Color.FromArgb(255, 116, 212, 230));
            DrawAbilitySlot(graphics, abilityRects[1], "2", "ボス", "北", _selectedBossName == "北アンカー", Color.FromArgb(255, 164, 220, 116));
            DrawAbilitySlot(graphics, abilityRects[2], "3", "ボス", "南", _selectedBossName == "南アンカー", Color.FromArgb(255, 230, 194, 88));
            DrawAbilitySlot(graphics, abilityRects[3], "Enter", "出撃", $"{_selectedBet}", false, Color.FromArgb(255, 208, 170, 104));
        }
        else
        {
            DrawAbilitySlot(graphics, abilityRects[0], "Q", "音紋", "追跡", _player.Weapon == WeaponType.SMG, Color.FromArgb(255, 230, 194, 88));
            DrawAbilitySlot(graphics, abilityRects[1], "W", "視界", $"{_weaponStats[_player.Weapon].VisionRange:0}", _player.Weapon == WeaponType.Rifle, Color.FromArgb(255, 116, 212, 230));
            DrawAbilitySlot(graphics, abilityRects[2], "E", "賭け", $"{_selectedBet}", _player.IsBoss, Color.FromArgb(255, 164, 220, 116));
            DrawAbilitySlot(graphics, abilityRects[3], "R", "コア", $"{(int)_coreHealth}", false, Color.FromArgb(255, 208, 170, 104));
        }

        DrawHudText(graphics, "スキル / コマンド", 8.5f, FontStyle.Bold, Color.FromArgb(236, 206, 216, 228), skillsRect.Left + 10, skillsRect.Top + 90);

        var itemRects = new[]
        {
            new Rectangle(itemRect.Left + 10, itemRect.Top + 18, 28, 28),
            new Rectangle(itemRect.Left + 44, itemRect.Top + 18, 28, 28),
            new Rectangle(itemRect.Left + 78, itemRect.Top + 18, 28, 28),
            new Rectangle(itemRect.Left + 10, itemRect.Top + 52, 28, 28),
            new Rectangle(itemRect.Left + 44, itemRect.Top + 52, 28, 28),
            new Rectangle(itemRect.Left + 78, itemRect.Top + 52, 28, 28),
        };

        DrawItemSlot(graphics, itemRects[0], _phase == GamePhase.Construct ? "AP" : "SMG", Color.FromArgb(255, 116, 212, 230), _phase == GamePhase.Construct);
        DrawItemSlot(graphics, itemRects[1], _phase == GamePhase.Construct ? "蜜" : "RFL", Color.FromArgb(255, 230, 194, 88), _selectedWeapon == WeaponType.Rifle);
        DrawItemSlot(graphics, itemRects[2], _phase == GamePhase.Construct ? "巣" : "SR", Color.FromArgb(255, 164, 220, 116), _selectedWeapon == WeaponType.Sniper);
        DrawItemSlot(graphics, itemRects[3], "賭", Color.FromArgb(255, 208, 170, 104), _phase == GamePhase.Bet);
        DrawItemSlot(graphics, itemRects[4], "音", Color.FromArgb(255, 84, 188, 228), _phase == GamePhase.Hunt);
        DrawItemSlot(graphics, itemRects[5], "R", Color.FromArgb(255, 212, 104, 104), _phase is GamePhase.Victory or GamePhase.Defeat);

        DrawCenteredHudText(graphics, $"G {_credits}", 11.5f, FontStyle.Bold, Color.FromArgb(255, 238, 202, 112), new RectangleF(itemRect.Left + 6, itemRect.Bottom - 24, itemRect.Width - 12, 18));
        DrawWeaponStatusCard(graphics, weaponRect);
        DrawQuickStatusStrip(graphics, statusRect);
    }

    private void DrawHudText(Graphics graphics, string text, float size, FontStyle style, Color color, float x, float y)
    {
        using var font = new Font(UiFontFamily, size, style);
        using var brush = new SolidBrush(color);
        graphics.DrawString(text, font, brush, x, y);
    }

    private void DrawCenteredHudText(Graphics graphics, string text, float size, FontStyle style, Color color, RectangleF bounds)
    {
        using var font = new Font(UiFontFamily, size, style);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
        };

        graphics.DrawString(text, font, brush, bounds, format);
    }

    private void DrawChampionHudFrame(Graphics graphics, Rectangle bounds)
    {
        using var outerBrush = new LinearGradientBrush(bounds, Color.FromArgb(188, 18, 30, 42), Color.FromArgb(168, 10, 16, 22), 90f);
        graphics.FillRectangle(outerBrush, bounds);

        using var goldBorder = new Pen(Color.FromArgb(214, 170, 146, 92), 2.4f);
        graphics.DrawRectangle(goldBorder, bounds);

        using var innerBorder = new Pen(Color.FromArgb(105, 80, 116, 132), 1.2f);
        graphics.DrawRectangle(innerBorder, Rectangle.Inflate(bounds, -8, -8));

        var crest = new[]
        {
            new Point(bounds.Left + 18, bounds.Top),
            new Point(bounds.Left + 46, bounds.Top - 14),
            new Point(bounds.Left + 78, bounds.Top),
        };

        using var crestBrush = new SolidBrush(Color.FromArgb(214, 170, 146, 92));
        graphics.FillPolygon(crestBrush, crest);
        graphics.FillPolygon(crestBrush, crest.Select(point => new Point(bounds.Right - (point.X - bounds.Left), point.Y)).ToArray());
    }

    private void DrawPortraitOrb(Graphics graphics, PointF center, float diameter, Color accent)
    {
        var outerRect = new RectangleF(center.X - (diameter / 2f), center.Y - (diameter / 2f), diameter, diameter);
        var innerRect = RectangleF.Inflate(outerRect, -10f, -10f);

        using var shadow = new SolidBrush(Color.FromArgb(72, 0, 0, 0));
        graphics.FillEllipse(shadow, outerRect.X + 8f, outerRect.Y + 10f, outerRect.Width, outerRect.Height);

        using var rimBrush = new LinearGradientBrush(Rectangle.Round(outerRect), Color.FromArgb(220, 176, 152, 94), Color.FromArgb(150, 84, 60, 34), 90f);
        using var coreBrush = new LinearGradientBrush(Rectangle.Round(innerRect), accent, Color.FromArgb(255, 26, 52, 72), 90f);
        using var rimPen = new Pen(Color.FromArgb(244, 212, 184, 122), 2.4f);
        using var innerPen = new Pen(Color.FromArgb(120, 222, 240, 248), 1.4f);
        graphics.FillEllipse(rimBrush, outerRect);
        graphics.FillEllipse(coreBrush, innerRect);
        graphics.DrawEllipse(rimPen, outerRect);
        graphics.DrawEllipse(innerPen, innerRect);

        using var emblemPen = new Pen(Color.FromArgb(210, 236, 244, 248), 2f);
        graphics.DrawLine(emblemPen, center.X - 18f, center.Y, center.X + 18f, center.Y);
        graphics.DrawLine(emblemPen, center.X, center.Y - 18f, center.X, center.Y + 18f);
        graphics.DrawEllipse(emblemPen, center.X - 10f, center.Y - 10f, 20f, 20f);
    }

    private void DrawLabeledBar(Graphics graphics, RectangleF bounds, string label, float ratio, Color fillColor, Color backColor, string valueText)
    {
        ratio = Math.Clamp(ratio, 0f, 1f);

        using var backBrush = new SolidBrush(backColor);
        using var fillBrush = new SolidBrush(fillColor);
        using var borderPen = new Pen(Color.FromArgb(148, 170, 146, 92), 1.2f);
        graphics.FillRectangle(backBrush, bounds);
        graphics.FillRectangle(fillBrush, bounds.Left, bounds.Top, bounds.Width * ratio, bounds.Height);
        graphics.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);

        DrawHudText(graphics, label, 7.2f, FontStyle.Bold, Color.FromArgb(236, 240, 232, 214), bounds.Left + 4f, bounds.Top - 1f);
        DrawHudText(graphics, valueText, 7.4f, FontStyle.Bold, Color.FromArgb(236, 240, 232, 214), bounds.Right - 48f, bounds.Top - 1f);
    }

    private void DrawAbilitySlot(Graphics graphics, Rectangle bounds, string hotkey, string title, string subtitle, bool selected, Color accent)
    {
        using var fill = new LinearGradientBrush(bounds, selected ? Color.FromArgb(164, accent) : Color.FromArgb(106, 18, 26, 32), Color.FromArgb(82, 8, 12, 18), 90f);
        using var border = new Pen(selected ? Color.FromArgb(248, accent) : Color.FromArgb(128, 170, 146, 92), selected ? 2.2f : 1.4f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);

        DrawHudText(graphics, hotkey, 8f, FontStyle.Bold, Color.FromArgb(246, 244, 228, 196), bounds.Left + 6, bounds.Top + 4);
        DrawCenteredHudText(graphics, title, 9f, FontStyle.Bold, Color.FromArgb(242, 238, 244, 248), new RectangleF(bounds.Left + 4, bounds.Top + 18, bounds.Width - 8, 14));
        DrawCenteredHudText(graphics, subtitle, 7.4f, FontStyle.Regular, Color.FromArgb(226, 208, 220, 228), new RectangleF(bounds.Left + 4, bounds.Top + 34, bounds.Width - 8, 14));
    }

    private void DrawItemSlot(Graphics graphics, Rectangle bounds, string label, Color accent, bool selected)
    {
        using var fill = new SolidBrush(selected ? Color.FromArgb(132, accent) : Color.FromArgb(92, 18, 26, 32));
        using var border = new Pen(selected ? Color.FromArgb(240, accent) : Color.FromArgb(116, 154, 138, 102), selected ? 2f : 1.2f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);
        DrawCenteredHudText(graphics, label, 8.2f, FontStyle.Bold, Color.FromArgb(246, 238, 244, 248), new RectangleF(bounds.Left + 2, bounds.Top + 2, bounds.Width - 4, bounds.Height - 4));
    }

    private void DrawWeaponStatusCard(Graphics graphics, Rectangle bounds)
    {
        var weaponType = DisplayedWeaponType();
        using var weaponPen = new Pen(Color.FromArgb(228, 214, 188, 118), 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        var midY = bounds.Top + (bounds.Height / 2f);
        graphics.DrawLine(weaponPen, bounds.Left + 16f, midY, bounds.Left + 104f, midY);
        graphics.DrawLine(weaponPen, bounds.Left + 34f, midY - 8f, bounds.Left + 58f, midY - 8f);
        graphics.DrawLine(weaponPen, bounds.Left + 76f, midY, bounds.Left + 92f, midY - 10f);
        graphics.DrawLine(weaponPen, bounds.Left + 92f, midY - 10f, bounds.Left + 110f, midY - 10f);
        graphics.DrawLine(weaponPen, bounds.Left + 96f, midY, bounds.Left + 118f, midY + 6f);

        DrawHudText(graphics, weaponType switch
        {
            WeaponType.SMG => "SMG-02",
            WeaponType.Sniper => "SR-11",
            _ => "RFL-07",
        }, 7.8f, FontStyle.Bold, Color.FromArgb(236, 238, 244, 248), bounds.Left + 122, bounds.Top + 5);
        DrawHudText(graphics, $"x {CurrentMagazineAmmo()}", 9.2f, FontStyle.Bold, Color.FromArgb(248, 238, 244, 248), bounds.Right - 44, bounds.Top + 4);
        DrawHudText(graphics, $"RP {(int)_player.Health} / {(int)_player.MaxHealth}", 7.6f, FontStyle.Bold, Color.FromArgb(236, 164, 232, 168), bounds.Left + 12, bounds.Bottom - 14);
    }

    private void DrawQuickStatusStrip(Graphics graphics, Rectangle bounds)
    {
        var chipBounds = new[]
        {
            new Rectangle(bounds.Left + 8, bounds.Top + 3, 28, bounds.Height - 6),
            new Rectangle(bounds.Left + 40, bounds.Top + 3, 28, bounds.Height - 6),
            new Rectangle(bounds.Left + 72, bounds.Top + 3, 34, bounds.Height - 6),
        };

        DrawItemSlot(graphics, chipBounds[0], "R", Color.FromArgb(255, 116, 212, 230), _phase == GamePhase.RoundResult);
        DrawItemSlot(graphics, chipBounds[1], "C", Color.FromArgb(255, 214, 190, 108), _phase == GamePhase.Construct);
        DrawItemSlot(graphics, chipBounds[2], "3", Color.FromArgb(255, 220, 170, 92), false);

        DrawHudText(graphics, CurrentControlsHint(), 7.7f, FontStyle.Bold, Color.FromArgb(238, 214, 224, 232), bounds.Left + 118, bounds.Top + 8);
    }

    private void DrawPanelFrame(Graphics graphics, Rectangle bounds)
    {
        using var fill = new LinearGradientBrush(bounds, Color.FromArgb(188, 14, 20, 28), Color.FromArgb(160, 8, 12, 18), 90f);
        using var border = new Pen(Color.FromArgb(166, 170, 146, 92), 2f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);
        using var inner = new Pen(Color.FromArgb(76, 90, 112, 120), 1f);
        graphics.DrawRectangle(inner, Rectangle.Inflate(bounds, -6, -6));
    }

    private void DrawInsetPanel(Graphics graphics, Rectangle bounds)
    {
        using var fill = new LinearGradientBrush(bounds, Color.FromArgb(108, 18, 24, 30), Color.FromArgb(78, 8, 12, 18), 90f);
        using var border = new Pen(Color.FromArgb(118, 166, 140, 88), 1.6f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);
        using var inner = new Pen(Color.FromArgb(44, 96, 220, 232), 1f);
        graphics.DrawRectangle(inner, Rectangle.Inflate(bounds, -4, -4));
    }

    private void DrawPanelTitle(Graphics graphics, Rectangle bounds, string title)
    {
        DrawHudText(graphics, title, 10.6f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), bounds.Left + 12, bounds.Top + 8);
        using var accent = new Pen(Color.FromArgb(132, 170, 146, 92), 1.4f);
        graphics.DrawLine(accent, bounds.Left + 12, bounds.Top + 26, bounds.Right - 12, bounds.Top + 26);
    }

    private void DrawChoiceCard(Graphics graphics, Rectangle bounds, string keyLabel, string title, string subtitle, bool selected, Color accent)
    {
        using var fill = new SolidBrush(selected ? Color.FromArgb(120, accent) : Color.FromArgb(70, 16, 24, 30));
        using var border = new Pen(selected ? Color.FromArgb(240, accent) : Color.FromArgb(100, 76, 110, 124), selected ? 2.2f : 1.4f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);

        DrawHudText(graphics, keyLabel, 9f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), bounds.Left + 10, bounds.Top + 8);
        DrawHudText(graphics, title, 10.2f, FontStyle.Bold, Color.FromArgb(240, 238, 244, 248), bounds.Left + 10, bounds.Top + 30);
        DrawHudText(graphics, subtitle, 8.8f, FontStyle.Regular, Color.FromArgb(225, 204, 218, 226), bounds.Left + 10, bounds.Top + 56);
    }

    private void DrawWeaponChoice(Graphics graphics, Rectangle bounds, string keyLabel, WeaponType weaponType, bool selected)
    {
        var weapon = _weaponStats[weaponType];
        var accent = weaponType switch
        {
            WeaponType.SMG => Color.FromArgb(255, 255, 196, 82),
            WeaponType.Rifle => Color.FromArgb(255, 92, 220, 235),
            _ => Color.FromArgb(255, 245, 170, 120),
        };

        DrawChoiceCard(graphics, bounds, keyLabel, weapon.Label, $"{weapon.Cost}c / 視界 {weapon.VisionRange:0}", selected, accent);
    }

    private void DrawMiniMapActor(Graphics graphics, Rectangle inner, float scaleX, float scaleY, Actor actor, Color color)
    {
        if (!actor.IsAlive)
        {
            return;
        }

        using var brush = new SolidBrush(color);
        var point = new PointF(inner.Left + ((actor.Position.X - WorldBounds.Left) * scaleX), inner.Top + ((actor.Position.Y - WorldBounds.Top) * scaleY));
        var size = actor.IsBoss ? 8f : 6f;
        graphics.FillEllipse(brush, point.X - (size / 2f), point.Y - (size / 2f), size, size);
    }

    private string CurrentModeTitle()
    {
        return _phase switch
        {
            GamePhase.Construct => "初期構築フェーズ",
            GamePhase.Bet => "ベット & ロードアウト",
            GamePhase.Hunt => "防衛ハント進行中",
            GamePhase.RoundResult => "ラウンド精算",
            GamePhase.Victory => "作戦成功",
            _ => "作戦失敗",
        };
    }

    private string CurrentModeBody()
    {
        return _phase switch
        {
            GamePhase.Construct => $"{BuildToolLabel(_selectedBuildTool)} を選択中。初期配置は全ラウンド共通です。",
            GamePhase.Bet => $"{_selectedBossName} を賞金首に指名。賭け金 {_selectedBet}、武器 {_weaponStats[_selectedWeapon].Label}。",
            GamePhase.Hunt => $"120 度視界で防衛中。残り {_roundTimer:0.0} 秒、残敵 {_pendingEnemies + _enemies.Count(enemy => enemy.IsAlive)}。",
            GamePhase.RoundResult => _resultMessage,
            GamePhase.Victory => "全ラウンド防衛成功。ループを最後まで完走しました。",
            _ => "コア破壊、または防衛班壊滅。再編成して再挑戦。",
        };
    }

    private string CurrentControlsHint()
    {
        return _phase switch
        {
            GamePhase.Construct => "1/2/3 選択  左設置  右撤去  Enter確定",
            GamePhase.Bet => "1/2/3 ボス  Q/E武器  A/D賭け  Enter出撃",
            GamePhase.Hunt => "WASD移動  マウス照準  左クリック射撃",
            _ => "Enter進行  R再挑戦",
        };
    }

    private bool ActiveSelection(WeaponType weaponType)
    {
        return _phase switch
        {
            GamePhase.Bet => _selectedWeapon == weaponType,
            _ => _player.Weapon == weaponType,
        };
    }

    private WeaponType DisplayedWeaponType()
    {
        return _phase == GamePhase.Bet ? _selectedWeapon : _player.Weapon;
    }

    private int CurrentMagazineAmmo()
    {
        return DisplayedWeaponType() switch
        {
            WeaponType.SMG => 42,
            WeaponType.Sniper => 8,
            _ => 30,
        };
    }

    private void TryPlaceStructure(Point location)
    {
        if (!TryGetWorldPointFromScreen(location, out _))
        {
            return;
        }

        var cell = ScreenToCell(location);
        if (!_buildSlots.Contains(cell) || _structures.Any(structure => structure.Cell == cell))
        {
            return;
        }

        var candidate = CreateStructure(_selectedBuildTool, cell);
        if (candidate.APCost > _buildPoints)
        {
            return;
        }

        _buildPoints -= candidate.APCost;
        _structures.Add(candidate);
        _resultMessage = $"{candidate.Label} を {cell.X},{cell.Y} に設置。";
    }

    private void TryRemoveStructure(Point location)
    {
        if (!TryGetWorldPointFromScreen(location, out _))
        {
            return;
        }

        var cell = ScreenToCell(location);
        var structure = _structures.FirstOrDefault(candidate => candidate.Cell == cell);
        if (structure is null)
        {
            return;
        }

        _buildPoints += structure.APCost;
        _structures.Remove(structure);
        _resultMessage = $"{structure.Label} を撤去して AP を返還。";
    }

    private void StartRound()
    {
        var weapon = _weaponStats[_selectedWeapon];
        var totalCost = weapon.Cost + _selectedBet;
        if (totalCost > _credits)
        {
            _resultMessage = "所持クレジットが足りません。賭け金か装備を見直してください。";
            return;
        }

        _credits -= totalCost;
        _coreHealth = 180f;
        _player.Weapon = _selectedWeapon;
        _player.Health = _player.MaxHealth;
        _player.Position = CellCenter(_player.HomeCell);
        _player.FireCooldown = 0f;

        foreach (var actor in _allies)
        {
            actor.Health = actor.MaxHealth;
            actor.Position = CellCenter(actor.HomeCell);
            actor.FireCooldown = 0f;
            actor.Path.Clear();
        }

        foreach (var structure in _structures.Where(structure => structure.Kind == StructureKind.BlastDoor))
        {
            structure.Health = structure.MaxHealth;
        }

        _enemies.Clear();
        _ripples.Clear();
        _pendingEnemies = 6 + (_currentRound * 3);
        _spawnCooldown = 0.4f;
        _roundTimer = 46f + (_currentRound * 3f);
        _pingCooldown = 0f;

        RestoreBossFlags();
        _phase = GamePhase.Hunt;
        _showBriefing = false;
        _resultDestination = GamePhase.Bet;
        _resultMessage = $"第{_currentRound}ラウンド開始。{_selectedBossName}を生存させて防衛を完了してください。";
    }

    private void EndRound(bool won)
    {
        var bossAlive = SelectedBoss()?.IsAlive ?? false;

        if (won)
        {
            _credits += 150;
            if (bossAlive)
            {
                _credits += _selectedBet * 2;
                _resultMessage = $"第{_currentRound}ラウンド防衛成功。ボス生存につき +{(_selectedBet * 2) + 150} クレジット。";
            }
            else
            {
                _resultMessage = $"第{_currentRound}ラウンドは勝利。ただしボスが倒れたため賭け金は没収。";
            }

            _currentRound++;
            if (_currentRound > TotalRounds)
            {
                _resultDestination = GamePhase.Victory;
                _resultMessage = $"全ラウンド制圧。最終資産 {_credits} クレジット。";
            }
            else
            {
                _resultDestination = GamePhase.Bet;
            }
        }
        else
        {
            _resultDestination = GamePhase.Defeat;
            _resultMessage = "グリッド崩壊。コア突破、または防衛班壊滅。";
        }

        _phase = GamePhase.RoundResult;
        _resultTimer = 2.4f;
    }

    private void BeginBetPhase()
    {
        _phase = GamePhase.Bet;
        _selectedBet = Math.Min(Math.Max(25, AffordableCredits()), 100);
        _resultDestination = GamePhase.Bet;
        _resultMessage = $"第{_currentRound}ラウンド準備。ボス、賭け金、武器を決めてください。";
    }

    private void ResetCampaign()
    {
        _buildPoints = 12;
        _credits = 425;
        _currentRound = 1;
        _selectedBet = 100;
        _selectedWeapon = WeaponType.Rifle;
        _selectedBuildTool = BuildToolKind.BlastDoor;
        _selectedBossName = "あなた";
        _coreHealth = 180f;
        _phase = GamePhase.Construct;
        _resultDestination = GamePhase.Bet;
        _showBriefing = true;
        _structures.Clear();
        _ripples.Clear();
        _enemies.Clear();
        _resultMessage = "陣地構築は一度だけ。この配置が全ラウンドを左右する。";

        _player.Health = _player.MaxHealth;
        _player.Position = CellCenter(_player.HomeCell);
        _player.Weapon = WeaponType.Rifle;

        foreach (var ally in _allies)
        {
            ally.Health = ally.MaxHealth;
            ally.Position = CellCenter(ally.HomeCell);
            ally.IsBoss = false;
            ally.Path.Clear();
        }

        _player.IsBoss = true;
    }

    private void RestoreBossFlags()
    {
        _player.IsBoss = _selectedBossName == _player.Name;
        foreach (var ally in _allies)
        {
            ally.IsBoss = ally.Name == _selectedBossName;
        }
    }

    private void CycleWeapon(int direction)
    {
        var order = new[] { WeaponType.SMG, WeaponType.Rifle, WeaponType.Sniper };
        var index = Array.IndexOf(order, _selectedWeapon);
        _selectedWeapon = order[(index + direction + order.Length) % order.Length];
    }

    private int AffordableCredits()
    {
        return Math.Max(25, _credits - _weaponStats[_selectedWeapon].Cost);
    }

    private Actor? SelectedBoss()
    {
        if (_selectedBossName == _player.Name)
        {
            return _player;
        }

        return _allies.FirstOrDefault(actor => actor.Name == _selectedBossName);
    }

    private void SpawnEnemiesIfNeeded()
    {
        if (_pendingEnemies <= 0 || _spawnCooldown > 0f)
        {
            return;
        }

        _spawnCooldown = 2.1f - MathF.Min(0.8f, _currentRound * 0.15f);
        _pendingEnemies--;

        var spawnCell = _spawnCells[_random.Next(_spawnCells.Count)];
        var weapon = _random.Next(100) switch
        {
            < 40 => WeaponType.SMG,
            < 85 => WeaponType.Rifle,
            _ => WeaponType.Sniper,
        };

        var enemyHealth = weapon == WeaponType.SMG ? 48f : weapon == WeaponType.Rifle ? 58f : 44f;
        var enemy = new Actor
        {
            Name = "襲撃者",
            Type = ActorType.Enemy,
            HomeCell = spawnCell,
            Weapon = weapon,
            Position = CellCenter(spawnCell),
            Radius = 13f,
            MaxHealth = enemyHealth,
            Health = enemyHealth,
            HearingRange = 260f,
            BaseMoveSpeed = weapon == WeaponType.SMG ? 155f : weapon == WeaponType.Rifle ? 130f : 118f,
        };

        _enemies.Add(enemy);
        EmitRipple(enemy.Position, 0.82f, Color.FromArgb(255, 150, 95, 75));
    }

    private void RebuildEnemyPath(Actor enemy)
    {
        enemy.Path.Clear();
        enemy.PathCooldown = 0.65f;

        var start = WorldToCell(enemy.Position);
        var desiredGoal = PickPathGoal(enemy);
        var path = FindPath(start, desiredGoal);

        if (path.Count == 0)
        {
            var nearestDoor = _structures
                .Where(structure => structure.Kind == StructureKind.BlastDoor)
                .OrderBy(structure => Distance(enemy.Position, CellCenter(structure.Cell)))
                .FirstOrDefault();

            if (nearestDoor is not null)
            {
                path = FindPath(start, nearestDoor.Cell);
            }
        }

        foreach (var cell in path.Skip(1))
        {
            enemy.Path.Enqueue(CellCenter(cell));
        }
    }

    private Point PickPathGoal(Actor enemy)
    {
        var defenders = LiveDefenders()
            .OrderBy(actor => Distance(enemy.Position, actor.Position))
            .ToList();

        if (defenders.Count > 0 && Distance(enemy.Position, defenders[0].Position) < 180f)
        {
            return WorldToCell(defenders[0].Position);
        }

        return new Point(14, 6);
    }

    private void FollowPath(Actor enemy, float deltaSeconds)
    {
        if (enemy.Path.Count == 0)
        {
            return;
        }

        var waypoint = enemy.Path.Peek();
        var vector = new PointF(waypoint.X - enemy.Position.X, waypoint.Y - enemy.Position.Y);
        var length = MathF.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y));

        if (length <= 4f)
        {
            enemy.Path.Dequeue();
            return;
        }

        vector = new PointF(vector.X / length, vector.Y / length);

        var speed = enemy.BaseMoveSpeed;
        var cell = WorldToCell(enemy.Position);
        if (_structures.Any(structure => structure.Kind == StructureKind.HoneyTrap && structure.Cell == cell))
        {
            speed *= 0.45f;
        }

        var next = new PointF(enemy.Position.X + (vector.X * speed * deltaSeconds), enemy.Position.Y + (vector.Y * speed * deltaSeconds));
        enemy.Position = ResolveCollision(next, enemy.Radius);
        enemy.FacingAngle = MathF.Atan2(vector.Y, vector.X);

        if (enemy.FootstepCooldown <= 0f)
        {
            var stepStrength = _structures.Any(structure => structure.Kind == StructureKind.HoneyTrap && structure.Cell == cell) ? 1.05f : 0.6f;
            EmitRipple(enemy.Position, stepStrength, Color.FromArgb(255, 255, 180, 110));
            enemy.FootstepCooldown = 0.48f;
        }
    }

    private Actor? PickBestTarget(PointF origin, float range, ActorType sourceType)
    {
        IEnumerable<Actor> candidates = sourceType == ActorType.Enemy
            ? LiveDefenders()
            : _enemies.Where(actor => actor.IsAlive);

        return candidates
            .Where(actor => Distance(origin, actor.Position) <= range && HasLineOfSight(origin, actor.Position))
            .OrderBy(actor => Distance(origin, actor.Position))
            .FirstOrDefault();
    }

    private Actor? PickEnemyTarget(Actor enemy)
    {
        var defenders = LiveDefenders()
            .Where(actor => Distance(enemy.Position, actor.Position) <= _weaponStats[enemy.Weapon].ProjectileRange + 30f)
            .OrderBy(actor => Distance(enemy.Position, actor.Position))
            .ToList();

        return defenders.FirstOrDefault(actor => HasLineOfSight(enemy.Position, actor.Position));
    }

    private Actor? PickRaycastTarget(PointF origin, PointF targetPoint, float range)
    {
        var direction = new PointF(targetPoint.X - origin.X, targetPoint.Y - origin.Y);
        var length = MathF.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y));
        if (length <= 1f)
        {
            return null;
        }

        direction = new PointF(direction.X / length, direction.Y / length);

        var bestDistance = float.MaxValue;
        Actor? best = null;

        foreach (var enemy in _enemies.Where(actor => actor.IsAlive))
        {
            var toEnemy = new PointF(enemy.Position.X - origin.X, enemy.Position.Y - origin.Y);
            var projection = (toEnemy.X * direction.X) + (toEnemy.Y * direction.Y);
            if (projection < 0f || projection > range)
            {
                continue;
            }

            var closest = new PointF(origin.X + (direction.X * projection), origin.Y + (direction.Y * projection));
            if (Distance(closest, enemy.Position) <= enemy.Radius + 5f && projection < bestDistance && HasLineOfSight(origin, enemy.Position))
            {
                best = enemy;
                bestDistance = projection;
            }
        }

        return best;
    }

    private bool PlayerCanSee(Actor enemy)
    {
        if (!_player.IsAlive)
        {
            return false;
        }

        var weapon = _weaponStats[_player.Weapon];
        var vector = new PointF(enemy.Position.X - _player.Position.X, enemy.Position.Y - _player.Position.Y);
        var distance = MathF.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y));
        if (distance > weapon.VisionRange)
        {
            return false;
        }

        var angle = MathF.Atan2(vector.Y, vector.X);
        var difference = NormalizeAngle(angle - _player.FacingAngle);
        if (MathF.Abs(difference) > DegreesToRadians(FovDegrees / 2f))
        {
            return false;
        }

        if (!HasLineOfSight(_player.Position, enemy.Position))
        {
            return false;
        }

        if (_structures.Any(structure => structure.Kind == StructureKind.StaticNest && Distance(enemy.Position, CellCenter(structure.Cell)) <= 90f))
        {
            return distance <= 120f;
        }

        return true;
    }

    private bool PlayerCanPerceive(PointF position, float strength)
    {
        if (_phase != GamePhase.Hunt)
        {
            return true;
        }

        var hearing = _player.HearingRange * _weaponStats[_player.Weapon].HearingMultiplier * 1.8f * strength;
        return Distance(_player.Position, position) <= hearing;
    }

    private List<Point> FindPath(Point start, Point goal)
    {
        if (start == goal)
        {
            return [start];
        }

        var frontier = new Queue<Point>();
        frontier.Enqueue(start);

        var cameFrom = new Dictionary<Point, Point?>
        {
            [start] = null,
        };

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var neighbor in Neighbors(current))
            {
                if (cameFrom.ContainsKey(neighbor) || IsBlockedCell(neighbor))
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                if (neighbor == goal)
                {
                    frontier.Clear();
                    break;
                }

                frontier.Enqueue(neighbor);
            }
        }

        if (!cameFrom.ContainsKey(goal))
        {
            return [];
        }

        var path = new List<Point>();
        Point? cursor = goal;
        while (cursor is not null)
        {
            path.Add(cursor.Value);
            cursor = cameFrom[cursor.Value];
        }

        path.Reverse();
        return path;
    }

    private IEnumerable<Point> Neighbors(Point cell)
    {
        var candidates = new[]
        {
            new Point(cell.X + 1, cell.Y),
            new Point(cell.X - 1, cell.Y),
            new Point(cell.X, cell.Y + 1),
            new Point(cell.X, cell.Y - 1),
        };

        foreach (var candidate in candidates)
        {
            if (candidate.X >= 0 && candidate.X < GridColumns && candidate.Y >= 0 && candidate.Y < GridRows)
            {
                yield return candidate;
            }
        }
    }

    private bool IsBlockedCell(Point cell)
    {
        if (_permanentWalls.Contains(cell))
        {
            return true;
        }

        return _structures.Any(structure => structure.Kind == StructureKind.BlastDoor && structure.Cell == cell && structure.Health > 0f);
    }

    private PointF ResolveCollision(PointF desiredPosition, float radius)
    {
        var clamped = new PointF(
            Math.Clamp(desiredPosition.X, WorldBounds.Left + radius + 2f, WorldBounds.Right - radius - 2f),
            Math.Clamp(desiredPosition.Y, WorldBounds.Top + radius + 2f, WorldBounds.Bottom - radius - 2f));

        foreach (var blockedCell in _permanentWalls.Concat(_structures.Where(structure => structure.Kind == StructureKind.BlastDoor && structure.Health > 0f).Select(structure => structure.Cell)))
        {
            var expanded = RectangleF.Inflate(CellRectangle(blockedCell), radius, radius);
            if (!expanded.Contains(clamped))
            {
                continue;
            }

            var center = CellCenter(blockedCell);
            var push = new PointF(clamped.X - center.X, clamped.Y - center.Y);
            var length = MathF.Max(1f, MathF.Sqrt((push.X * push.X) + (push.Y * push.Y)));
            clamped = new PointF(center.X + ((push.X / length) * (CellSize / 2f + radius + 2f)), center.Y + ((push.Y / length) * (CellSize / 2f + radius + 2f)));
        }

        return clamped;
    }

    private bool HasLineOfSight(PointF start, PointF end)
    {
        var distance = Distance(start, end);
        var steps = Math.Max(2, (int)(distance / 8f));

        for (var step = 1; step < steps; step++)
        {
            var progress = step / (float)steps;
            var sample = new PointF(
                start.X + ((end.X - start.X) * progress),
                start.Y + ((end.Y - start.Y) * progress));
            var cell = WorldToCell(sample);
            if (IsBlockedCell(cell))
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerable<Actor> LiveDefenders()
    {
        if (_player.IsAlive)
        {
            yield return _player;
        }

        foreach (var ally in _allies.Where(actor => actor.IsAlive))
        {
            yield return ally;
        }
    }

    private void BuildMapGeometry()
    {
        _permanentWalls.Clear();
        _buildSlots.Clear();
        _spawnCells.Clear();

        for (var x = 0; x < GridColumns; x++)
        {
            _permanentWalls.Add(new Point(x, 0));
            _permanentWalls.Add(new Point(x, GridRows - 1));
        }

        for (var y = 0; y < GridRows; y++)
        {
            _permanentWalls.Add(new Point(0, y));
            _permanentWalls.Add(new Point(GridColumns - 1, y));
        }

        AddWallLine(6, 2, 6, 4);
        AddWallLine(6, 7, 6, 9);
        AddWallLine(9, 2, 9, 4);
        AddWallLine(9, 7, 9, 9);
        AddWallLine(12, 2, 12, 3);
        AddWallLine(12, 8, 12, 9);
        AddWallLine(3, 5, 4, 5);
        AddWallLine(10, 5, 11, 5);

        foreach (var slot in new[]
        {
            new Point(3, 4), new Point(3, 6), new Point(5, 5), new Point(6, 5), new Point(6, 6),
            new Point(7, 4), new Point(7, 7), new Point(8, 5), new Point(8, 6), new Point(9, 5),
            new Point(10, 6), new Point(11, 4), new Point(11, 7), new Point(13, 5), new Point(13, 7),
        })
        {
            if (!_permanentWalls.Contains(slot))
            {
                _buildSlots.Add(slot);
            }
        }

        _spawnCells.AddRange([new Point(1, 2), new Point(1, 5), new Point(1, 9)]);
    }

    private void AddWallLine(int startX, int startY, int endX, int endY)
    {
        for (var x = Math.Min(startX, endX); x <= Math.Max(startX, endX); x++)
        {
            for (var y = Math.Min(startY, endY); y <= Math.Max(startY, endY); y++)
            {
                _permanentWalls.Add(new Point(x, y));
            }
        }
    }

    private static bool IsPerimeterCell(Point cell)
    {
        return cell.X == 0 || cell.Y == 0 || cell.X == GridColumns - 1 || cell.Y == GridRows - 1;
    }

    private Structure CreateStructure(BuildToolKind tool, Point cell)
    {
        return tool switch
        {
            BuildToolKind.BlastDoor => new Structure
            {
                Kind = StructureKind.BlastDoor,
                Cell = cell,
                APCost = 2,
                Label = "防壁ドア",
                Health = 120f,
                MaxHealth = 120f,
                PulseCooldown = 0f,
            },
            BuildToolKind.HoneyTrap => new Structure
            {
                Kind = StructureKind.HoneyTrap,
                Cell = cell,
                APCost = 3,
                Label = "ハチミツトラップ",
                Health = 1f,
                MaxHealth = 1f,
                PulseCooldown = 0f,
            },
            _ => new Structure
            {
                Kind = StructureKind.StaticNest,
                Cell = cell,
                APCost = 4,
                Label = "スタティックネスト",
                Health = 1f,
                MaxHealth = 1f,
                PulseCooldown = 0.3f,
            },
        };
    }

    private static Dictionary<WeaponType, WeaponStats> CreateWeaponStats()
    {
        return new Dictionary<WeaponType, WeaponStats>
        {
            [WeaponType.SMG] = new()
            {
                Type = WeaponType.SMG,
                Label = "SMG / 聴覚特化",
                Cost = 50,
                VisionRange = 225f,
                HearingMultiplier = 1.35f,
                FireCooldown = 0.12f,
                Damage = 8f,
                MoveSpeed = 230f,
                ProjectileRange = 205f,
            },
            [WeaponType.Rifle] = new()
            {
                Type = WeaponType.Rifle,
                Label = "ライフル / 汎用",
                Cost = 100,
                VisionRange = 320f,
                HearingMultiplier = 1f,
                FireCooldown = 0.22f,
                Damage = 15f,
                MoveSpeed = 210f,
                ProjectileRange = 300f,
            },
            [WeaponType.Sniper] = new()
            {
                Type = WeaponType.Sniper,
                Label = "SR / 視界特化",
                Cost = 150,
                VisionRange = 470f,
                HearingMultiplier = 0.75f,
                FireCooldown = 0.68f,
                Damage = 36f,
                MoveSpeed = 185f,
                ProjectileRange = 430f,
            },
        };
    }

    private void EmitRipple(PointF position, float strength, Color color)
    {
        _ripples.Add(new Ripple
        {
            Position = position,
            Strength = strength,
            Lifetime = 0.9f,
            Color = color,
        });
    }

    private Point ScreenToCell(Point location)
    {
        var worldPoint = ScreenToWorldPoint(location);
        return new Point(
            (int)MathF.Floor((worldPoint.X - WorldBounds.Left) / CellSize),
            (int)MathF.Floor((worldPoint.Y - WorldBounds.Top) / CellSize));
    }

    private bool TryGetWorldPointFromScreen(Point screenPoint, out PointF worldPoint)
    {
        worldPoint = ScreenToWorldPoint(screenPoint);
        return worldPoint.X >= WorldBounds.Left &&
               worldPoint.X < WorldBounds.Right &&
               worldPoint.Y >= WorldBounds.Top &&
               worldPoint.Y < WorldBounds.Bottom;
    }

    private PointF ScreenToWorldPoint(Point screenPoint)
    {
        var points = new[] { new PointF(screenPoint.X, screenPoint.Y) };
        using var projection = CreateWorldProjectionMatrix();
        projection.Invert();
        projection.TransformPoints(points);
        return points[0];
    }

    private Point WorldToCell(PointF point)
    {
        var x = (int)Math.Clamp((point.X - WorldBounds.Left) / CellSize, 0f, GridColumns - 1);
        var y = (int)Math.Clamp((point.Y - WorldBounds.Top) / CellSize, 0f, GridRows - 1);
        return new Point(x, y);
    }

    private Rectangle CellRectangle(Point cell)
    {
        return new Rectangle(WorldBounds.Left + (cell.X * CellSize), WorldBounds.Top + (cell.Y * CellSize), CellSize, CellSize);
    }

    private PointF CellCenter(Point cell)
    {
        return new PointF(
            WorldBounds.Left + (cell.X * CellSize) + (CellSize / 2f),
            WorldBounds.Top + (cell.Y * CellSize) + (CellSize / 2f));
    }

    private PointF CorePosition()
    {
        return CellCenter(new Point(14, 6));
    }

    private Matrix CreateWorldProjectionMatrix()
    {
        return new Matrix(
            WorldPerspectiveScaleX,
            0f,
            WorldPerspectiveShearX,
            WorldPerspectiveScaleY,
            WorldVisualBounds.Left - (WorldPerspectiveScaleX * WorldBounds.Left) - (WorldPerspectiveShearX * WorldBounds.Top),
            WorldVisualBounds.Top - (WorldPerspectiveScaleY * WorldBounds.Top));
    }

    private PointF[] GetProjectedWorldCorners()
    {
        var points = new[]
        {
            new PointF(WorldBounds.Left, WorldBounds.Top),
            new PointF(WorldBounds.Right, WorldBounds.Top),
            new PointF(WorldBounds.Right, WorldBounds.Bottom),
            new PointF(WorldBounds.Left, WorldBounds.Bottom),
        };

        using var projection = CreateWorldProjectionMatrix();
        projection.TransformPoints(points);
        return points;
    }

    private string BuildToolLabel(BuildToolKind tool)
    {
        return tool switch
        {
            BuildToolKind.BlastDoor => "防壁ドア / 2AP",
            BuildToolKind.HoneyTrap => "ハチミツトラップ / 3AP",
            _ => "スタティックネスト / 4AP",
        };
    }

    private string PhaseLabel()
    {
        return _phase switch
        {
            GamePhase.Construct => "構築",
            GamePhase.Bet => "賭け",
            GamePhase.Hunt => "狩り",
            GamePhase.RoundResult => "精算",
            GamePhase.Victory => "勝利",
            _ => "敗北",
        };
    }

    private Color PhaseColor()
    {
        return _phase switch
        {
            GamePhase.Construct => Color.FromArgb(255, 115, 225, 205),
            GamePhase.Bet => Color.FromArgb(255, 255, 225, 130),
            GamePhase.Hunt => Color.FromArgb(255, 255, 125, 105),
            GamePhase.Victory => Color.FromArgb(255, 120, 235, 165),
            GamePhase.Defeat => Color.FromArgb(255, 255, 105, 95),
            _ => Color.FromArgb(255, 205, 215, 225),
        };
    }

    private static float Distance(PointF left, PointF right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    private static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);

    private static float RadiansToDegrees(float radians) => radians * (180f / MathF.PI);

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI)
        {
            angle -= MathF.PI * 2f;
        }

        while (angle < -MathF.PI)
        {
            angle += MathF.PI * 2f;
        }

        return angle;
    }
}
