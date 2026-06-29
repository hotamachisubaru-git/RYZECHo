namespace RYZECHo;

internal sealed class ProgressProfile
{
    public int AccountLevel { get; set; } = 1;
    public int CurrentXp { get; set; }
    public int AgentCredits { get; set; }
    public int RankRating { get; set; }
    public int MatchesPlayed { get; set; }
    public int MatchesWon { get; set; }
    public int ContractsCompleted { get; set; }
    public int CosmeticTokens { get; set; }
    public int LifetimeAdImpressions { get; set; }
    public int StoreCursor { get; set; }
    public string ActiveContract { get; set; } = "ヴェール";
    public int ActiveContractProgress { get; set; }
    public List<string> UnlockedAgents { get; set; } = new List<string> { "ヴェール" };
    public List<string> UnlockedStructureSkins { get; set; } = new List<string> { "シグナル標準" };
    public List<string> UnlockedAdThemes { get; set; } = new List<string> { "NEO CORE" };
    public List<string> UnlockedBanners { get; set; } = new List<string> { "SIGNAL//STANDARD" };
    public List<string> UnlockedKillEffects { get; set; } = new List<string> { "SIGNAL BURST" };
    public string SelectedStructureSkin { get; set; } = "シグナル標準";
    public string SelectedAdTheme { get; set; } = "NEO CORE";
    public string SelectedBanner { get; set; } = "SIGNAL//STANDARD";
    public string SelectedKillEffect { get; set; } = "SIGNAL BURST";
    public string IntegritySalt { get; set; } = string.Empty;
    public string IntegrityStamp { get; set; } = string.Empty;
}
