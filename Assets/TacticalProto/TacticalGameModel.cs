using System;
using System.Collections.Generic;
using System.Linq;
using RYZECHo;

namespace RYZECHo.TacticalProto
{
    /// <summary>
    /// Tactical structure entity (wall, trap, beacon, etc.)
    /// </summary>
    public sealed class TacticalStructure
    {
        public required int APCost { get; init; }
        public required string Label { get; init; }
        public float Health { get; set; }
        public float MaxHealth { get; init; }
        public bool BlocksMovement { get; init; }
        public bool AiTargetable { get; init; }
    }

    /// <summary>
    /// Tactical world effect (smoke, poison cloud, etc.)
    /// </summary>
    public sealed class TacticalWorldEffect
    {
        public required float Radius { get; init; }
        public required float Lifetime { get; init; }
        public float Age { get; set; }
        public bool BlocksVision { get; init; }
    }

    /// <summary>
    /// Core game model for tactical shooter prototype.
    /// Manages phases (Construct/Bet/Hunt/RoundResult/Victory/Defeat),
    /// actor state, structures, weapons, and match progression.
    /// GameRules: StartingCredits=1000, RoundsToWin=7, TeamSize=4
    /// </summary>
    public sealed class TacticalGameModel
    {
        // === GameRules constants ===
        private const int RoundsToWin = GameRules.RoundsToWin;       // 7
        private const int RegulationSideSwitchRound = GameRules.RegulationSideSwitchRound; // 4
        private const int TeamSize = GameRules.TeamSize;             // 4
        private const int StartingCredits = GameRules.StartingCredits; // 1000
        private const int WinRewardCredits = GameRules.WinRewardCredits; // 2200
        private const int LossRewardCredits = GameRules.LossRewardCredits; // 1200
        private const int KillRewardCredits = GameRules.KillRewardCredits; // 400
        private const int ObjectiveRewardCredits = GameRules.ObjectiveRewardCredits; // 350
        private const int InitialBuildPoints = GameRules.InitialBuildPoints; // 12
        private const int MaxBuildPoints = GameRules.MaxBuildPoints; // 12
        private const int SideSwapBuildPointRefill = GameRules.SideSwapBuildPointRefill; // 12
        private const float RoundDurationSeconds = GameRules.RoundDurationSeconds; // 100f
        private const float BombPlantSeconds = GameRules.BombPlantSeconds; // 3f
        private const float BombFuseSeconds = GameRules.BombFuseSeconds; // 35f
        private const float BombDefuseSeconds = GameRules.BombDefuseSeconds; // 8f
        private const float BombSiteRadius = GameRules.BombSiteRadius; // 28f

        // === Internal state ===
        private GamePhase _currentPhase;
        private int _playerRoundWins;
        private int _enemyRoundWins;
        private int _currentRound;
        private float _roundTimer;
        private int _credits;
        private int _buildPoints;
        private int _selectedBet;
        private int _matchTeamEliminations;
        private int _matchPlayerDeaths;
        private int _roundBossKillCount;
        private float _coreHealth;
        private float _bombPlantProgress;
        private float _bombDefuseProgress;
        private bool _bombPlanted;
        private bool _isOvertime;
        private bool _showBriefing;
        private string _resultMessage;
        private GamePhase _resultDestination;
        private ObjectiveSiteId _attackFocusSite;
        private TeamRole _playerTeamRole;

        // Actors
        private TacticalActor? _player;
        private TacticalActor[] _allies;
        private TacticalActor[] _enemies;

        // Weapon stats dictionary
        private readonly Dictionary<WeaponType, WeaponStats> _weaponStats;

        // Structures and effects
        private List<TacticalStructure> _structures;
        private List<TacticalWorldEffect> _worldEffects;

        // Selection state
        private AgentKind _selectedAgent;
        private WeaponType _selectedWeapon;
        private WeaponType _selectedSidearmWeapon;
        private WeaponType _playerPrimaryWeapon;
        private WeaponType _playerSidearmWeapon;
        private BuildToolKind _selectedBuildTool;
        private string _selectedBossName;

        // Events
        public event Action<GamePhase>? OnPhaseChanged;
        public event Action<bool, string>? OnRoundResult;
        public event Action<bool, string>? OnMatchResult;
        public event Action<string>? OnActivityFeed;

        // Public accessors
        public GamePhase CurrentPhase => _currentPhase;
        public int PlayerRoundWins => _playerRoundWins;
        public int EnemyRoundWins => _enemyRoundWins;
        public int CurrentRound => _currentRound;
        public int Credits => _credits;
        public int BuildPoints => _buildPoints;
        public TacticalActor? Player => _player;
        public TacticalActor[] Allies => _allies;
        public TacticalActor[] Enemies => _enemies;
        public List<TacticalStructure> Structures => _structures;
        public List<TacticalWorldEffect> WorldEffects => _worldEffects;
        public bool PlayerIsAlive => _player?.IsAlive ?? false;
        public bool HasMatchWinner => _playerRoundWins >= RoundsToWin || _enemyRoundWins >= RoundsToWin;
        public TeamRole PlayerTeamRole => _playerTeamRole;
        public float RoundTimer => _roundTimer;
        public float CoreHealth => _coreHealth;
        public bool BombPlanted => _bombPlanted;
        public ObjectiveSiteId AttackFocusSite => _attackFocusSite;
        public string ResultMessage => _resultMessage;

        // === Constructor ===
        public TacticalGameModel()
        {
            _weaponStats = CreateWeaponStats();
            _structures = new List<TacticalStructure>();
            _worldEffects = new List<TacticalWorldEffect>();
            _allies = Array.Empty<TacticalActor>();
            _enemies = Array.Empty<TacticalActor>();
            ResetCampaign();
        }

        // === Phase update dispatcher ===
        public void Update(float deltaSeconds, TacticalInput input)
        {
            switch (_currentPhase)
            {
                case GamePhase.Construct:
                    UpdateConstructPhase(deltaSeconds, input);
                    break;
                case GamePhase.Bet:
                    UpdateBetPhase(deltaSeconds, input);
                    break;
                case GamePhase.Hunt:
                    UpdateHuntPhase(deltaSeconds, input);
                    break;
                case GamePhase.RoundResult:
                    UpdateRoundResultPhase(deltaSeconds, input);
                    break;
                case GamePhase.Victory:
                case GamePhase.Defeat:
                    UpdateEndState(deltaSeconds, input);
                    break;
            }
        }

        // === Construct phase logic ===
        private void UpdateConstructPhase(float deltaSeconds, TacticalInput input)
        {
            if (_showBriefing && input.Confirm)
            {
                _showBriefing = false;
                BeginBetPhase();
                return;
            }

            // Build tool selection via number keys
            if (input.Press1) _selectedBuildTool = BuildToolKind.BlastDoor;
            else if (input.Press2) _selectedBuildTool = BuildToolKind.HoneyTrap;
            else if (input.Press3) _selectedBuildTool = BuildToolKind.StaticNest;
            else if (input.Press4) _selectedBuildTool = BuildToolKind.ReconBeacon;
            else if (input.Press5) _selectedBuildTool = BuildToolKind.ShieldRelay;
            else if (input.Press6) CycleSelectedAgent();

            // Q/E: cycle build tool
            if (input.PressQ) CycleBuildTool(-1);
            if (input.PressE) CycleBuildTool(1);

            // Confirm: advance to Bet phase
            if (input.Confirm)
            {
                _showBriefing = false;
                BeginBetPhase();
            }
        }

        // === Bet phase logic ===
        private void UpdateBetPhase(float deltaSeconds, TacticalInput input)
        {
            // Boss selection via 1-4
            if (input.Press1) _selectedBossName = RosterCatalog.PlayerName;
            else if (input.Press2) _selectedBossName = RosterCatalog.NorthAnchorName;
            else if (input.Press3) _selectedBossName = RosterCatalog.SouthAnchorName;
            else if (input.Press4) _selectedBossName = RosterCatalog.CenterLinkName;

            // Weapon cycling via Q/E
            if (input.PressQ) CycleWeapon(-1);
            if (input.PressE) CycleWeapon(1);

            // Bet adjustment
            if (input.AdjustBetLeft) AdjustBet(-25);
            if (input.AdjustBetRight) AdjustBet(25);

            // Confirm: start round
            if (input.Confirm)
            {
                if (!TryStartRound()) return;
                StartRound();
            }
        }

        /// <summary>Transition to Bet phase.</summary>
        private void BeginBetPhase()
        {
            _currentPhase = GamePhase.Bet;
            _bombPlanted = false;
            _bombPlantProgress = 0f;
            _bombDefuseProgress = 0f;
            _resultDestination = GamePhase.Bet;
            _showBriefing = false;
            _resultMessage = $"Round {CurrentRound} - Select your boss and loadout.";
            OnPhaseChanged?.Invoke(_currentPhase);
        }

        /// <summary>Check credits before starting round.</summary>
        private bool TryStartRound()
        {
            if (_player == null) return false;

            var primaryWeapon = _weaponStats[_selectedWeapon];
            var sidearmWeapon = _weaponStats[_selectedSidearmWeapon];
            var totalCost = primaryWeapon.Cost + sidearmWeapon.Cost + _selectedBet;

            if (totalCost > _credits)
            {
                _resultMessage = "Insufficient credits for this loadout.";
                return false;
            }

            _credits -= totalCost;
            return true;
        }

        /// <summary>Start the round: restore all actors, spawn enemies.</summary>
        private void StartRound()
        {
            _currentPhase = GamePhase.Hunt;
            _roundTimer = RoundDurationSeconds;
            _bombPlanted = false;
            _bombPlantProgress = 0f;
            _bombDefuseProgress = 0f;
            _coreHealth = 180f;
            _attackFocusSite = ChooseAttackFocusSite();
            _resultDestination = GamePhase.Bet;
            _roundBossKillCount = 0;

            // Restore player
            if (_player != null)
            {
                _player.Agent = _selectedAgent;
                _player.Weapon = _playerPrimaryWeapon;
                _player.FullRestore();
            }

            // Restore allies
            foreach (var ally in _allies) ally.FullRestore();

            // Spawn enemies
            CreateEnemySquad();

            OnPhaseChanged?.Invoke(_currentPhase);

            var roleLabel = IsPlayerTeamAttacking() ? "Attack" : "Defense";
            var siteLabel = _attackFocusSite == ObjectiveSiteId.Alpha ? "A" : "B";
            _resultMessage = $"Round {CurrentRound}: {roleLabel} on site {siteLabel}. Bet: {_selectedBet}c.";
            OnActivityFeed?.Invoke(_resultMessage);
        }

        // === Hunt phase logic ===
        private void UpdateHuntPhase(float deltaSeconds, TacticalInput input)
        {
            _roundTimer -= deltaSeconds;

            // Player update
            if (_player != null)
            {
                _player.UpdateFireCooldown(deltaSeconds);
                _player.UpdateSkillCooldowns(deltaSeconds);
                _player.UpdateShieldRegen(deltaSeconds);
                _player.Move(input, deltaSeconds);

                // Player fire
                if (input.FireHeld && _player.IsAlive)
                {
                    TryPlayerFire(input);
                }
            }

            // Ally updates
            foreach (var ally in _allies)
            {
                ally.UpdateFireCooldown(deltaSeconds);
                ally.UpdateShieldRegen(deltaSeconds);
            }

            // Enemy updates
            foreach (var enemy in _enemies)
            {
                enemy.UpdateFireCooldown(deltaSeconds);
                enemy.UpdateShieldRegen(deltaSeconds);
            }

            // Structure updates
            UpdateStructures(deltaSeconds);

            // World effect updates
            UpdateWorldEffects(deltaSeconds);

            // Check round end
            CheckRoundEnd();
        }

        /// <summary>Player fire logic with vision-based damage.</summary>
        private void TryPlayerFire(TacticalInput input)
        {
            if (_player == null || !_player.IsAlive) return;

            var weapon = GetTacticalWeapon(_player.Weapon);
            if (weapon.CurrentCooldown > 0f) return;

            if (weapon.TryFire())
            {
                // Damage enemies within vision range
                foreach (var enemy in _enemies)
                {
                    if (!enemy.IsAlive) continue;
                    var dist = Vector2Distance(_player.Position, enemy.Position);
                    if (dist <= weapon.VisionRange)
                    {
                        enemy.TakeDamage(weapon.Damage, null);
                    }
                }
            }
        }

        /// <summary>Update structures (remove expired ones).</summary>
        private void UpdateStructures(float deltaSeconds)
        {
            for (int i = _structures.Count - 1; i >= 0; i--)
            {
                var s = _structures[i];
                s.Health -= deltaSeconds * 10f; // passive decay
                if (s.Health <= 0f)
                {
                    _structures.RemoveAt(i);
                }
            }
        }

        /// <summary>Update world effects (remove expired ones).</summary>
        private void UpdateWorldEffects(float deltaSeconds)
        {
            for (int i = _worldEffects.Count - 1; i >= 0; i--)
            {
                _worldEffects[i].Age += deltaSeconds;
                if (_worldEffects[i].Age >= _worldEffects[i].Lifetime)
                {
                    _worldEffects.RemoveAt(i);
                }
            }
        }

        /// <summary>Check round end conditions.</summary>
        private void CheckRoundEnd()
        {
            if (_bombPlanted)
            {
                if (_coreHealth <= 0f)
                {
                    EndRound(true, "Bomb destroyed. Defense wins.");
                    return;
                }
                return;
            }

            // All enemies eliminated
            if (LiveEnemyCount() == 0)
            {
                EndRound(true, IsPlayerTeamAttacking() ? "All enemies eliminated. Attack wins." : "All objectives held. Defense wins.");
                return;
            }

            // All player team dead
            if (!IsPlayerTeamAlive())
            {
                EndRound(false, IsPlayerTeamAttacking() ? "Attack team eliminated. Defense wins." : "Defense team eliminated. Attack wins.");
                return;
            }

            // Round timer expired
            if (_roundTimer <= 0f)
            {
                if (IsPlayerTeamAttacking())
                {
                    EndRound(false, "Time expired. Attack failed.");
                }
                else
                {
                    EndRound(true, "Time expired. Defense holds.");
                }
            }
        }

        /// <summary>Round result phase: show summary, then advance.</summary>
        private void UpdateRoundResultPhase(float deltaSeconds, TacticalInput input)
        {
            _roundTimer -= deltaSeconds;
            if (_roundTimer <= 0f)
            {
                if (_currentRound == RegulationSideSwitchRound)
                {
                    _playerTeamRole = ToggleRole(_playerTeamRole);
                    _buildPoints = Math.Min(MaxBuildPoints, _buildPoints + SideSwapBuildPointRefill);
                    _currentPhase = GamePhase.Construct;
                    _showBriefing = true;
                    _resultMessage = "Side swap! Build phase begins.";
                }
                else
                {
                    _currentPhase = _resultDestination;
                    if (_resultDestination == GamePhase.Construct)
                    {
                        _buildPoints = InitialBuildPoints;
                        _showBriefing = true;
                    }
                }
                _currentRound++;
                OnPhaseChanged?.Invoke(_currentPhase);
            }
        }

        /// <summary>Victory/Defeat end state.</summary>
        private void UpdateEndState(float deltaSeconds, TacticalInput input)
        {
            if (input.Confirm)
            {
                ResetCampaign();
            }
        }

        // === Round end handling ===
        public void EndRound(bool won, string outcomeSummary)
        {
            if (won)
                _playerRoundWins++;
            else
                _enemyRoundWins++;

            _credits += won ? WinRewardCredits : LossRewardCredits;

            var summary = $"{outcomeSummary} SCORE {_playerRoundWins}-{_enemyRoundWins}";
            _resultMessage = summary;

            OnRoundResult?.Invoke(won, summary);
            OnActivityFeed?.Invoke(summary);

            if (HasMatchWinner)
            {
                _currentPhase = won ? GamePhase.Victory : GamePhase.Defeat;
                OnMatchResult?.Invoke(won, summary);
                OnPhaseChanged?.Invoke(_currentPhase);
                return;
            }

            _currentPhase = GamePhase.RoundResult;
            _roundTimer = 2.4f;
            OnPhaseChanged?.Invoke(_currentPhase);
        }

        // === Campaign reset ===
        private void ResetCampaign()
        {
            _currentPhase = GamePhase.Construct;
            _playerRoundWins = 0;
            _enemyRoundWins = 0;
            _currentRound = 1;
            _roundTimer = RoundDurationSeconds;
            _credits = StartingCredits;
            _buildPoints = InitialBuildPoints;
            _selectedBet = 300;
            _playerTeamRole = TeamRole.Defense;
            _isOvertime = false;
            _bombPlanted = false;
            _coreHealth = 180f;
            _bombPlantProgress = 0f;
            _bombDefuseProgress = 0f;
            _showBriefing = true;
            _resultMessage = "New campaign. Select your agent (1-4) to begin.";
            _resultDestination = GamePhase.Bet;
            _attackFocusSite = ObjectiveSiteId.Alpha;
            _selectedBossName = RosterCatalog.PlayerName;
            _structures.Clear();
            _worldEffects.Clear();
            _enemies = Array.Empty<TacticalActor>();

            // Player blueprint
            _player = CreateActorFromBlueprint(RosterCatalog.Player);
            _playerPrimaryWeapon = _selectedWeapon;
            _playerSidearmWeapon = _selectedSidearmWeapon;
            _player.Weapon = _playerPrimaryWeapon;
            _player.FullRestore();

            // Ally blueprints
            var allyBlueprints = RosterCatalog.Allies;
            _allies = new TacticalActor[allyBlueprints.Length];
            for (int i = 0; i < allyBlueprints.Length; i++)
            {
                _allies[i] = CreateActorFromBlueprint(allyBlueprints[i]);
                _allies[i].Weapon = RosterCatalog.DefaultFriendlyWeaponFor(allyBlueprints[i].Name);
                _allies[i].FullRestore();
            }

            OnPhaseChanged?.Invoke(_currentPhase);
        }

        // === Actor creation ===
        private TacticalActor CreateActorFromBlueprint(ActorBlueprint bp)
        {
            return new TacticalActor
            {
                Name = bp.Name,
                Agent = (AgentKind)(int)bp.Agent,
                Type = (ActorType)(int)bp.Type,
                Weapon = (WeaponType)(int)bp.Weapon,
                Position = new System.Numerics.Vector2(
                    bp.HomeCell.X * GameLayout.CellSize,
                    bp.HomeCell.Y * GameLayout.CellSize),
                HomePosition = new System.Numerics.Vector2(
                    bp.HomeCell.X * GameLayout.CellSize,
                    bp.HomeCell.Y * GameLayout.CellSize),
                Radius = bp.Radius,
                MaxHealth = bp.MaxHealth,
                MaxShield = bp.MaxShield,
                HearingRange = bp.HearingRange,
                BaseMoveSpeed = bp.BaseMoveSpeed,
                Health = bp.MaxHealth,
                Shield = bp.MaxShield,
            };
        }

        /// <summary>Create enemy squad: North Anchor, South Anchor, Center Link, plus one random.</summary>
        private void CreateEnemySquad()
        {
            _enemies = new TacticalActor[TeamSize];
            var enemyAgents = new[] { AgentKind.Glitch, AgentKind.Divide, AgentKind.Nitro, AgentKind.Vine };
            for (int i = 0; i < TeamSize; i++)
            {
                var agent = enemyAgents[i % enemyAgents.Length];
                var weapon = i < 3
                    ? (_player != null ? _player.Weapon : GetRandomEnemyWeapon())
                    : GetRandomEnemyWeapon();

                _enemies[i] = new TacticalActor
                {
                    Name = $"Enemy{i + 1}",
                    Agent = agent,
                    Type = ActorType.Enemy,
                    Weapon = weapon,
                    Position = new System.Numerics.Vector2(
                        2f + i * 3f,
                        4f + (i % 2) * 4f),
                    HomePosition = new System.Numerics.Vector2(2f, 6f),
                    Radius = 13f,
                    MaxHealth = 95f,
                    MaxShield = 42f,
                    HearingRange = 300f,
                    BaseMoveSpeed = 180f,
                    Health = 95f,
                    Shield = 42f,
                };
            }
        }

        private WeaponType GetRandomEnemyWeapon()
        {
            var weapons = new[]
            {
                WeaponType.Blitz, WeaponType.Monster, WeaponType.Melt, WeaponType.Fairy,
                WeaponType.Giant, WeaponType.Violet, WeaponType.Howl, WeaponType.Shard
            };
            return weapons[(_currentRound * 7 + 3) % weapons.Length];
        }

        // === Attack focus site ===
        private ObjectiveSiteId ChooseAttackFocusSite()
        {
            return _currentRound <= RegulationSideSwitchRound
                ? _attackFocusSite
                : (_attackFocusSite == ObjectiveSiteId.Alpha ? ObjectiveSiteId.Bravo : ObjectiveSiteId.Alpha);
        }

        // === Helpers ===
        private bool IsPlayerTeamAttacking() => _playerTeamRole == TeamRole.Attack;

        private int LiveEnemyCount()
        {
            int count = 0;
            foreach (var e in _enemies) if (e.IsAlive) count++;
            return count;
        }

        private int LivePlayerTeamCount()
        {
            int count = 0;
            if (_player?.IsAlive == true) count++;
            foreach (var a in _allies) if (a.IsAlive) count++;
            return count;
        }

        private bool IsPlayerTeamAlive() => LivePlayerTeamCount() > 0;

        private static TeamRole ToggleRole(TeamRole role) =>
            role == TeamRole.Attack ? TeamRole.Defense : TeamRole.Attack;

        private static float Vector2Distance(System.Numerics.Vector2 a, System.Numerics.Vector2 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private TacticalWeapon GetTacticalWeapon(WeaponType type)
        {
            var stats = _weaponStats[type];
            return new TacticalWeapon
            {
                Type = type,
                Label = stats.Label,
                ShortLabel = stats.ShortLabel,
                MaxMagazineAmmo = stats.MagazineAmmo,
                MagazineAmmo = stats.MagazineAmmo,
                MaxReserveAmmo = stats.ReserveAmmo,
                ReserveAmmo = stats.ReserveAmmo,
                Damage = stats.Damage,
                FireCooldown = stats.FireCooldown,
                VisionRange = stats.VisionRange,
                MoveSpeedModifier = stats.MoveSpeed,
                Cost = stats.Cost,
            };
        }

        // === Cycle methods ===
        private void CycleSelectedAgent()
        {
            var agents = new[] { AgentKind.Veil, AgentKind.Vine, AgentKind.Nitro, AgentKind.Oasis, AgentKind.Divide, AgentKind.Glitch };
            var idx = Array.IndexOf(agents, _selectedAgent);
            _selectedAgent = agents[(idx + 1) % agents.Length];
        }

        private void CycleBuildTool(int direction)
        {
            var tools = new[]
            {
                BuildToolKind.BlastDoor, BuildToolKind.HoneyTrap, BuildToolKind.StaticNest,
                BuildToolKind.ReconBeacon, BuildToolKind.ShieldRelay, BuildToolKind.PortableCover,
                BuildToolKind.VisorWall, BuildToolKind.HoloDecoy
            };
            var idx = Array.IndexOf(tools, _selectedBuildTool);
            _selectedBuildTool = tools[(idx + direction + tools.Length) % tools.Length];
        }

        private void CycleWeapon(int direction)
        {
            var weapons = new[]
            {
                WeaponType.Blitz, WeaponType.Monster, WeaponType.Melt, WeaponType.Fairy,
                WeaponType.Giant, WeaponType.Juggernaut, WeaponType.Violet, WeaponType.Changer,
                WeaponType.Howl, WeaponType.Pulse, WeaponType.Shard
            };
            var idx = Array.IndexOf(weapons, _selectedWeapon);
            _selectedWeapon = weapons[(idx + direction + weapons.Length) % weapons.Length];
        }

        private void AdjustBet(int amount)
        {
            _selectedBet = Math.Clamp(_selectedBet + amount, 0, 1000);
        }

        // === Weapon stats dictionary ===
        private static Dictionary<WeaponType, WeaponStats> CreateWeaponStats()
        {
            var stats = new Dictionary<WeaponType, WeaponStats>();

            var weaponData = new (WeaponType, string, string, int, int, int, float, float, float)[]
            {
                (WeaponType.Blitz,        "Assault",  "Standard", 2700, 30, 90,  28f, 0.08f, 180f),
                (WeaponType.Monster,      "Heavy",    "LongRange", 4750, 12, 36, 500f, 1.2f,  250f),
                (WeaponType.Melt,         "SMG",      "Standard",  1900, 30, 90,  24f, 0.06f, 200f),
                (WeaponType.Fairy,        "DMR",      "Precision", 3500, 15, 45, 350f, 0.4f,  320f),
                (WeaponType.Giant,        "Assault",  "Standard",  2900, 25, 75,  28f, 0.07f, 220f),
                (WeaponType.Juggernaut,   "Heavy",    "LongRange", 5000, 20, 60, 600f, 1.5f,  400f),
                (WeaponType.Violet,       "Pistol",   "Standard",   800, 14, 42,  18f, 0.15f, 150f),
                (WeaponType.Changer,      "Special",  "Variable",  3200, 20, 60, 300f, 0.1f,  260f),
                (WeaponType.Howl,         "Sniper",   "LongRange", 3000,  8, 24, 600f, 2.0f,  500f),
                (WeaponType.Pulse,        "Pistol",   "Standard",   600, 12, 36,  16f, 0.12f, 130f),
                (WeaponType.Shard,        "Special",  "ShortRange", 1500, 20, 60,  20f, 0.05f, 100f),
            };

            foreach (var (type, category, visionClass, cost, mag, reserve, damage, cd, vision) in weaponData)
            {
                stats[type] = new WeaponStats
                {
                    Type = (RYZECHo.WeaponType)(int)type,
                    Label = type.ToString(),
                    ShortLabel = type.ToString().Substring(0, 3).ToUpper(),
                    Code = type.ToString().Substring(0, 3).ToUpper(),
                    Category = category,
                    VisionClass = visionClass,
                    Cost = cost,
                    MagazineAmmo = mag,
                    ReserveAmmo = reserve,
                    VisionRange = vision,
                    HearingMultiplier = vision / 180f,
                    FireCooldown = cd,
                    Damage = damage,
                    MoveSpeed = 100f,
                    ProjectileRange = 12f,
                    ScopedFov = visionClass == "LongRange",
                };
            }

            return stats;
        }
    }
}