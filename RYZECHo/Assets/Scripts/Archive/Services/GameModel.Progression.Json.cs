using System.Text.Json;

namespace RYZECHo;

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

    private static void SaveProgressProfile()
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

    private static string[] ContractOrder()
    {
        return ["ヴェール", "ヴァイン", "ニトロ", "オアシス", "ディバイド", "グリッチ"];
    }

    private static string[] StructureSkinCatalog()
    {
        return ["シグナル標準", "カーボンゲート", "サンドパルス", "プリズムバイザー", "ローグクローム"];
    }

    private static string[] AdThemeCatalog()
    {
        return ["NEO CORE", "VERTEX CUP", "SUNSET GRID", "ARC LEAGUE"];
    }

    private static string[] BannerCatalog()
    {
        return ["SIGNAL//STANDARD", "CONTRACT//ARC", "BOSS//BACKER", "MAP//ARCHITECT", "AD//PARTNER"];
    }

    private static string[] KillEffectCatalog()
    {
        return ["SIGNAL BURST", "RIPPLE TRACE", "PRISM BREAK", "CLEAN CUT"];
    }

    private static CosmeticOffer[] CosmeticStoreCatalog()
    {
        return
        [
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
        ];
    }
}
