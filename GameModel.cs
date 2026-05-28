using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private const string UiFontFamily = "Yu Gothic UI";
    private const int DefaultClientWidth = GameLayout.DefaultClientWidth;
    private const int DefaultClientHeight = GameLayout.DefaultClientHeight;
    private const int WorldMargin = GameLayout.WorldMargin;
    private const int TopBarHeight = GameLayout.TopBarHeight;
    private const int SidePanelGap = GameLayout.SidePanelGap;
    private const int SidePanelWidth = GameLayout.SidePanelWidth;
    private const int BottomHudHeight = GameLayout.BottomHudHeight;
    private const int GridColumns = GameLayout.GridColumns;
    private const int GridRows = GameLayout.GridRows;
    private const int CellSize = GameLayout.CellSize;
    private const int RoundsToWin = GameRules.RoundsToWin;
    private const int RegulationSideSwitchRound = GameRules.RegulationSideSwitchRound;
    private const int OvertimeTriggerScore = GameRules.OvertimeTriggerScore;
    private const int TeamSize = GameRules.TeamSize;
    private const int StartingCredits = GameRules.StartingCredits;
    private const int WinRewardCredits = GameRules.WinRewardCredits;
    private const int LossRewardCredits = GameRules.LossRewardCredits;
    private const int KillRewardCredits = GameRules.KillRewardCredits;
    private const int ObjectiveRewardCredits = GameRules.ObjectiveRewardCredits;
    private const int BossKillDividendCredits = GameRules.BossKillDividendCredits;
    private const int BossEliminationBonusCredits = GameRules.BossEliminationBonusCredits;
    private const int MaxBossSelectionsPerActor = GameRules.MaxBossSelectionsPerActor;
    private const int OptimalBossInvestment = GameRules.OptimalBossInvestment;
    private const int AgentSkillPurchaseCost = 400;
    private const int MaxUltPoints = GameRules.MaxUltPoints;
    private const float DefaultFovDegrees = GameRules.DefaultFovDegrees;
    private const float SniperFovDegrees = GameRules.SniperFovDegrees;
    private const float SoundCueLifetimeSeconds = GameRules.SoundCueLifetimeSeconds;
    private const float SharedVisionDurationSeconds = GameRules.SharedVisionDurationSeconds;
    private const float IdleBreathExposeSeconds = GameRules.IdleBreathExposeSeconds;
    private const float BreathingRippleIntervalSeconds = GameRules.BreathingRippleIntervalSeconds;
    private const float RoundDurationSeconds = GameRules.RoundDurationSeconds;
    private const float BombPlantSeconds = GameRules.BombPlantSeconds;
    private const float BombFuseSeconds = GameRules.BombFuseSeconds;
    private const float BombDefuseSeconds = GameRules.BombDefuseSeconds;
    private const float BombSiteRadius = GameRules.BombSiteRadius;
    private const float WorldPerspectiveScaleX = GameLayout.WorldPerspectiveScaleX;
    private const float WorldPerspectiveScaleY = GameLayout.WorldPerspectiveScaleY;
    private const float WorldPerspectiveShearX = GameLayout.WorldPerspectiveShearX;
    private const float WorldPerspectiveTopInset = GameLayout.WorldPerspectiveTopInset;
    private const float HuntCameraZoom = GameLayout.HuntCameraZoom;
    private const float HuntVisibleWorldFractionX = GameLayout.HuntVisibleWorldFractionX;
    private const float HuntVisibleWorldFractionY = GameLayout.HuntVisibleWorldFractionY;
    private const float HuntCameraTargetX = GameLayout.HuntCameraTargetX;
    private const float HuntCameraTargetY = GameLayout.HuntCameraTargetY;

    private readonly Random _random = new();
    private readonly Dictionary<WeaponType, WeaponStats> _weaponStats = CreateWeaponStats();
    private readonly List<Structure> _structures = [];
    private readonly List<WorldEffect> _worldEffects = [];
    private readonly List<Ripple> _ripples = [];
    private readonly List<Actor> _allies = [];
    private readonly List<Actor> _enemies = [];
    private readonly HashSet<Point> _permanentWalls = [];
    private readonly HashSet<Point> _buildSlots = [];
    private readonly HashSet<Point> _noBuildZones = [];
    private readonly List<Point> _spawnCells = [];
    private readonly List<string> _activityFeed = [];
    private readonly Dictionary<string, int> _bossSelectionCounts = [];
    private readonly Dictionary<string, int> _bossInvestments = [];
    private readonly Dictionary<string, int> _ultPoints = [];
    private readonly Dictionary<string, float> _sharedVisionTimers = [];
    private readonly ProgressProfile _profile = LoadProgressProfile();
    private Size _layoutSize = new(DefaultClientWidth, DefaultClientHeight);

    private GamePhase _phase = GamePhase.Construct;
    private BuildToolKind _selectedBuildTool = BuildToolKind.BlastDoor;
    private WeaponType _selectedWeapon = WeaponType.Giant;
    private WeaponType _selectedSidearmWeapon = WeaponType.Pulse;
    private WeaponType _playerPrimaryWeapon = WeaponType.Giant;
    private WeaponType _playerSidearmWeapon = WeaponType.Pulse;
    private LoadoutFocus _selectedLoadoutFocus = LoadoutFocus.Primary;
    private AgentKind _selectedAgent = AgentKind.Veil;
    private bool _agentSkillPurchased;
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
    private float _breathingRippleCooldown;
    private float _agentSkillOneCooldown;
    private float _agentSkillTwoCooldown;
    private float _playerDashTimer;
    private float _playerOverdriveTimer;
    private float _playerHealingTimer;
    private float _playerGhostTimer;
    private float _hunterEyeTimer;
    private float _systemCrashTimer;
    private float _uiPulseTime;
    private float _adImpressionTimer;
    private GamePhase _resultDestination = GamePhase.Bet;
    private string _selectedBossName = RosterCatalog.PlayerName;
    private string _lastProgressionSummary = string.Empty;
    private string _resultMessage = "最初の構築が、全ラウンドを支配する。";
    private ObjectiveSiteId _attackFocusSite = ObjectiveSiteId.Alpha;
    private ObjectiveSiteId? _armedBombSiteId;
    private bool _showBriefing = true;
    private bool _bombPlanted;
    private bool _isOvertime;
    private bool _sideSwapConstructPending;
    private Actor? _activePlanter;
    private TeamRole _playerTeamRole = TeamRole.Defense;

    private readonly Actor _player;

    internal event Action<RippleKind, PointF, float>? AudioCueEmitted;

    internal PointF AudioListenerPosition => _player.Position;

    public GameModel()
    {
        BuildMapGeometry();

        _player = CreateActor(RosterCatalog.Player);
        foreach (var blueprint in RosterCatalog.Allies)
        {
            _allies.Add(CreateActor(blueprint));
        }

        NormalizeProgressProfile();
        ResetCampaign();
        SaveProgressProfile();
    }

    private Actor CreateActor(ActorBlueprint blueprint)
    {
        return new Actor
        {
            Name = blueprint.Name,
            Agent = blueprint.Agent,
            Type = blueprint.Type,
            HomeCell = blueprint.HomeCell,
            Weapon = blueprint.Weapon,
            Position = CellCenter(blueprint.HomeCell),
            Radius = blueprint.Radius,
            MaxHealth = blueprint.MaxHealth,
            MaxShield = blueprint.MaxShield,
            Health = blueprint.MaxHealth,
            Shield = blueprint.MaxShield,
            HearingRange = blueprint.HearingRange,
            BaseMoveSpeed = blueprint.BaseMoveSpeed,
        };
    }

}
