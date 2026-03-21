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
    bool Press4,
    bool PressQ,
    bool PressE,
    bool PressR,
    bool FireHeld,
    bool InteractHeld,
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
    Blitz,
    Monster,
    Melt,
    Fairy,
    Giant,
    Juggernaut,
    Violet,
    Changer,
    Howl,
}

internal enum RippleKind
{
    Footstep,
    Gunshot,
    Skill,
}

internal enum TeamRole
{
    Attack,
    Defense,
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
    public required string ShortLabel { get; init; }
    public required string Code { get; init; }
    public required string Category { get; init; }
    public required string VisionClass { get; init; }
    public required int Cost { get; init; }
    public required int MagazineAmmo { get; init; }
    public required int ReserveAmmo { get; init; }
    public required float VisionRange { get; init; }
    public required float HearingMultiplier { get; init; }
    public required float FireCooldown { get; init; }
    public required float Damage { get; init; }
    public required float MoveSpeed { get; init; }
    public required float ProjectileRange { get; init; }
    public required bool ScopedFov { get; init; }
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
    public required RippleKind Kind { get; init; }
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
    public required float MaxShield { get; init; }
    public required float HearingRange { get; init; }
    public required float BaseMoveSpeed { get; init; }
    public float Health { get; set; }
    public float Shield { get; set; }
    public float ShieldRegenDelay { get; set; }
    public float FireCooldown { get; set; }
    public float PathCooldown { get; set; }
    public float FootstepCooldown { get; set; }
    public int FootstepPulseIndex { get; set; }
    public float FacingAngle { get; set; }
    public bool IsBoss { get; set; }
    public Queue<PointF> Path { get; } = new();
    public bool IsAlive => Health > 0.01f;
}

internal sealed partial class GameModel
{
    private const string UiFontFamily = "Yu Gothic UI";
    private const int DefaultClientWidth = 1440;
    private const int DefaultClientHeight = 960;
    private const int WorldMargin = 24;
    private const int TopBarHeight = 56;
    private const int SidePanelGap = 20;
    private const int SidePanelWidth = 280;
    private const int BottomHudHeight = 132;
    private const int GridColumns = 18;
    private const int GridRows = 12;
    private const int CellSize = 56;
    private const int RoundsToWin = 7;
    private const int RegulationSideSwitchRound = 4;
    private const int OvertimeTriggerScore = 6;
    private const int TeamSize = 4;
    private const int StartingCredits = 1000;
    private const int WinRewardCredits = 2200;
    private const int LossRewardCredits = 1200;
    private const int KillRewardCredits = 400;
    private const int ObjectiveRewardCredits = 350;
    private const int BossKillDividendCredits = 200;
    private const int BossEliminationBonusCredits = 800;
    private const int MaxBossSelectionsPerActor = 2;
    private const int OptimalBossInvestment = 300;
    private const float DefaultFovDegrees = 120f;
    private const float SniperFovDegrees = 100f;
    private const float SoundCueLifetimeSeconds = 0.3f;
    private const float RoundDurationSeconds = 50f;
    private const float BombPlantSeconds = 3f;
    private const float BombFuseSeconds = 35f;
    private const float BombDefuseSeconds = 8f;
    private const float BombSiteRadius = 28f;
    private const float WorldPerspectiveScaleX = 0.84f;
    private const float WorldPerspectiveScaleY = 0.78f;
    private const float WorldPerspectiveShearX = 0.22f;
    private const float WorldPerspectiveTopInset = 10f;
    private const float HuntCameraZoom = 1.32f;
    private const float HuntCameraTargetX = 0.46f;
    private const float HuntCameraTargetY = 0.64f;

    private readonly Random _random = new();
    private readonly Dictionary<WeaponType, WeaponStats> _weaponStats = CreateWeaponStats();
    private readonly List<Structure> _structures = [];
    private readonly List<Ripple> _ripples = [];
    private readonly List<Actor> _allies = [];
    private readonly List<Actor> _enemies = [];
    private readonly HashSet<Point> _permanentWalls = [];
    private readonly HashSet<Point> _buildSlots = [];
    private readonly HashSet<Point> _noBuildZones = [];
    private readonly List<Point> _spawnCells = [];
    private readonly List<string> _activityFeed = [];
    private readonly Dictionary<string, int> _bossSelectionCounts = [];
    private readonly ProgressProfile _profile = LoadProgressProfile();
    private Size _layoutSize = new(DefaultClientWidth, DefaultClientHeight);

    private GamePhase _phase = GamePhase.Construct;
    private BuildToolKind _selectedBuildTool = BuildToolKind.BlastDoor;
    private WeaponType _selectedWeapon = WeaponType.Giant;
    private int _buildPoints = 12;
    private int _credits = StartingCredits;
    private int _currentRound = 1;
    private int _playerRoundWins;
    private int _enemyRoundWins;
    private int _selectedBet = OptimalBossInvestment;
    private int _enemyBossInvestment;
    private int _matchTeamEliminations;
    private int _matchPlayerDeaths;
    private int _roundBossKillCount;
    private float _roundTimer;
    private float _pingCooldown;
    private float _resultTimer;
    private float _coreHealth;
    private float _bombPlantProgress;
    private float _bombDefuseProgress;
    private float _playerIdleSeconds;
    private float _uiPulseTime;
    private GamePhase _resultDestination = GamePhase.Bet;
    private string _selectedBossName = "あなた";
    private string _lastProgressionSummary = string.Empty;
    private string _resultMessage = "最初の構築が、全ラウンドを支配する。";
    private bool _showBriefing = true;
    private bool _bombPlanted;
    private bool _isOvertime;
    private bool _sideSwapConstructPending;
    private Actor? _activePlanter;
    private TeamRole _playerTeamRole = TeamRole.Defense;

    private readonly Actor _player;

    public GameModel()
    {
        BuildMapGeometry();

        _player = new Actor
        {
            Name = "あなた",
            Type = ActorType.Player,
            HomeCell = new Point(13, 6),
            Weapon = WeaponType.Giant,
            Position = CellCenter(new Point(13, 6)),
            Radius = 14f,
            MaxHealth = 100f,
            MaxShield = 60f,
            Health = 100f,
            Shield = 60f,
            HearingRange = 350f,
            BaseMoveSpeed = 210f,
        };

        _allies.Add(new Actor
        {
            Name = "北アンカー",
            Type = ActorType.Ally,
            HomeCell = new Point(13, 4),
            Weapon = WeaponType.Violet,
            Position = CellCenter(new Point(13, 4)),
            Radius = 13f,
            MaxHealth = 95f,
            MaxShield = 42f,
            Health = 95f,
            Shield = 42f,
            HearingRange = 300f,
            BaseMoveSpeed = 168f,
        });

        _allies.Add(new Actor
        {
            Name = "南アンカー",
            Type = ActorType.Ally,
            HomeCell = new Point(13, 8),
            Weapon = WeaponType.Blitz,
            Position = CellCenter(new Point(13, 8)),
            Radius = 13f,
            MaxHealth = 95f,
            MaxShield = 36f,
            Health = 95f,
            Shield = 36f,
            HearingRange = 420f,
            BaseMoveSpeed = 188f,
        });

        _allies.Add(new Actor
        {
            Name = "中央リンク",
            Type = ActorType.Ally,
            HomeCell = new Point(12, 6),
            Weapon = WeaponType.Fairy,
            Position = CellCenter(new Point(12, 6)),
            Radius = 13f,
            MaxHealth = 95f,
            MaxShield = 48f,
            Health = 95f,
            Shield = 48f,
            HearingRange = 340f,
            BaseMoveSpeed = 176f,
        });

        NormalizeProgressProfile();
        ResetCampaign();
        SaveProgressProfile();
    }

    private Rectangle WorldBounds => new((((_layoutSize.Width - (GridColumns * CellSize)) / 2) - 96), 88, GridColumns * CellSize, GridRows * CellSize);

    private Rectangle TopBarBounds => new((_layoutSize.Width - 340) / 2, 16, 340, TopBarHeight);

    private Rectangle BottomHudBounds => new((_layoutSize.Width - 780) / 2, _layoutSize.Height - BottomHudHeight - 18, 780, BottomHudHeight);

    private Rectangle SidePanelBounds => new(_layoutSize.Width - WorldMargin - SidePanelWidth, WorldBounds.Top, SidePanelWidth, WorldBounds.Height);

    private Rectangle RosterBounds => new(_layoutSize.Width - WorldMargin - SidePanelWidth, 18, SidePanelWidth, 88);

    private Rectangle IntelBounds => new(
        _layoutSize.Width - WorldMargin - SidePanelWidth,
        RosterBounds.Bottom + 8,
        SidePanelWidth,
        332);

    private Rectangle MinimapBounds => new(WorldMargin, 18, 176, 140);

    private Rectangle TimerBounds => new(MinimapBounds.Left, MinimapBounds.Bottom + 6, 104, 30);

    private Rectangle CreditsBounds => new(TimerBounds.Right + 4, MinimapBounds.Bottom + 6, MinimapBounds.Right - (TimerBounds.Right + 4), 30);

    private Rectangle BriefingOverlayBounds
    {
        get
        {
            var width = Math.Min(760, _layoutSize.Width - (WorldMargin * 2));
            var height = 188;
            var top = Math.Max(MinimapBounds.Bottom + 18, BottomHudBounds.Top - height - 16);
            return new Rectangle((_layoutSize.Width - width) / 2, top, width, height);
        }
    }

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
        PrepareIntegrityFrame(deltaSeconds);
        _uiPulseTime += deltaSeconds;
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

        FinalizeIntegrityFrame(deltaSeconds);
    }

}
