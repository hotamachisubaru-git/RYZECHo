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
    private const int WorldMargin = 24;
    private const int TopBarHeight = 52;
    private const int SidePanelGap = 20;
    private const int SidePanelWidth = 364;
    private const int BottomHudHeight = 146;
    private const int GridColumns = 18;
    private const int GridRows = 12;
    private const int CellSize = 56;
    private const int TotalRounds = 3;
    private const float FovDegrees = 120f;

    private readonly Random _random = new();
    private readonly Dictionary<WeaponType, WeaponStats> _weaponStats = CreateWeaponStats();
    private readonly List<Structure> _structures = [];
    private readonly List<Ripple> _ripples = [];
    private readonly List<Actor> _allies = [];
    private readonly List<Actor> _enemies = [];
    private readonly HashSet<Point> _permanentWalls = [];
    private readonly HashSet<Point> _buildSlots = [];
    private readonly List<Point> _spawnCells = [];

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

    private Rectangle WorldBounds => new(WorldMargin, 88, GridColumns * CellSize, GridRows * CellSize);

    private Rectangle TopBarBounds => new(WorldMargin, 20, WorldBounds.Width + SidePanelGap + SidePanelWidth, TopBarHeight);

    private Rectangle BottomHudBounds => new(WorldMargin, WorldBounds.Bottom + 18, WorldBounds.Width, BottomHudHeight);

    private Rectangle SidePanelBounds => new(WorldBounds.Right + SidePanelGap, WorldBounds.Top, SidePanelWidth, WorldBounds.Height);

    private Rectangle RosterBounds => new(SidePanelBounds.Left, SidePanelBounds.Top, SidePanelWidth, 228);

    private Rectangle IntelBounds => new(SidePanelBounds.Left, RosterBounds.Bottom + 18, SidePanelWidth, 168);

    private Rectangle MinimapBounds => new(SidePanelBounds.Left, SidePanelBounds.Bottom - 238, SidePanelWidth, 238);

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

        var aimVector = new PointF(input.MousePosition.X - _player.Position.X, input.MousePosition.Y - _player.Position.Y);
        if (Math.Abs(aimVector.X) > 0.01f || Math.Abs(aimVector.Y) > 0.01f)
        {
            _player.FacingAngle = MathF.Atan2(aimVector.Y, aimVector.X);
        }

        if (input.FireHeld && _player.FireCooldown <= 0f)
        {
            var target = PickRaycastTarget(_player.Position, input.MousePosition, weapon.ProjectileRange);
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
        using var background = new LinearGradientBrush(clientBounds, Color.FromArgb(5, 10, 14), Color.FromArgb(12, 22, 30), 90f);
        graphics.FillRectangle(background, clientBounds);

        DrawWorldPanel(graphics);
        DrawStructures(graphics);
        DrawCore(graphics);
        DrawRipples(graphics);
        DrawActors(graphics, mousePosition);
        DrawHud(graphics);

        if (_showBriefing)
        {
            DrawBriefingOverlay(graphics);
        }
    }

    private void DrawWorldPanel(Graphics graphics)
    {
        using var worldBrush = new LinearGradientBrush(WorldBounds, Color.FromArgb(8, 15, 22), Color.FromArgb(16, 28, 35), 45f);
        graphics.FillRectangle(worldBrush, WorldBounds);

        using var gridPen = new Pen(Color.FromArgb(24, 110, 120, 125), 1f);
        for (var column = 0; column <= GridColumns; column++)
        {
            var x = WorldBounds.Left + (column * CellSize);
            graphics.DrawLine(gridPen, x, WorldBounds.Top, x, WorldBounds.Bottom);
        }

        for (var row = 0; row <= GridRows; row++)
        {
            var y = WorldBounds.Top + (row * CellSize);
            graphics.DrawLine(gridPen, WorldBounds.Left, y, WorldBounds.Right, y);
        }

        foreach (var cell in _permanentWalls)
        {
            var rectangle = CellRectangle(cell);
            using var wallBrush = new SolidBrush(Color.FromArgb(32, 48, 60));
            graphics.FillRectangle(wallBrush, rectangle);
            using var edgePen = new Pen(Color.FromArgb(80, 130, 145), 2f);
            graphics.DrawRectangle(edgePen, rectangle);
        }

        foreach (var slot in _buildSlots)
        {
            var rectangle = CellRectangle(slot);
            using var outlinePen = new Pen(Color.FromArgb(70, 45, 210, 205), 2f)
            {
                DashStyle = DashStyle.Dash,
            };

            graphics.DrawRectangle(outlinePen, rectangle);
        }

        using var borderPen = new Pen(Color.FromArgb(80, 115, 210, 220), 2f);
        graphics.DrawRectangle(borderPen, WorldBounds);
    }

    private void DrawStructures(Graphics graphics)
    {
        foreach (var structure in _structures)
        {
            var rectangle = Rectangle.Inflate(CellRectangle(structure.Cell), -6, -6);

            switch (structure.Kind)
            {
                case StructureKind.BlastDoor:
                    using (var fill = new SolidBrush(Color.FromArgb(60, 105, 235, 240)))
                    using (var border = new Pen(Color.FromArgb(215, 240, 250), 2f))
                    {
                        graphics.FillRectangle(fill, rectangle);
                        graphics.DrawRectangle(border, rectangle);
                    }

                    var ratio = structure.Health / structure.MaxHealth;
                    using (var hpBack = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    using (var hpFill = new SolidBrush(Color.FromArgb(220, 80, 220, 165)))
                    {
                        var hpRect = new RectangleF(rectangle.Left, rectangle.Bottom + 3f, rectangle.Width, 5f);
                        graphics.FillRectangle(hpBack, hpRect);
                        graphics.FillRectangle(hpFill, hpRect.Left, hpRect.Top, hpRect.Width * ratio, hpRect.Height);
                    }
                    break;
                case StructureKind.HoneyTrap:
                    using (var fill = new SolidBrush(Color.FromArgb(175, 245, 177, 55)))
                    using (var pen = new Pen(Color.FromArgb(255, 255, 225, 145), 2f))
                    {
                        graphics.FillEllipse(fill, rectangle);
                        graphics.DrawEllipse(pen, rectangle);
                    }
                    break;
                case StructureKind.StaticNest:
                    using (var fill = new SolidBrush(Color.FromArgb(110, 120, 220, 90)))
                    using (var pen = new Pen(Color.FromArgb(225, 210, 255, 135), 2f))
                    {
                        graphics.FillEllipse(fill, rectangle);
                        graphics.DrawEllipse(pen, rectangle);
                    }

                    using (var auraPen = new Pen(Color.FromArgb(70, 165, 225, 115), 1.5f))
                    {
                        graphics.DrawEllipse(auraPen, rectangle.Left - 20, rectangle.Top - 20, rectangle.Width + 40, rectangle.Height + 40);
                    }
                    break;
            }
        }
    }

    private void DrawCore(Graphics graphics)
    {
        var coreCenter = CorePosition();
        var coreRect = new RectangleF(coreCenter.X - 24f, coreCenter.Y - 24f, 48f, 48f);

        using var glow = new SolidBrush(Color.FromArgb(60, 70, 225, 240));
        graphics.FillEllipse(glow, coreCenter.X - 42f, coreCenter.Y - 42f, 84f, 84f);

        using var fill = new SolidBrush(Color.FromArgb(220, 55, 180, 210));
        using var border = new Pen(Color.FromArgb(235, 210, 255, 255), 2.5f);
        graphics.FillEllipse(fill, coreRect);
        graphics.DrawEllipse(border, coreRect);

        var ratio = _coreHealth / 180f;
        using var hpBack = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        using var hpFill = new SolidBrush(Color.FromArgb(225, 70, 220, 185));
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

            using var pen = new Pen(color, 2f);
            graphics.DrawEllipse(pen, ripple.Position.X - radius, ripple.Position.Y - radius, radius * 2f, radius * 2f);
        }
    }

    private void DrawActors(Graphics graphics, Point mousePosition)
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

    private void DrawActor(Graphics graphics, Actor actor, Point mousePosition)
    {
        var center = actor.Position;
        var isPlayer = actor.Type == ActorType.Player;
        var color = actor.IsBoss ? Color.FromArgb(255, 245, 210, 110) : actor.Type == ActorType.Ally ? Color.FromArgb(255, 95, 225, 200) : Color.FromArgb(255, 75, 220, 245);

        using var fill = new SolidBrush(actor.IsAlive ? color : Color.FromArgb(80, 80, 80, 90));
        using var outline = new Pen(Color.FromArgb(220, 240, 250, 250), actor.IsBoss ? 2.8f : 2f);

        graphics.FillEllipse(fill, center.X - actor.Radius, center.Y - actor.Radius, actor.Radius * 2f, actor.Radius * 2f);
        graphics.DrawEllipse(outline, center.X - actor.Radius, center.Y - actor.Radius, actor.Radius * 2f, actor.Radius * 2f);

        var facing = new PointF(center.X + (MathF.Cos(actor.FacingAngle) * 22f), center.Y + (MathF.Sin(actor.FacingAngle) * 22f));
        using var directionPen = new Pen(Color.FromArgb(220, 240, 250, 250), 2f);
        graphics.DrawLine(directionPen, center, facing);

        using var textBrush = new SolidBrush(Color.FromArgb(230, 235, 245, 245));
        using var nameFont = new Font(UiFontFamily, isPlayer ? 11f : 9f, FontStyle.Bold);
        graphics.DrawString(actor.Name, nameFont, textBrush, center.X - 34f, center.Y - actor.Radius - 24f);

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
            CenterColor = Color.FromArgb(70, 100, 240, 255),
            SurroundColors = [Color.FromArgb(0, 100, 240, 255)],
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

        using var cuePen = new Pen(Color.FromArgb((int)(205 * strength), 255, 180, 110), 2.6f);
        graphics.DrawEllipse(cuePen, anchor.X - ring, anchor.Y - ring, ring * 2f, ring * 2f);
        graphics.DrawEllipse(cuePen, anchor.X - ring - 7f, anchor.Y - ring - 7f, (ring * 2f) + 14f, (ring * 2f) + 14f);
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
        DrawHudText(graphics, "MOBA 風 HUD に合わせて、上段で戦況、右側でロスターとミニマップ、下段で装備と操作を確認できます。", 11f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 22, box.Top + 66);
        DrawHudText(graphics, "構築は最初の一度だけ。以降は毎ラウンドごとにボスを指名し、賭け金を積み、120 度の視界でコアを守り抜いてください。", 11f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 22, box.Top + 94);
        DrawHudText(graphics, "ハチミツトラップは足音を増幅し、スタティックネストは偽の波紋を撒いて視界を乱します。", 11f, FontStyle.Regular, Color.FromArgb(220, 195, 215, 222), box.Left + 22, box.Top + 122);
        DrawHudText(graphics, "スペースキーでこのパネルを閉じます。", 10f, FontStyle.Bold, Color.FromArgb(255, 255, 215, 135), box.Left + 22, box.Bottom - 36);
    }

    private void DrawTopBar(Graphics graphics)
    {
        var left = TopBarBounds.Left + 18;
        var center = TopBarBounds.Left + (TopBarBounds.Width / 2);
        var right = TopBarBounds.Right - 18;
        var enemiesLeft = _pendingEnemies + _enemies.Count(enemy => enemy.IsAlive);

        DrawHudText(graphics, "RYZECHØ // タクティカル・エコノミー・ハイディング FPS", 14f, FontStyle.Bold, Color.FromArgb(235, 225, 248, 255), left, TopBarBounds.Top + 9);
        DrawHudText(graphics, $"防衛班  3人", 11f, FontStyle.Bold, Color.FromArgb(255, 115, 225, 205), left, TopBarBounds.Top + 29);
        DrawHudText(graphics, $"資金 {_credits}", 11f, FontStyle.Bold, Color.FromArgb(255, 255, 225, 140), left + 124, TopBarBounds.Top + 29);
        DrawHudText(graphics, $"コア {(int)_coreHealth}", 11f, FontStyle.Bold, Color.FromArgb(255, 85, 215, 185), left + 232, TopBarBounds.Top + 29);

        DrawHudText(graphics, $"第{Math.Min(_currentRound, TotalRounds)}/{TotalRounds}ラウンド", 16f, FontStyle.Bold, Color.FromArgb(255, 245, 240, 210), center - 86, TopBarBounds.Top + 8);
        DrawHudText(graphics, $"フェーズ {PhaseLabel()}   残り {_roundTimer:0.0} 秒", 11f, FontStyle.Bold, PhaseColor(), center - 86, TopBarBounds.Top + 30);

        using var topRightFont = new Font(UiFontFamily, 12f, FontStyle.Bold);
        var rightTextSize = graphics.MeasureString($"残敵 {enemiesLeft}", topRightFont);
        DrawHudText(graphics, $"残敵 {enemiesLeft}", 12f, FontStyle.Bold, Color.FromArgb(255, 255, 125, 105), right - rightTextSize.Width, TopBarBounds.Top + 10);
        DrawHudText(graphics, $"ボス {_selectedBossName}", 11f, FontStyle.Bold, Color.FromArgb(255, 248, 214, 130), right - 160, TopBarBounds.Top + 30);
    }

    private void DrawRosterPanel(Graphics graphics)
    {
        DrawPanelTitle(graphics, RosterBounds, "味方ロスター");

        var y = RosterBounds.Top + 46;
        foreach (var actor in new[] { _player }.Concat(_allies))
        {
            var row = new Rectangle(RosterBounds.Left + 14, y, RosterBounds.Width - 28, 54);
            var accent = actor.IsBoss ? Color.FromArgb(255, 245, 210, 110) : actor.Type == ActorType.Player ? Color.FromArgb(255, 95, 225, 245) : Color.FromArgb(255, 95, 225, 200);
            using var fill = new SolidBrush(Color.FromArgb(82, 18, 28, 36));
            using var border = new Pen(Color.FromArgb(140, accent), actor.IsBoss ? 2.4f : 1.8f);
            graphics.FillRectangle(fill, row);
            graphics.DrawRectangle(border, row);

            using var orbBrush = new SolidBrush(accent);
            graphics.FillEllipse(orbBrush, row.Left + 10, row.Top + 12, 28, 28);

            DrawHudText(graphics, actor.Name, 10.5f, FontStyle.Bold, Color.FromArgb(240, 238, 244, 248), row.Left + 48, row.Top + 8);
            DrawHudText(graphics, actor.IsBoss ? "賞金首" : "防衛ユニット", 8.8f, FontStyle.Bold, Color.FromArgb(220, accent), row.Left + 48, row.Top + 28);
            DrawHudText(graphics, _weaponStats[actor.Weapon].Label, 8.8f, FontStyle.Regular, Color.FromArgb(220, 190, 210, 220), row.Right - 112, row.Top + 8);

            var hpRatio = actor.Health / actor.MaxHealth;
            using var hpBack = new SolidBrush(Color.FromArgb(55, 0, 0, 0));
            using var hpFill = new SolidBrush(Color.FromArgb(235, 82, 220, 170));
            var hpRect = new RectangleF(row.Left + 48, row.Bottom - 14, row.Width - 62, 6f);
            graphics.FillRectangle(hpBack, hpRect);
            graphics.FillRectangle(hpFill, hpRect.Left, hpRect.Top, hpRect.Width * Math.Clamp(hpRatio, 0f, 1f), hpRect.Height);

            y += 62;
        }
    }

    private void DrawIntelPanel(Graphics graphics)
    {
        DrawPanelTitle(graphics, IntelBounds, "戦況フィード");

        var body = _phase switch
        {
            GamePhase.Construct => $"構築AP: {_buildPoints}\n選択中: {BuildToolLabel(_selectedBuildTool)}\nクリックで設置、右クリックで撤去。",
            GamePhase.Bet => $"指名ボス: {_selectedBossName}\n賭け金: {_selectedBet}\n購入武器: {_weaponStats[_selectedWeapon].Label}",
            GamePhase.Hunt => $"残敵: {_pendingEnemies + _enemies.Count(enemy => enemy.IsAlive)}\n視界: {_weaponStats[_player.Weapon].VisionRange:0}\n聴覚倍率: x{_weaponStats[_player.Weapon].HearingMultiplier:0.0}",
            GamePhase.RoundResult => _resultMessage,
            GamePhase.Victory => "三ラウンド連続で防衛成功。再挑戦は R。",
            _ => "コアが陥落しました。再挑戦は R。",
        };

        using var font = new Font(UiFontFamily, 10.2f, FontStyle.Regular);
        using var brush = new SolidBrush(Color.FromArgb(228, 208, 220, 228));
        var bodyRect = new RectangleF(IntelBounds.Left + 16, IntelBounds.Top + 44, IntelBounds.Width - 32, 56);
        graphics.DrawString(body, font, brush, bodyRect);

        using var feedFont = new Font(UiFontFamily, 9.2f, FontStyle.Bold);
        using var feedBrush = new SolidBrush(Color.FromArgb(235, 248, 214, 130));
        graphics.DrawString(_resultMessage, feedFont, feedBrush, new RectangleF(IntelBounds.Left + 16, IntelBounds.Top + 106, IntelBounds.Width - 32, 48));
    }

    private void DrawMiniMap(Graphics graphics)
    {
        DrawPanelTitle(graphics, MinimapBounds, "戦術マップ");

        var inner = Rectangle.Inflate(MinimapBounds, -16, -18);
        inner = new Rectangle(inner.Left, inner.Top + 18, inner.Width, inner.Height - 18);

        using var mapBrush = new SolidBrush(Color.FromArgb(150, 9, 16, 22));
        graphics.FillRectangle(mapBrush, inner);

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

        var core = CorePosition();
        using var coreBrush = new SolidBrush(Color.FromArgb(255, 78, 220, 195));
        var corePoint = new PointF(inner.Left + ((core.X - WorldBounds.Left) * scaleX), inner.Top + ((core.Y - WorldBounds.Top) * scaleY));
        graphics.FillEllipse(coreBrush, corePoint.X - 5f, corePoint.Y - 5f, 10f, 10f);

        using var border = new Pen(Color.FromArgb(110, 120, 220, 235), 1.6f);
        graphics.DrawRectangle(border, inner);
        DrawHudText(graphics, "青=味方  赤=感知敵  緑=コア", 8.6f, FontStyle.Regular, Color.FromArgb(220, 190, 210, 220), MinimapBounds.Left + 16, MinimapBounds.Bottom - 20);
    }

    private void DrawBottomBar(Graphics graphics)
    {
        var portraitRect = new Rectangle(BottomHudBounds.Left + 16, BottomHudBounds.Top + 14, 190, BottomHudBounds.Height - 28);
        var commandRect = new Rectangle(portraitRect.Right + 16, BottomHudBounds.Top + 14, 426, BottomHudBounds.Height - 28);
        var loadoutRect = new Rectangle(commandRect.Right + 16, BottomHudBounds.Top + 14, BottomHudBounds.Right - commandRect.Right - 32, BottomHudBounds.Height - 28);

        DrawInsetPanel(graphics, portraitRect);
        DrawInsetPanel(graphics, commandRect);
        DrawInsetPanel(graphics, loadoutRect);

        using var avatarBrush = new SolidBrush(Color.FromArgb(255, 80, 220, 245));
        graphics.FillEllipse(avatarBrush, portraitRect.Left + 16, portraitRect.Top + 18, 54, 54);
        DrawHudText(graphics, "あなた", 12f, FontStyle.Bold, Color.FromArgb(240, 238, 244, 248), portraitRect.Left + 84, portraitRect.Top + 18);
        DrawHudText(graphics, _player.IsBoss ? "現在の賞金首" : $"指名ボス: {_selectedBossName}", 9.6f, FontStyle.Bold, Color.FromArgb(255, 248, 214, 130), portraitRect.Left + 84, portraitRect.Top + 42);
        DrawHudText(graphics, $"体力 {(int)_player.Health}/{(int)_player.MaxHealth}", 9.5f, FontStyle.Regular, Color.FromArgb(228, 208, 220, 228), portraitRect.Left + 16, portraitRect.Top + 86);
        DrawHudText(graphics, $"武器 {_weaponStats[_player.Weapon].Label}", 9.5f, FontStyle.Regular, Color.FromArgb(228, 208, 220, 228), portraitRect.Left + 16, portraitRect.Top + 108);

        DrawHudText(graphics, CurrentModeTitle(), 12f, FontStyle.Bold, PhaseColor(), commandRect.Left + 14, commandRect.Top + 12);
        using (var bodyFont = new Font(UiFontFamily, 10.3f, FontStyle.Regular))
        using (var bodyBrush = new SolidBrush(Color.FromArgb(230, 210, 224, 232)))
        {
            graphics.DrawString(CurrentModeBody(), bodyFont, bodyBrush, new RectangleF(commandRect.Left + 14, commandRect.Top + 38, commandRect.Width - 28, 50));
        }

        DrawHudText(graphics, CurrentControlsHint(), 9.3f, FontStyle.Bold, Color.FromArgb(255, 255, 225, 150), commandRect.Left + 14, commandRect.Bottom - 26);

        if (_phase == GamePhase.Construct)
        {
            DrawChoiceCard(graphics, new Rectangle(loadoutRect.Left + 12, loadoutRect.Top + 14, 112, 92), "1", "防壁ドア", "2AP / 通路封鎖", _selectedBuildTool == BuildToolKind.BlastDoor, Color.FromArgb(255, 105, 235, 240));
            DrawChoiceCard(graphics, new Rectangle(loadoutRect.Left + 130, loadoutRect.Top + 14, 112, 92), "2", "ハチミツ", "3AP / 鈍足+増音", _selectedBuildTool == BuildToolKind.HoneyTrap, Color.FromArgb(255, 255, 196, 82));
            DrawChoiceCard(graphics, new Rectangle(loadoutRect.Left + 248, loadoutRect.Top + 14, 112, 92), "3", "ネスト", "4AP / 偽波紋", _selectedBuildTool == BuildToolKind.StaticNest, Color.FromArgb(255, 180, 235, 120));
        }
        else
        {
            DrawWeaponChoice(graphics, new Rectangle(loadoutRect.Left + 12, loadoutRect.Top + 14, 112, 92), "Q", WeaponType.SMG, ActiveSelection(WeaponType.SMG));
            DrawWeaponChoice(graphics, new Rectangle(loadoutRect.Left + 130, loadoutRect.Top + 14, 112, 92), "E", WeaponType.Rifle, ActiveSelection(WeaponType.Rifle));
            DrawWeaponChoice(graphics, new Rectangle(loadoutRect.Left + 248, loadoutRect.Top + 14, 112, 92), "R", WeaponType.Sniper, ActiveSelection(WeaponType.Sniper));
        }

        DrawHudText(graphics, "スペース: 説明表示切替   R: 勝敗後に再挑戦", 9.2f, FontStyle.Regular, Color.FromArgb(220, 190, 210, 220), loadoutRect.Left + 12, loadoutRect.Bottom - 22);
    }

    private void DrawHudText(Graphics graphics, string text, float size, FontStyle style, Color color, float x, float y)
    {
        using var font = new Font(UiFontFamily, size, style);
        using var brush = new SolidBrush(color);
        graphics.DrawString(text, font, brush, x, y);
    }

    private void DrawPanelFrame(Graphics graphics, Rectangle bounds)
    {
        using var fill = new SolidBrush(Color.FromArgb(170, 6, 14, 20));
        using var border = new Pen(Color.FromArgb(110, 90, 215, 230), 2f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);
    }

    private void DrawInsetPanel(Graphics graphics, Rectangle bounds)
    {
        using var fill = new SolidBrush(Color.FromArgb(78, 10, 18, 24));
        using var border = new Pen(Color.FromArgb(85, 96, 175, 195), 1.6f);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(border, bounds);
    }

    private void DrawPanelTitle(Graphics graphics, Rectangle bounds, string title)
    {
        DrawHudText(graphics, title, 11.4f, FontStyle.Bold, Color.FromArgb(255, 245, 220, 155), bounds.Left + 14, bounds.Top + 12);
        using var accent = new Pen(Color.FromArgb(120, 92, 212, 225), 1.6f);
        graphics.DrawLine(accent, bounds.Left + 14, bounds.Top + 34, bounds.Right - 14, bounds.Top + 34);
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
            GamePhase.Construct => $"AP を使って初期陣地を構築します。現在の選択は {BuildToolLabel(_selectedBuildTool)}。この配置は全ラウンド共通です。",
            GamePhase.Bet => $"{_selectedBossName} を賞金首に指名中。賭け金 {_selectedBet}、購入武器は {_weaponStats[_selectedWeapon].Label}。勝利かつボス生存で投資額が倍化します。",
            GamePhase.Hunt => $"視界は 120 度固定。死角の敵は音の波紋で拾います。残り {_roundTimer:0.0} 秒、感知中の敵は {_pendingEnemies + _enemies.Count(enemy => enemy.IsAlive)} 体です。",
            GamePhase.RoundResult => _resultMessage,
            GamePhase.Victory => "全ラウンドの防衛に成功しました。構築・賭け・音索敵のループが一通り遊べる状態です。",
            _ => "コアが破壊されるか、防衛班が壊滅しました。構築とボス指名を見直して再挑戦してください。",
        };
    }

    private string CurrentControlsHint()
    {
        return _phase switch
        {
            GamePhase.Construct => "1/2/3 で建築物選択  |  左クリック設置  |  右クリック撤去  |  Enter で構築確定",
            GamePhase.Bet => "1/2/3 でボス選択  |  Q/E で武器変更  |  A/D で賭け金調整  |  Enter で出撃",
            GamePhase.Hunt => "WASD 移動  |  マウス照準  |  左クリック長押し射撃",
            _ => "Enter で進行  |  R で再挑戦",
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

    private void TryPlaceStructure(Point location)
    {
        if (!WorldBounds.Contains(location))
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
        if (!WorldBounds.Contains(location))
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

    private Actor? PickRaycastTarget(PointF origin, Point targetPoint, float range)
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
        return new Point((location.X - WorldBounds.Left) / CellSize, (location.Y - WorldBounds.Top) / CellSize);
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
