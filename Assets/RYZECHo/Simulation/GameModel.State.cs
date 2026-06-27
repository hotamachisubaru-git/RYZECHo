using System;
using System.Collections.Generic;
using System.Linq;

namespace RYZECHo;

public sealed partial class GameModel : IGameModel
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
    private const int BossPayoutMultiplier = GameRules.BossPayoutMultiplier;
    private const int AgentSkillPurchaseCost = 400;
    private const int MaxUltPoints = GameRules.MaxUltPoints;
    private const int InitialBuildPoints = GameRules.InitialBuildPoints;
    private const int MaxBuildPoints = GameRules.MaxBuildPoints;
    private const int SideSwapBuildPointRefill = GameRules.SideSwapBuildPointRefill;
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

    private readonly GameRulesSettingsSO _gameRules;
    private readonly LayoutSettingsSO _layoutSettings;
    private readonly GameplaySettingsSO _gameplaySettings;
    private readonly VisualSettingsSO _visualSettings;
    private readonly AudioSettingsSO _audioSettings;
    private readonly Random _random = new();
    private readonly Dictionary<WeaponType, WeaponStats> _weaponStats = CreateWeaponStats();
    private readonly List<Structure> _structures = new();
    private readonly List<WorldEffect> _worldEffects = new();
    private readonly List<Ripple> _ripples = new();
    private readonly List<Actor> _allies = new();
    private readonly List<Actor> _enemies = new();
    private readonly HashSet<Point> _permanentWalls = new();
    private readonly HashSet<Point> _buildSlots = new();
    private readonly HashSet<Point> _noBuildZones = new();
    private readonly List<Point> _spawnCells = new();
    private readonly List<string> _activityFeed = new();
    private readonly Dictionary<string, int> _bossSelectionCounts = new();
    private readonly Dictionary<string, int> _bossInvestments = new();
    private readonly Dictionary<string, int> _ultPoints = new();
    private readonly Dictionary<string, float> _sharedVisionTimers = new();
    private readonly ProgressProfile _profile = LoadProgressProfile();
    private readonly IEventBus _eventBus;
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
    private int _buildPoints = InitialBuildPoints;
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

    public event Action<RippleKind, PointF, float>? AudioCueEmitted;

    internal PointF AudioListenerPosition => _player.Position;

    public bool IsPaused { get; set; }
    public Size LayoutSize => _layoutSize;
    public GamePhase Phase => _phase;
    public bool PlayerIsAlive => _player.IsAlive;
    public bool ShowBriefing => _showBriefing;
    public float PlayerHealth => _player.Health;
    public float PlayerMaxHealth => _player.MaxHealth;
    public float PlayerShield => _player.Shield;
    public float PlayerMaxShield => _player.MaxShield;
    public int PlayerRoundWins => _playerRoundWins;
    public int EnemyRoundWins => _enemyRoundWins;
    public int CurrentRound => _currentRound;
    public int Credits => _credits;
    public int MatchTeamEliminations => _matchTeamEliminations;
    public int MatchPlayerDeaths => _matchPlayerDeaths;

    internal float GetPlayerHealth() => _player.Health;
    internal float GetPlayerMaxHealth() => _player.MaxHealth;
    internal float GetPlayerShield() => _player.Shield;
    internal float GetPlayerMaxShield() => _player.MaxShield;
    internal bool IsPlayerAlive() => _player.IsAlive;
    internal bool IsPlayerBoss() => _player.Agent != AgentKind.Veil;
    internal string GetAgentName() => _player.Agent switch { AgentKind.Veil => "V", AgentKind.Vine => "Vine", AgentKind.Nitro => "Nitro", AgentKind.Oasis => "Oasis", AgentKind.Divide => "Divide", AgentKind.Glitch => "Glitch", _ => "Unknown" };
    internal string GetWeaponName() => _player.Weapon.ToString();
    internal int GetCredits() => _credits;
    internal int GetBuildPoints() => _buildPoints;
    internal int GetUltPoints() => GetUltPoints(_player.Name);
    internal int GetPlayerRoundWins() => _playerRoundWins;
    internal int GetEnemyRoundWins() => _enemyRoundWins;
    internal int GetCurrentRound() => _currentRound;
    internal GamePhase GetPhase() => _phase;
    internal string GetPhaseLabel() => _phase switch { GamePhase.Construct => "CONSTRUCT", GamePhase.Bet => "BET", GamePhase.Hunt => "HUNT", GamePhase.RoundResult => "RESULT", GamePhase.Victory => "VICTORY", GamePhase.Defeat => "DEFEAT", _ => "UNKNOWN" };
    internal float GetRoundTimer() => _roundTimer;
    internal string GetCombatResult() => "";
    internal float GetCombatResultTimer() => 2f;
    internal bool GetShowPhaseFlash() => false;
    internal bool GetShowBriefing() => _showBriefing;
    internal string GetResultMessage() => _resultMessage;
    internal Actor GetPlayer() => _player;
    internal Actor[] GetAllies() => _allies.ToArray();
    internal Actor[] GetEnemies() => _enemies.ToArray();
    internal string[] GetActivityFeedMessages() => _activityFeed.ToArray();
    internal string GetWeaponLabel(WeaponType weapon) => weapon.ToString();
    internal string GetWeaponShortLabel(WeaponType weapon) => weapon.ToString();
    internal string GetAgentLabel(AgentKind agent) => agent.ToString();
    internal int GetCurrentPhase() => (int)_phase;
    internal BuildToolKind GetSelectedBuildTool() => _selectedBuildTool;
    internal ObjectiveSiteId GetAttackFocusSite() => _attackFocusSite;
    internal bool GetBombPlanted() => _bombPlanted;
    internal ObjectiveSiteId? GetArmedBombSite() => _armedBombSiteId;
    internal AgentKind GetSelectedAgent() => _selectedAgent;
    internal TeamRole GetPlayerTeamRole() => _playerTeamRole;
    internal WeaponType GetPlayerPrimaryWeapon() => _playerPrimaryWeapon;
    internal WeaponType GetPlayerSidearmWeapon() => _playerSidearmWeapon;
    internal float GetAgentSkillOneCooldown() => _agentSkillOneCooldown;
    internal float GetAgentSkillTwoCooldown() => _agentSkillTwoCooldown;
    internal int GetMaxUltPoints() => MaxUltPoints;

    internal GameModel(
        IEventBus? eventBus = null,
        GameRulesSettingsSO? gameRules = null,
        LayoutSettingsSO? layoutSettings = null,
        GameplaySettingsSO? gameplaySettings = null,
        VisualSettingsSO? visualSettings = null,
        AudioSettingsSO? audioSettings = null)
    {
        _eventBus = eventBus ?? GameEventBusAdapter.Instance;
        _gameRules = gameRules ?? UnityEngine.ScriptableObject.CreateInstance<GameRulesSettingsSO>();
        _layoutSettings = layoutSettings ?? UnityEngine.ScriptableObject.CreateInstance<LayoutSettingsSO>();
        _gameplaySettings = gameplaySettings ?? UnityEngine.ScriptableObject.CreateInstance<GameplaySettingsSO>();
        _visualSettings = visualSettings ?? UnityEngine.ScriptableObject.CreateInstance<VisualSettingsSO>();
        _audioSettings = audioSettings ?? UnityEngine.ScriptableObject.CreateInstance<AudioSettingsSO>();
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
