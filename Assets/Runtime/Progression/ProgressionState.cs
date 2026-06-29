using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RYZECHo;

/// <summary>
/// プログレス状態データ。アカウントレベル、XP、報酬を含む。
/// </summary>
public sealed class ProgressionState
{
    [JsonProperty("accountLevel")]
    public int AccountLevel { get; set; } = 1;

    [JsonProperty("currentXp")]
    public int CurrentXp { get; set; }

    [JsonProperty("agentCredits")]
    public int AgentCredits { get; set; }

    [JsonProperty("cosmeticTokens")]
    public int CosmeticTokens { get; set; }

    [JsonProperty("rankRating")]
    public int RankRating { get; set; }

    [JsonProperty("matchesPlayed")]
    public int MatchesPlayed { get; set; }

    [JsonProperty("matchesWon")]
    public int MatchesWon { get; set; }

    [JsonProperty("contractsCompleted")]
    public int ContractsCompleted { get; set; }

    [JsonProperty("activeContract")]
    public string ActiveContract { get; set; } = "ヴェール";

    [JsonProperty("activeContractProgress")]
    public int ActiveContractProgress { get; set; }

    [JsonProperty("unlockedAgents")]
    public List<string> UnlockedAgents { get; set; } = new List<string> { "ヴェール" };

    [JsonProperty("unlockedStructureSkins")]
    public List<string> UnlockedStructureSkins { get; set; } = new List<string> { "シグナル標準" };

    [JsonProperty("unlockedAdThemes")]
    public List<string> UnlockedAdThemes { get; set; } = new List<string> { "NEO CORE" };

    [JsonProperty("unlockedBanners")]
    public List<string> UnlockedBanners { get; set; } = new List<string> { "SIGNAL//STANDARD" };

    [JsonProperty("unlockedKillEffects")]
    public List<string> UnlockedKillEffects { get; set; } = new List<string> { "SIGNAL BURST" };

    [JsonProperty("selectedStructureSkin")]
    public string SelectedStructureSkin { get; set; } = "シグナル標準";

    [JsonProperty("selectedAdTheme")]
    public string SelectedAdTheme { get; set; } = "NEO CORE";

    [JsonProperty("selectedBanner")]
    public string SelectedBanner { get; set; } = "SIGNAL//STANDARD";

    [JsonProperty("selectedKillEffect")]
    public string SelectedKillEffect { get; set; } = "SIGNAL BURST";

    [JsonProperty("storeCursor")]
    public int StoreCursor { get; set; }

    [JsonProperty("lifetimeAdImpressions")]
    public int LifetimeAdImpressions { get; set; }

    [JsonProperty("difficultyModifier")]
    public float DifficultyModifier { get; set; } = 1.0f;

    [JsonProperty("integritySalt")]
    public string IntegritySalt { get; set; } = string.Empty;

    [JsonProperty("integrityStamp")]
    public string IntegrityStamp { get; set; } = string.Empty;
}
