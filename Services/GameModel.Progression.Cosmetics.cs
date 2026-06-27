namespace RYZECHo;

internal sealed partial class GameModel
{
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
