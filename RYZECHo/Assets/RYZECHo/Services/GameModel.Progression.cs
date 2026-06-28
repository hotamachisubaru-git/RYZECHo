using Newtonsoft.Json;

namespace RYZECHo;

public sealed partial class GameModel
{
    private static readonly JsonSerializerSettings ProgressJsonSettings = new()
    {
        ObjectCreationHandling = ObjectCreationHandling.Replace,
    };

    private static ProgressProfile LoadProgressProfile()
    {
        try
        {
            if (File.Exists(ProgressProfilePath()))
            {
                var json = File.ReadAllText(ProgressProfilePath());
                var profile = JsonConvert.DeserializeObject<ProgressProfile>(json, ProgressJsonSettings);
                if (profile is not null)
                {
                    if (HasValidProgressIntegrity(profile))
                    {
                        return profile;
                    }

                    profile.IntegritySalt = string.Empty;
                    profile.IntegrityStamp = string.Empty;
                    return profile;
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
        return new[] { "ヴェール", "ヴァイン", "ニトロ", "オアシス", "ディバイド", "グリッチ" };
    }

    private static string[] StructureSkinCatalog()
    {
        return new[] { "シグナル標準", "カーボンゲート", "サンドパルス", "プリズムバイザー", "ローグクローム" };
    }

    private static string[] AdThemeCatalog()
    {
        return new[] { "NEO CORE", "VERTEX CUP", "SUNSET GRID", "ARC LEAGUE" };
    }

    private static string[] BannerCatalog()
    {
        return new[] { "SIGNAL//STANDARD", "CONTRACT//ARC", "BOSS//BACKER", "MAP//ARCHITECT", "AD//PARTNER" };
    }

    private static string[] KillEffectCatalog()
    {
        return new[] { "SIGNAL BURST", "RIPPLE TRACE", "PRISM BREAK", "CLEAN CUT" };
    }

    private static CosmeticOffer[] CosmeticStoreCatalog()
    {
        return new CosmeticOffer[]
        {
            new(CosmeticKind.StructureSkin, "カーボンゲート", 3, "設置物スキン"),
            new(CosmeticKind.AdTheme, "VERTEX CUP", 3, "会場広告テーマ"),
            new(CosmeticKind.Banner, "BOSS//BACKER", 2, "プロフィールバナー"),
            new(CosmeticKind.KillEffect, "RIPPLE TRACE", 4, "キル演出"),
            new(CosmeticKind.StructureSkin, "プリズムバイザー", 5, "設置物スキン"),
            new(CosmeticKind.AdTheme, "ARC LEAGUE", 5, "会場広告テーマ"),
            new(CosmeticKind.Banner, "MAP//ARCHITECT", 4, "プロフィールバナー"),
            new(CosmeticKind.KillEffect, "PRISM BREAK", 6, "キル演出"),
            new(CosmeticKind.StructureSkin, "ローグクローム", 6, "設置物スキン"),
            new(CosmeticKind.Banner, "AD//PARTNER", 3, "プロフィールバナー"),
            new(CosmeticKind.KillEffect, "CLEAN CUT", 5, "キル演出"),
        };
    }

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

    private void SaveProgressProfile()
    {
        try
        {
            NormalizeProgressProfile();
            _profile.IntegrityStamp = CreateProgressIntegrityStamp(_profile);
            var json = JsonConvert.SerializeObject(_profile, Formatting.Indented, ProgressJsonSettings);
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
            EnsureUnlocked(_profile.UnlockedBanners, $"{_profile.ActiveContract} 契約章");

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

    private string SelectedBannerName()
    {
        return _profile.SelectedBanner;
    }

    private string SelectedKillEffectName()
    {
        return _profile.SelectedKillEffect;
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

    private CosmeticOffer CurrentStoreOffer()
    {
        var catalog = CosmeticStoreCatalog();
        var cursor = Math.Clamp(_profile.StoreCursor, 0, Math.Max(0, catalog.Length - 1));
        return catalog[cursor];
    }

    private string StoreOfferSummaryLine()
    {
        var offer = CurrentStoreOffer();
        var state = IsCosmeticUnlocked(offer) ? "所持済み" : $"{offer.TokenCost}CT";
        return $"STORE {offer.Label}: {offer.Name} ({state})";
    }

    private void TryPurchaseOrSelectStoreOffer()
    {
        NormalizeProgressProfile();
        var offer = CurrentStoreOffer();
        if (IsCosmeticUnlocked(offer))
        {
            SelectCosmetic(offer);
            AdvanceStoreCursor();
            SaveProgressProfile();
            SetResultMessage($"{offer.Name} を選択し、次のストア枠へ進めました。");
            return;
        }

        if (_profile.CosmeticTokens < offer.TokenCost)
        {
            SetResultMessage($"{offer.Name} の購入には {offer.TokenCost}CT が必要です。現在 {_profile.CosmeticTokens}CT。課金ではなく試合報酬と広告露出報酬で入手できます。");
            return;
        }

        _profile.CosmeticTokens -= offer.TokenCost;
        UnlockCosmetic(offer);
        SelectCosmetic(offer);
        AdvanceStoreCursor();
        SaveProgressProfile();
        SetResultMessage($"{offer.Name} を購入して選択しました。性能には影響しません。");
    }

    private void AdvanceStoreCursor()
    {
        var catalogLength = CosmeticStoreCatalog().Length;
        _profile.StoreCursor = (_profile.StoreCursor + 1) % Math.Max(1, catalogLength);
    }

    private bool IsCosmeticUnlocked(CosmeticOffer offer)
    {
        return offer.Kind switch
        {
            CosmeticKind.StructureSkin => _profile.UnlockedStructureSkins.Contains(offer.Name),
            CosmeticKind.AdTheme => _profile.UnlockedAdThemes.Contains(offer.Name),
            CosmeticKind.Banner => _profile.UnlockedBanners.Contains(offer.Name),
            CosmeticKind.KillEffect => _profile.UnlockedKillEffects.Contains(offer.Name),
            _ => false,
        };
    }

    private void UnlockCosmetic(CosmeticOffer offer)
    {
        switch (offer.Kind)
        {
            case CosmeticKind.StructureSkin:
                EnsureUnlocked(_profile.UnlockedStructureSkins, offer.Name);
                break;
            case CosmeticKind.AdTheme:
                EnsureUnlocked(_profile.UnlockedAdThemes, offer.Name);
                break;
            case CosmeticKind.Banner:
                EnsureUnlocked(_profile.UnlockedBanners, offer.Name);
                break;
            case CosmeticKind.KillEffect:
                EnsureUnlocked(_profile.UnlockedKillEffects, offer.Name);
                break;
        }
    }

    private void SelectCosmetic(CosmeticOffer offer)
    {
        switch (offer.Kind)
        {
            case CosmeticKind.StructureSkin:
                _profile.SelectedStructureSkin = offer.Name;
                break;
            case CosmeticKind.AdTheme:
                _profile.SelectedAdTheme = offer.Name;
                break;
            case CosmeticKind.Banner:
                _profile.SelectedBanner = offer.Name;
                break;
            case CosmeticKind.KillEffect:
                _profile.SelectedKillEffect = offer.Name;
                break;
        }
    }

    private void UpdateMonetizationRuntime(float deltaSeconds)
    {
        if (_phase != GamePhase.Hunt || IsIntegrityRewardsLocked())
        {
            return;
        }

        _adImpressionTimer += deltaSeconds;
        if (_adImpressionTimer < 12f)
        {
            return;
        }

        _adImpressionTimer = 0f;
        _profile.LifetimeAdImpressions++;
        if (_profile.LifetimeAdImpressions % 3 == 0)
        {
            _profile.CosmeticTokens++;
            SaveProgressProfile();
            PushActivityFeed($"会場広告露出報酬 +1CT。合計 {_profile.CosmeticTokens}CT。");
            return;
        }

        SaveProgressProfile();
    }

    private void EmitCosmeticEliminationEffect(PointF position)
    {
        var color = SelectedKillEffectName() switch
        {
            "RIPPLE TRACE" => Color.FromArgb(255, 124, 228, 255),
            "PRISM BREAK" => Color.FromArgb(255, 196, 132, 255),
            "CLEAN CUT" => Color.FromArgb(255, 238, 244, 248),
            _ => Color.FromArgb(255, 255, 220, 132),
        };

        EmitRipple(position, 1.12f, RippleKind.Skill, color);
    }
}
