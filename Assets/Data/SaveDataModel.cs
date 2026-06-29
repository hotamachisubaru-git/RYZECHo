using System;
using System.Collections.Generic;
using UnityEngine;

namespace RYZECHo.Data
{
    /// <summary>
    /// ゲーム全体のセーブデータ構造。
    /// ProgressProfileのデータをJSONシリアライズ可能な形式に変換する。
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public string saveVersion = "1.0.0";
        public string gameName = "RYZECHo";
        public long saveTimestamp;
        public PlayerSaveData player;
        public WorldSaveData world;
        public GameSettingsSaveData settings;

        public GameSaveData()
        {
            saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            player = new PlayerSaveData();
            world = new WorldSaveData();
            settings = new GameSettingsSaveData();
        }

        /// <summary>ProgressProfileからデータを転送</summary>
        public void FromProgressProfile(ProgressProfile profile)
        {
            player.accountLevel = profile.AccountLevel;
            player.currentXp = profile.CurrentXp;
            player.agentCredits = profile.AgentCredits;
            player.rankRating = profile.RankRating;
            player.matchesPlayed = profile.MatchesPlayed;
            player.matchesWon = profile.MatchesWon;
            player.contractsCompleted = profile.ContractsCompleted;
            player.cosmeticTokens = profile.CosmeticTokens;
            player.lifetimeAdImpressions = profile.LifetimeAdImpressions;
            player.storeCursor = profile.StoreCursor;
            player.activeContract = profile.ActiveContract;
            player.activeContractProgress = profile.ActiveContractProgress;
            player.unlockedAgents = new List<string>(profile.UnlockedAgents);
            player.unlockedStructureSkins = new List<string>(profile.UnlockedStructureSkins);
            player.unlockedAdThemes = new List<string>(profile.UnlockedAdThemes);
            player.unlockedBanners = new List<string>(profile.UnlockedBanners);
            player.unlockedKillEffects = new List<string>(profile.UnlockedKillEffects);
            player.selectedStructureSkin = profile.SelectedStructureSkin;
            player.selectedAdTheme = profile.SelectedAdTheme;
            player.selectedBanner = profile.SelectedBanner;
            player.selectedKillEffect = profile.SelectedKillEffect;
            player.integritySalt = profile.IntegritySalt;
            player.integrityStamp = profile.IntegrityStamp;
        }

        /// <summary>ProgressProfileにデータを転送</summary>
        public void ToProgressProfile(ProgressProfile profile)
        {
            profile.AccountLevel = player.accountLevel;
            profile.CurrentXp = player.currentXp;
            profile.AgentCredits = player.agentCredits;
            profile.RankRating = player.rankRating;
            profile.MatchesPlayed = player.matchesPlayed;
            profile.MatchesWon = player.matchesWon;
            profile.ContractsCompleted = player.contractsCompleted;
            profile.CosmeticTokens = player.cosmeticTokens;
            profile.LifetimeAdImpressions = player.lifetimeAdImpressions;
            profile.StoreCursor = player.storeCursor;
            profile.ActiveContract = player.activeContract;
            profile.ActiveContractProgress = player.activeContractProgress;
            profile.UnlockedAgents = player.unlockedAgents ?? new List<string>();
            profile.UnlockedStructureSkins = player.unlockedStructureSkins ?? new List<string>();
            profile.UnlockedAdThemes = player.unlockedAdThemes ?? new List<string>();
            profile.UnlockedBanners = player.unlockedBanners ?? new List<string>();
            profile.UnlockedKillEffects = player.unlockedKillEffects ?? new List<string>();
            profile.SelectedStructureSkin = player.selectedStructureSkin;
            profile.SelectedAdTheme = player.selectedAdTheme;
            profile.SelectedBanner = player.selectedBanner;
            profile.SelectedKillEffect = player.selectedKillEffect;
            profile.IntegritySalt = player.integritySalt;
            profile.IntegrityStamp = player.integrityStamp;
        }

        /// <summary>デフォルトの新しいセーブデータを作成</summary>
        public static GameSaveData CreateDefault()
        {
            var data = new GameSaveData();
            data.player = new PlayerSaveData
            {
                accountLevel = 1,
                currentXp = 0,
                agentCredits = 0,
                rankRating = 1000,
                activeContract = "ヴェール",
                activeContractProgress = 0,
                unlockedAgents = new List<string> { "ヴェール" },
                unlockedStructureSkins = new List<string> { "シグナル標準" },
                unlockedAdThemes = new List<string> { "NEO CORE" },
                unlockedBanners = new List<string> { "SIGNAL//STANDARD" },
                unlockedKillEffects = new List<string> { "SIGNAL BURST" },
                selectedStructureSkin = "シグナル標準",
                selectedAdTheme = "NEO CORE",
                selectedBanner = "SIGNAL//STANDARD",
                selectedKillEffect = "SIGNAL BURST",
            };
            data.world = WorldSaveData.CreateDefault();
            data.settings = new GameSettingsSaveData();
            return data;
        }
    }

    /// <summary>
    /// プレイヤー固有のセーブデータ。
    /// ProgressProfileの全フィールドをカバーする。
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        // --- Level & Progression ---
        public int accountLevel = 1;
        public int currentXp = 0;
        public int agentCredits = 0;
        public int rankRating = 1000;

        // --- Match Statistics ---
        public int matchesPlayed = 0;
        public int matchesWon = 0;
        public int contractsCompleted = 0;

        // --- Cosmetics ---
        public int cosmeticTokens = 0;
        public int lifetimeAdImpressions = 0;
        public int storeCursor = 0;

        // --- Contract ---
        public string activeContract = "ヴェール";
        public int activeContractProgress = 0;

        // --- Unlocks ---
        public List<string> unlockedAgents = new List<string>();
        public List<string> unlockedStructureSkins = new List<string>();
        public List<string> unlockedAdThemes = new List<string>();
        public List<string> unlockedBanners = new List<string>();
        public List<string> unlockedKillEffects = new List<string>();

        // --- Selected Cosmetics ---
        public string selectedStructureSkin = "シグナル標準";
        public string selectedAdTheme = "NEO CORE";
        public string selectedBanner = "SIGNAL//STANDARD";
        public string selectedKillEffect = "SIGNAL BURST";

        // --- Integrity ---
        public string integritySalt = string.Empty;
        public string integrityStamp = string.Empty;

        public bool HasIntegrityData()
        {
            return !string.IsNullOrWhiteSpace(integritySalt) && !string.IsNullOrWhiteSpace(integrityStamp);
        }
    }

    /// <summary>
    /// ワールド固有のセーブデータ。
    /// マップ状態、ビルド済み構造物、ゲーム進行状態を保存する。
    /// </summary>
    [Serializable]
    public class WorldSaveData
    {
        public string mapId = string.Empty;
        public int currentRound = 0;
        public int roundsToWin = 7;
        public GamePhase currentPhase;

        // ゲーム状態
        public int playerRoundWins = 0;
        public int enemyRoundWins = 0;

        // プレイヤー状態
        public float playerHealth = 100f;
        public float playerMaxHealth = 100f;
        public float playerShield = 0f;
        public float playerMaxShield = 0f;
        public float playerPositionX = 0f;
        public float playerPositionY = 0f;
        public float playerFacingDegrees = 0f;

        // クレジット & ポイント
        public int credits = 1000;
        public int ultPoints = 0;
        public int buildPoints = 12;

        // 構造物リスト（シリアライズ用）
        public List<StructureSaveEntry> structures = new List<StructureSaveEntry>();

        // マップレイアウト
        public string mapLayoutSeed = string.Empty;

        /// <summary>デフォルトのワールドデータを生成</summary>
        public static WorldSaveData CreateDefault()
        {
            return new WorldSaveData
            {
                mapId = "default",
                currentRound = 0,
                roundsToWin = 7,
                currentPhase = GamePhase.Construct,
                playerRoundWins = 0,
                enemyRoundWins = 0,
                playerHealth = 100f,
                playerMaxHealth = 100f,
                playerShield = 0f,
                playerMaxShield = 0f,
                credits = GameRules.StartingCredits,
                ultPoints = 0,
                buildPoints = GameRules.InitialBuildPoints,
            };
        }

        /// <summary>デフォルトの新しいワールドデータ</summary>
        public static WorldSaveData NewDefault() => CreateDefault();
    }

    /// <summary>構造物のセーブエントリ</summary>
    [Serializable]
    public class StructureSaveEntry
    {
        public string structureType;
        public int gridX;
        public int gridY;
        public float currentHealth;
        public float maxHealth;
        public bool isDestroyed;

        public StructureSaveEntry() { }

        public StructureSaveEntry(string type, int x, int y, float health, float maxHealth, bool destroyed)
        {
            structureType = type;
            gridX = x;
            gridY = y;
            currentHealth = health;
            maxHealth = maxHealth;
            isDestroyed = destroyed;
        }
    }

    /// <summary>
    /// ゲーム設定のセーブデータ。
    /// 現在の設定値を保存・復元する。
    /// </summary>
    [Serializable]
    public class GameSettingsSaveData
    {
        // ビジュアル設定
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public float musicVolume = 1f;
        public bool isVsyncEnabled = false;
        public int targetFrameRate = 60;

        // ゲームプレイ設定
        public float fovDegrees = 100f;
        public int difficulty = 0;

        // UI設定
        public float uiScale = 1f;
        public bool showCrosshair = true;
        public bool showHealthBars = true;
        public bool showMinimap = true;

        // その他
        public string language = "ja";
        public string lastSavedMap = string.Empty;
    }
}
