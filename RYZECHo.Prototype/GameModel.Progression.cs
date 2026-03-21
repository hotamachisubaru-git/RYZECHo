using System.Text.Json;

namespace RYZECHo.Prototype;

internal sealed class ProgressProfile
{
    public int AccountLevel { get; set; } = 1;
    public int CurrentXp { get; set; }
    public int AgentCredits { get; set; }
    public int RankRating { get; set; }
    public int MatchesPlayed { get; set; }
    public int MatchesWon { get; set; }
    public int ContractsCompleted { get; set; }
    public string ActiveContract { get; set; } = "ヴェール";
    public int ActiveContractProgress { get; set; }
    public List<string> UnlockedAgents { get; set; } = ["ヴェール"];
    public List<string> UnlockedStructureSkins { get; set; } = ["シグナル標準"];
    public List<string> UnlockedAdThemes { get; set; } = ["NEO CORE"];
    public List<string> UnlockedBanners { get; set; } = ["SIGNAL//STANDARD"];
    public string SelectedStructureSkin { get; set; } = "シグナル標準";
    public string SelectedAdTheme { get; set; } = "NEO CORE";
    public string IntegritySalt { get; set; } = string.Empty;
    public string IntegrityStamp { get; set; } = string.Empty;
}

internal sealed partial class GameModel
{
    private static readonly JsonSerializerOptions ProgressJsonOptions = new()
    {
        WriteIndented = true,
    };

    private static ProgressProfile LoadProgressProfile()
    {
        try
        {
            if (File.Exists(ProgressProfilePath()))
            {
                var json = File.ReadAllText(ProgressProfilePath());
                var profile = JsonSerializer.Deserialize<ProgressProfile>(json, ProgressJsonOptions);
                if (profile is not null)
                {
                    if (HasValidProgressIntegrity(profile))
                    {
                        return profile;
                    }

                    ForceTerminateForIntegrityViolation("保存データ整合性", "prototype-profile.json の署名検証に失敗しました。");
                }
            }
        }
        catch
        {
        }

        return new ProgressProfile();
    }

    private static string ProgressProfilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "prototype-profile.json");
    }

    private static string[] ContractOrder()
    {
        return ["ヴェール", "ヴァイン", "ニトロ", "オアシス", "ディバイド", "グリッチ"];
    }

    private static string[] StructureSkinCatalog()
    {
        return ["シグナル標準", "カーボンゲート", "サンドパルス"];
    }

    private static string[] AdThemeCatalog()
    {
        return ["NEO CORE", "VERTEX CUP", "SUNSET GRID"];
    }

    private void NormalizeProgressProfile()
    {
        _profile.AccountLevel = Math.Clamp(_profile.AccountLevel, 1, IntegrityMaxAccountLevel);
        _profile.AgentCredits = Math.Clamp(_profile.AgentCredits, 0, IntegrityMaxCareerStat);
        _profile.RankRating = Math.Clamp(_profile.RankRating, 0, IntegrityMaxCareerStat);
        _profile.CurrentXp = Math.Max(0, _profile.CurrentXp);
        _profile.MatchesPlayed = Math.Clamp(_profile.MatchesPlayed, 0, IntegrityMaxCareerStat);
        _profile.MatchesWon = Math.Clamp(_profile.MatchesWon, 0, _profile.MatchesPlayed);
        _profile.ContractsCompleted = Math.Clamp(_profile.ContractsCompleted, 0, IntegrityMaxCareerStat);
        _profile.ActiveContractProgress = Math.Clamp(_profile.ActiveContractProgress, 0, 11);
        _profile.UnlockedAgents ??= [];
        _profile.UnlockedStructureSkins ??= [];
        _profile.UnlockedAdThemes ??= [];
        _profile.UnlockedBanners ??= [];

        NormalizeProgressList(_profile.UnlockedAgents, ContractOrder());
        NormalizeProgressList(_profile.UnlockedStructureSkins, StructureSkinCatalog());
        NormalizeProgressList(_profile.UnlockedAdThemes, AdThemeCatalog());
        NormalizeLooseProgressList(_profile.UnlockedBanners);

        EnsureUnlocked(_profile.UnlockedAgents, "ヴェール");
        EnsureUnlocked(_profile.UnlockedStructureSkins, "シグナル標準");
        EnsureUnlocked(_profile.UnlockedAdThemes, "NEO CORE");
        EnsureUnlocked(_profile.UnlockedBanners, "SIGNAL//STANDARD");
        UnlockProgressionRewards();

        if (!_profile.UnlockedStructureSkins.Contains(_profile.SelectedStructureSkin))
        {
            _profile.SelectedStructureSkin = _profile.UnlockedStructureSkins[0];
        }

        if (!_profile.UnlockedAdThemes.Contains(_profile.SelectedAdTheme))
        {
            _profile.SelectedAdTheme = _profile.UnlockedAdThemes[0];
        }

        if (!ContractOrder().Contains(_profile.ActiveContract))
        {
            _profile.ActiveContract = ContractOrder()[0];
        }

        _profile.IntegritySalt = EnsureProgressSalt(_profile.IntegritySalt);
    }

    private static void EnsureUnlocked(List<string> unlockedList, string reward)
    {
        if (!unlockedList.Contains(reward))
        {
            unlockedList.Add(reward);
        }
    }

    private void UnlockProgressionRewards()
    {
        if (_profile.AccountLevel >= 2)
        {
            EnsureUnlocked(_profile.UnlockedAgents, "ヴァイン");
            EnsureUnlocked(_profile.UnlockedStructureSkins, "カーボンゲート");
        }

        if (_profile.AccountLevel >= 3)
        {
            EnsureUnlocked(_profile.UnlockedAgents, "ニトロ");
            EnsureUnlocked(_profile.UnlockedAdThemes, "VERTEX CUP");
        }

        if (_profile.AccountLevel >= 4)
        {
            EnsureUnlocked(_profile.UnlockedAgents, "オアシス");
            EnsureUnlocked(_profile.UnlockedBanners, "CONTRACT//ARC");
        }

        if (_profile.AccountLevel >= 5)
        {
            EnsureUnlocked(_profile.UnlockedAgents, "ディバイド");
            EnsureUnlocked(_profile.UnlockedStructureSkins, "サンドパルス");
        }

        if (_profile.AccountLevel >= 6)
        {
            EnsureUnlocked(_profile.UnlockedAgents, "グリッチ");
            EnsureUnlocked(_profile.UnlockedAdThemes, "SUNSET GRID");
        }
    }

    private static int ExperienceForNextLevel(int level)
    {
        return 180 + ((Math.Max(1, level) - 1) * 120);
    }

    private void SaveProgressProfile()
    {
        try
        {
            NormalizeProgressProfile();
            _profile.IntegrityStamp = CreateProgressIntegrityStamp(_profile);
            var json = JsonSerializer.Serialize(_profile, ProgressJsonOptions);
            File.WriteAllText(ProgressProfilePath(), json);
        }
        catch
        {
        }
    }

    private void AwardMatchProgression(bool won)
    {
        if (IsIntegrityRewardsLocked())
        {
            _lastProgressionSummary = IntegrityRewardLockSummary();
            return;
        }

        NormalizeProgressProfile();

        var roundGap = _playerRoundWins - _enemyRoundWins;
        var xpGain = 180 + (_playerRoundWins * 28) + (_enemyRoundWins * 14) + (_matchTeamEliminations * 12) + (won ? 120 : 55);
        var rankDelta = (won ? 30 : -16) + (roundGap * 4) - (_matchPlayerDeaths * 2);

        _profile.MatchesPlayed++;
        if (won)
        {
            _profile.MatchesWon++;
        }

        _profile.AgentCredits += won ? 2 : 1;
        _profile.RankRating = Math.Max(0, _profile.RankRating + rankDelta);
        _profile.CurrentXp += xpGain;
        _profile.ActiveContractProgress += Math.Max(1, (won ? 3 : 1) + (_matchTeamEliminations / 4));

        var levelUps = 0;
        while (_profile.CurrentXp >= ExperienceForNextLevel(_profile.AccountLevel))
        {
            _profile.CurrentXp -= ExperienceForNextLevel(_profile.AccountLevel);
            _profile.AccountLevel++;
            _profile.AgentCredits++;
            levelUps++;
            UnlockProgressionRewards();
        }

        if (_profile.ActiveContractProgress >= 12)
        {
            _profile.ActiveContractProgress -= 12;
            _profile.ContractsCompleted++;
            _profile.AgentCredits += 2;
            EnsureUnlocked(_profile.UnlockedBanners, $"{_profile.ActiveContract} 契約章");

            var contractIndex = Array.IndexOf(ContractOrder(), _profile.ActiveContract);
            _profile.ActiveContract = ContractOrder()[(contractIndex + 1 + ContractOrder().Length) % ContractOrder().Length];
        }

        UnlockProgressionRewards();
        NormalizeProgressProfile();
        SaveProgressProfile();

        _lastProgressionSummary = $"XP +{xpGain} / LV {_profile.AccountLevel}{(levelUps > 0 ? $" (+{levelUps})" : string.Empty)} / {CurrentRankLabel()} / AGC {_profile.AgentCredits}";
    }

    private string CurrentRankLabel()
    {
        return _profile.RankRating switch
        {
            < 100 => "UNRANKED",
            < 240 => "IRON",
            < 420 => "BRONZE",
            < 640 => "SILVER",
            < 900 => "GOLD",
            < 1200 => "PLATINUM",
            _ => "DIAMOND",
        };
    }

    private string ProfileSummaryLine()
    {
        return $"LV {_profile.AccountLevel}  {CurrentRankLabel()}  AGC {_profile.AgentCredits}";
    }

    private string ContractSummaryLine()
    {
        return $"契約 {_profile.ActiveContract} {_profile.ActiveContractProgress}/12";
    }

    private string SelectedStructureSkinName()
    {
        return _profile.SelectedStructureSkin;
    }

    private string SelectedAdThemeName()
    {
        return _profile.SelectedAdTheme;
    }

    private void CycleStructureSkin(int direction)
    {
        NormalizeProgressProfile();
        var catalog = _profile.UnlockedStructureSkins
            .Where(StructureSkinCatalog().Contains)
            .ToList();
        if (catalog.Count == 0)
        {
            return;
        }

        var index = catalog.IndexOf(_profile.SelectedStructureSkin);
        if (index < 0)
        {
            index = 0;
        }

        _profile.SelectedStructureSkin = catalog[(index + direction + catalog.Count) % catalog.Count];
        SaveProgressProfile();
        SetResultMessage($"設置物スキンを {_profile.SelectedStructureSkin} に切り替えました。");
    }

    private void CycleAdTheme()
    {
        NormalizeProgressProfile();
        var catalog = _profile.UnlockedAdThemes
            .Where(AdThemeCatalog().Contains)
            .ToList();
        if (catalog.Count == 0)
        {
            return;
        }

        var index = catalog.IndexOf(_profile.SelectedAdTheme);
        if (index < 0)
        {
            index = 0;
        }

        _profile.SelectedAdTheme = catalog[(index + 1) % catalog.Count];
        SaveProgressProfile();
        SetResultMessage($"会場広告テーマを {_profile.SelectedAdTheme} に切り替えました。");
    }
}
