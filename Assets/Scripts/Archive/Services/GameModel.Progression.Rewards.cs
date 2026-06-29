namespace RYZECHo;

internal sealed partial class GameModel
{
    private void NormalizeProgressProfile()
    {
        _profile.AccountLevel = Math.Clamp(_profile.AccountLevel, 1, IntegrityMaxAccountLevel);
        _profile.AgentCredits = Math.Clamp(_profile.AgentCredits, 0, IntegrityMaxCareerStat);
        _profile.CosmeticTokens = Math.Clamp(_profile.CosmeticTokens, 0, IntegrityMaxCareerStat);
        _profile.LifetimeAdImpressions = Math.Clamp(_profile.LifetimeAdImpressions, 0, IntegrityMaxCareerStat);
        _profile.StoreCursor = Math.Clamp(_profile.StoreCursor, 0, Math.Max(0, CosmeticStoreCatalog().Length - 1));
        _profile.RankRating = Math.Clamp(_profile.RankRating, 0, IntegrityMaxCareerStat);
        _profile.CurrentXp = Math.Max(0, _profile.CurrentXp);
        _profile.MatchesPlayed = Math.Clamp(_profile.MatchesPlayed, 0, IntegrityMaxCareerStat);
        _profile.MatchesWon = Math.Clamp(_profile.MatchesWon, 0, _profile.MatchesPlayed);
        _profile.ContractsCompleted = Math.Clamp(_profile.ContractsCompleted, 0, IntegrityMaxCareerStat);
        _profile.ActiveContractProgress = Math.Clamp(_profile.ActiveContractProgress, 0, 11);
        _profile.UnlockedAgents ??= new List<string>();
        _profile.UnlockedStructureSkins ??= new List<string>();
        _profile.UnlockedAdThemes ??= new List<string>();
        _profile.UnlockedBanners ??= new List<string>();
        _profile.UnlockedKillEffects ??= new List<string>();

        NormalizeProgressList(_profile.UnlockedAgents, ContractOrder());
        NormalizeProgressList(_profile.UnlockedStructureSkins, StructureSkinCatalog());
        NormalizeProgressList(_profile.UnlockedAdThemes, AdThemeCatalog());
        NormalizeLooseProgressList(_profile.UnlockedBanners);
        NormalizeProgressList(_profile.UnlockedKillEffects, KillEffectCatalog());

        EnsureUnlocked(_profile.UnlockedAgents, "ヴェール");
        EnsureUnlocked(_profile.UnlockedStructureSkins, "シグナル標準");
        EnsureUnlocked(_profile.UnlockedAdThemes, "NEO CORE");
        EnsureUnlocked(_profile.UnlockedBanners, "SIGNAL//STANDARD");
        EnsureUnlocked(_profile.UnlockedKillEffects, "SIGNAL BURST");
        UnlockProgressionRewards();

        if (!_profile.UnlockedStructureSkins.Contains(_profile.SelectedStructureSkin))
        {
            _profile.SelectedStructureSkin = _profile.UnlockedStructureSkins[0];
        }

        if (!_profile.UnlockedAdThemes.Contains(_profile.SelectedAdTheme))
        {
            _profile.SelectedAdTheme = _profile.UnlockedAdThemes[0];
        }

        if (!_profile.UnlockedBanners.Contains(_profile.SelectedBanner))
        {
            _profile.SelectedBanner = _profile.UnlockedBanners[0];
        }

        if (!_profile.UnlockedKillEffects.Contains(_profile.SelectedKillEffect))
        {
            _profile.SelectedKillEffect = _profile.UnlockedKillEffects[0];
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

        if (_profile.ContractsCompleted >= 1)
        {
            EnsureUnlocked(_profile.UnlockedKillEffects, "RIPPLE TRACE");
        }

        if (_profile.ContractsCompleted >= 2)
        {
            EnsureUnlocked(_profile.UnlockedBanners, "BOSS//BACKER");
        }
    }

    private static int ExperienceForNextLevel(int level)
    {
        return 180 + ((Math.Max(1, level) - 1) * 120);
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
        _profile.CosmeticTokens += won ? 3 : 2;
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
            EnsureUnlocked(_profile.UnlockedBanners, $"{_profile.ActiveContract} 螂醍ｴ・ｫ");

            var contractIndex = Array.IndexOf(ContractOrder(), _profile.ActiveContract);
            _profile.ActiveContract = ContractOrder()[(contractIndex + 1 + ContractOrder().Length) % ContractOrder().Length];
        }

        UnlockProgressionRewards();
        NormalizeProgressProfile();
        SaveProgressProfile();

        _lastProgressionSummary = $"XP +{xpGain} / LV {_profile.AccountLevel}{(levelUps > 0 ? $" (+{levelUps})" : string.Empty)} / {CurrentRankLabel()} / AGC {_profile.AgentCredits} / CT {_profile.CosmeticTokens}";
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
        return $"LV {_profile.AccountLevel}  {CurrentRankLabel()}  AGC {_profile.AgentCredits}  CT {_profile.CosmeticTokens}";
    }

    private string ContractSummaryLine()
    {
        return $"螂醍ｴ・{_profile.ActiveContract} {_profile.ActiveContractProgress}/12";
    }
}
