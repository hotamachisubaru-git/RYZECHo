using System;
using System.Collections.Generic;
using System.Linq;

namespace RYZECHo;

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
    private const float BombPlantSeconds = GameRules.BombPlantSeconds;
    private const float BombFuseSeconds = GameRules.BombFuseSeconds;
    private const float BombDefuseSeconds = GameRules.BombDefuseSeconds;
    private const float BombSiteRadius = GameRules.BombSiteRadius;
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
    private string _resultMessage = "鬯ｯ・ｮ繝ｻ・ｫ郢晢ｽｻ繝ｻ・ｴ鬮ｯ譎｢・｣・ｰ郢晢ｽｻ繝ｻ・｢郢晢ｽｻ邵ｺ・､・つ鬯ｯ・ｮ繝ｻ・ｯ髯ｷ闌ｨ・ｽ・ｷ郢晢ｽｻ繝ｻ・ｽ郢晢ｽｻ繝ｻ・ｻ鬯ｮ・ｫ繝ｻ・ｴ髫ｰ・ｫ繝ｻ・ｾ郢晢ｽｻ繝ｻ・ｽ郢晢ｽｻ繝ｻ・ｴ鬯ｩ蟷｢・ｽ・｢髫ｴ雜｣・ｽ・｢郢晢ｽｻ繝ｻ・ｽ郢晢ｽｻ繝ｻ・ｻ鬯ｯ・ｮ繝ｻ・ｫ郢晢ｽｻ繝ｻ・ｶ鬮ｯ諛ｶ・ｽ・｣郢晢ｽｻ繝ｻ・､鬯ｮ・ｴ髢ｧ・ｴ闔・｢驛｢譎｢・ｽ・ｻ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｯ鬯ｮ・ｯ隴趣ｽ｢繝ｻ・ｽ繝ｻ・ｲ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｨ鬯ｩ蛹・ｽｽ・ｯ郢晢ｽｻ繝ｻ・ｶ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｲ鬯ｯ・ｩ隰ｳ・ｾ繝ｻ・ｽ繝ｻ・ｵ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｲ鬯ｩ蛹・ｽｽ・ｶ髣包ｽｵ陟搾ｽｺ繝ｻ・ｺ繝ｻ・｢鬩幢ｽ｢隴趣ｽ｢繝ｻ・ｽ繝ｻ・ｻ鬯ｯ・ｩ陝ｷ・｢繝ｻ・ｽ繝ｻ・｢鬮ｫ・ｴ髮懶ｽ｣繝ｻ・ｽ繝ｻ・｢驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｽ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｩ鬯ｯ・ｩ陝ｷ・｢繝ｻ・ｽ繝ｻ・｢驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｧ鬩幢ｽ｢隴趣ｽ｢繝ｻ・ｽ繝ｻ・ｻ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｦ鬯ｯ・ｩ陝ｷ・｢繝ｻ・ｽ繝ｻ・｢鬮ｫ・ｴ髮懶ｽ｣繝ｻ・ｽ繝ｻ・｢驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｽ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｳ鬯ｯ・ｩ陝ｷ・｢繝ｻ・ｽ繝ｻ・｢鬮ｫ・ｴ陷ｿ髢・ｾ蜉ｱ繝ｻ繝ｻ・ｽ郢晢ｽｻ繝ｻ・ｳ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｨ鬩幢ｽ｢隴趣ｽ｢繝ｻ・ｽ繝ｻ・ｻ鬮ｯ讖ｸ・ｽ・ｳ髯樊ｻゑｽｽ・ｲ郢晢ｽｻ繝ｻ・ｽ郢晢ｽｻ繝ｻ・ｬ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｾ鬩幢ｽ｢隴趣ｽ｢繝ｻ・ｽ繝ｻ・ｻ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｯ鬯ｯ・ｯ繝ｻ・ｯ郢晢ｽｻ繝ｻ・ｩ鬮ｮ迢暦ｽｿ・ｫ郢晢ｽｻ郢晢ｽｻ繝ｻ・ｺ驛｢・ｧ闔ｨ螟ｲ・ｽ・ｽ繝ｻ・ｬ髯区ｻゑｽｽ・･驛｢譎｢・ｽ・ｻ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｹ驛｢譎｢・ｽ・ｻ郢晢ｽｻ繝ｻ・ｧ鬯ｮ・｣陋ｹ繝ｻ・ｽ・ｽ繝ｻ・ｵ鬮ｫ・ｰ繝ｻ・ｨ鬯ｲ謇假ｽｽ・ｴ繝ｻ縺､ﾂ鬯ｩ蟷｢・ｽ・｢髫ｴ雜｣・ｽ・｢郢晢ｽｻ繝ｻ・ｽ郢晢ｽｻ繝ｻ・ｻ;
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

    public bool IsPaused { get; set; }
    // ==================== HUD Getter ====================

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
    internal int GetUltPoints(string name) => _ultPoints.GetValueOrDefault(name, 0);
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



    public GameModel(
        IEventBus? eventBus = null,
        GameRulesSettingsSO? gameRules = null,
        LayoutSettingsSO? layoutSettings = null,
        GameplaySettingsSO? gameplaySettings = null,
        VisualSettingsSO? visualSettings = null,
        AudioSettingsSO? audioSettings = null)
    {
        _eventBus = eventBus ?? GameEventBusAdapter.Instance;
        _gameRules = gameRules ?? ScriptableObject.CreateInstance<GameRulesSettingsSO>();
        _layoutSettings = layoutSettings ?? ScriptableObject.CreateInstance<LayoutSettingsSO>();
        _gameplaySettings = gameplaySettings ?? ScriptableObject.CreateInstance<GameplaySettingsSO>();
        _visualSettings = visualSettings ?? ScriptableObject.CreateInstance<VisualSettingsSO>();
        _audioSettings = audioSettings ?? ScriptableObject.CreateInstance<AudioSettingsSO>();
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
