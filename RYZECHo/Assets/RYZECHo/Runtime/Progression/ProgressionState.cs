using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RYZECHo;

/// <summary>
/// プログレス状態データ。アカウントレベル、XP、報酬を含む。
/// </summary>
public sealed class ProgressionState
{
    [JsonPropertyName("accountLevel")]
    public int AccountLevel { get; set; } = 1;

    [JsonPropertyName("currentXp")]
    public int CurrentXp { get; set; }

    [JsonPropertyName("agentCredits")]
    public int AgentCredits { get; set; }

    [JsonPropertyName("cosmeticTokens")]
    public int CosmeticTokens { get; set; }

    [JsonPropertyName("rankRating")]
    public int RankRating { get; set; }

    [JsonPropertyName("matchesPlayed")]
    public int MatchesPlayed { get; set; }

    [JsonPropertyName("matchesWon")]
    public int MatchesWon { get; set; }

    [JsonPropertyName("contractsCompleted")]
    public int ContractsCompleted { get; set; }

    [JsonPropertyName("activeContract")]
    public string ActiveContract { get; set; } = "ヴェール";

    [JsonPropertyName("activeContractProgress")]
    public int ActiveContractProgress { get; set; }

    [JsonPropertyName("unlockedAgents")]
    public List<string> UnlockedAgents { get; set; } = new List<string> { "ヴェール" };

    [JsonPropertyName("unlockedStructureSkins")]
    public List<string> UnlockedStructureSkins { get; set; } = new List<string> { "シグナル標準" };

    [JsonPropertyName("unlockedAdThemes")]
    public List<string> UnlockedAdThemes { get; set; } = new List<string> { "NEO CORE" };

    [JsonPropertyName("unlockedBanners")]
    public List<string> UnlockedBanners { get; set; } = new List<string> { "SIGNAL//STANDARD" };

    [JsonPropertyName("unlockedKillEffects")]
    public List<string> UnlockedKillEffects { get; set; } = new List<string> { "SIGNAL BURST" };

    [JsonPropertyName("selectedStructureSkin")]
    public string SelectedStructureSkin { get; set; } = "シグナル標準";

    [JsonPropertyName("selectedAdTheme")]
    public string SelectedAdTheme { get; set; } = "NEO CORE";

    [JsonPropertyName("selectedBanner")]
    public string SelectedBanner { get; set; } = "SIGNAL//STANDARD";

    [JsonPropertyName("selectedKillEffect")]
    public string SelectedKillEffect { get; set; } = "SIGNAL BURST";

    [JsonPropertyName("storeCursor")]
    public int StoreCursor { get; set; }

    [JsonPropertyName("lifetimeAdImpressions")]
    public int LifetimeAdImpressions { get; set; }

    [JsonPropertyName("difficultyModifier")]
    public float DifficultyModifier { get; set; } = 1.0f;

    [JsonPropertyName("integritySalt")]
    public string IntegritySalt { get; set; } = string.Empty;

    [JsonPropertyName("integrityStamp")]
    public string IntegrityStamp { get; set; } = string.Empty;
}
