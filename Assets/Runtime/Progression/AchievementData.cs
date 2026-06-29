using System;
using System.Collections.Generic;
namespace RYZECHo;

/// <summary>
/// アチーブメントデータ。ID、名前、説明、条件を定義。
/// </summary>
public sealed class AchievementData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AchievementCondition Condition { get; set; } = new();
    public int RewardAgentCredits { get; set; }
    public int RewardCosmeticTokens { get; set; }
    public bool Unlocked { get; set; }

    /// <summary>アチーブメントの条件。</summary>
    public record AchievementCondition
    {
        public string Type { get; set; } = string.Empty;
        public int Target { get; set; }
        public string? Actor { get; set; }
        public string? Agent { get; set; }
    }
}

/// <summary>
/// アチーブメントマニフェスト（静的定義）。
/// </summary>
public static class AchievementManifest
{
    private static readonly AchievementData[] _catalog = new AchievementData[]
    {
        new() { Id = "match_first_win", Name = "初勝利", Description = "最初のマッチに勝利しました", Condition = new() { Type = "matchesWon", Target = 1 }, RewardAgentCredits = 1, RewardCosmeticTokens = 0 },
        new() { Id = "match_10_wins", Name = "10勝", Description = "10回のマッチ勝利", Condition = new() { Type = "matchesWon", Target = 10 }, RewardAgentCredits = 2, RewardCosmeticTokens = 1 },
        new() { Id = "match_50_wins", Name = "50勝", Description = "50回のマッチ勝利", Condition = new() { Type = "matchesWon", Target = 50 }, RewardAgentCredits = 5, RewardCosmeticTokens = 3 },
        new() { Id = "level_5", Name = "レベル5到達", Description = "アカウントレベル5に到達", Condition = new() { Type = "accountLevel", Target = 5 }, RewardAgentCredits = 2, RewardCosmeticTokens = 1 },
        new() { Id = "level_10", Name = "レベル10到達", Description = "アカウントレベル10に到達", Condition = new() { Type = "accountLevel", Target = 10 }, RewardAgentCredits = 3, RewardCosmeticTokens = 2 },
        new() { Id = "level_20", Name = "レベル20到達", Description = "アカウントレベル20に到達", Condition = new() { Type = "accountLevel", Target = 20 }, RewardAgentCredits = 5, RewardCosmeticTokens = 5 },
        new() { Id = "contract_1", Name = "契約完了", Description = "最初の契約を完了", Condition = new() { Type = "contractsCompleted", Target = 1 }, RewardAgentCredits = 2, RewardCosmeticTokens = 1 },
        new() { Id = "contract_5", Name = "契約マスター", Description = "5回の契約を完了", Condition = new() { Type = "contractsCompleted", Target = 5 }, RewardAgentCredits = 3, RewardCosmeticTokens = 2 },
        new() { Id = "elimination_100", Name = "100キル", Description = "100回のキルを達成", Condition = new() { Type = "totalEliminations", Target = 100 }, RewardAgentCredits = 3, RewardCosmeticTokens = 2 },
        new() { Id = "elimination_500", Name = "500キル", Description = "500回のキルを達成", Condition = new() { Type = "totalEliminations", Target = 500 }, RewardAgentCredits = 5, RewardCosmeticTokens = 5 },
        new() { Id = "rank_iron", Name = "アイアンランク", Description = "アイアンランクに到達", Condition = new() { Type = "rankRating", Target = 100 }, RewardAgentCredits = 1, RewardCosmeticTokens = 1 },
        new() { Id = "rank_bronze", Name = "ブロンズランク", Description = "ブロンズランクに到達", Condition = new() { Type = "rankRating", Target = 240 }, RewardAgentCredits = 2, RewardCosmeticTokens = 1 },
        new() { Id = "rank_silver", Name = "シルバーランク", Description = "シルバーランクに到達", Condition = new() { Type = "rankRating", Target = 420 }, RewardAgentCredits = 3, RewardCosmeticTokens = 2 },
        new() { Id = "rank_gold", Name = "ゴールドランク", Description = "ゴールドランクに到達", Condition = new() { Type = "rankRating", Target = 640 }, RewardAgentCredits = 5, RewardCosmeticTokens = 3 },
        new() { Id = "rank_platinum", Name = "プラチナランク", Description = "プラチナランクに到達", Condition = new() { Type = "rankRating", Target = 900 }, RewardAgentCredits = 8, RewardCosmeticTokens = 5 },
        new() { Id = "store_purchase_1", Name = "初の購入", Description = "ストアで初めてアイテムを購入", Condition = new() { Type = "storePurchases", Target = 1 }, RewardAgentCredits = 1, RewardCosmeticTokens = 0 },
        new() { Id = "ad_impression_100", Name = "広告露出100", Description = "広告露出を100回達成", Condition = new() { Type = "lifetimeAdImpressions", Target = 100 }, RewardAgentCredits = 2, RewardCosmeticTokens = 3 },
        new() { Id = "skin_complete", Name = "スキンコレクション", Description = "すべての構造物スキンをアンロック", Condition = new() { Type = "structureSkinsUnlocked", Target = 5 }, RewardAgentCredits = 3, RewardCosmeticTokens = 3 },
        new() { Id = "banner_complete", Name = "バナーコレクション", Description = "すべてのバナーをアンロック", Condition = new() { Type = "bannersUnlocked", Target = 5 }, RewardAgentCredits = 3, RewardCosmeticTokens = 3 },
        new() { Id = "kill_effect_complete", Name = "キルエフェクトコレクション", Description = "すべてのキルエフェクトをアンロック", Condition = new() { Type = "killEffectsUnlocked", Target = 4 }, RewardAgentCredits = 3, RewardCosmeticTokens = 3 },
    };

    public static IReadOnlyList<AchievementData> All => _catalog;

    public static AchievementData? GetById(string id)
    {
        foreach (var a in _catalog)
        {
            if (a.Id == id) return a;
        }
        return null;
    }

    public static AchievementData Clone(string id)
    {
        var source = GetById(id) ?? throw new ArgumentException($"Achievement not found: {id}");
        return new AchievementData
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Condition = new AchievementData.AchievementCondition
            {
                Type = source.Condition.Type,
                Target = source.Condition.Target,
                Actor = source.Condition.Actor,
                Agent = source.Condition.Agent,
            },
            RewardAgentCredits = source.RewardAgentCredits,
            RewardCosmeticTokens = source.RewardCosmeticTokens,
            Unlocked = false,
        };
    }
}
