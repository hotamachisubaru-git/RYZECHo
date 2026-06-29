using UnityEngine;

namespace RYZECHo;

/// <summary>
/// 経済バランス調整用設定。
/// 各通貨の初期値、イベントごとの変動率、難易度に応じた調整を管理する。
/// </summary>
public sealed class EconomyBalanceConfig
{
    // --- 通貨初期値 ---
    public int StartingCredits { get; set; } = GameRules.StartingCredits;
    public int WinRewardCredits { get; set; } = GameRules.WinRewardCredits;
    public int LossRewardCredits { get; set; } = GameRules.LossRewardCredits;
    public int KillRewardCredits { get; set; } = GameRules.KillRewardCredits;
    public int ObjectiveRewardCredits { get; set; } = GameRules.ObjectiveRewardCredits;
    public int BossKillDividendCredits { get; set; } = GameRules.BossKillDividendCredits;
    public int BossEliminationBonusCredits { get; set; } = GameRules.BossEliminationBonusCredits;
    public int AgentSkillPurchaseCost { get; set; } = 400;
    public int MaxUltPoints { get; set; } = GameRules.MaxUltPoints;
    public int OptimalBossInvestment { get; set; } = GameRules.OptimalBossInvestment;
    public int BossPayoutMultiplier { get; set; } = GameRules.BossPayoutMultiplier;
    public int InitialBuildPoints { get; set; } = GameRules.InitialBuildPoints;
    public int MaxBuildPoints { get; set; } = GameRules.MaxBuildPoints;

    // --- インフレ/デフレーション率 ---
    public float CreditInflationRate { get; set; } = 1.0f;
    public float BuildPointInflationRate { get; set; } = 1.0f;

    // --- 難易度別バランス調整 ---
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Custom
    }

    public Difficulty CurrentDifficulty { get; set; } = Difficulty.Normal;

    // Easy: 報酬+20%, 投資効率-10%
    public float EasyRewardMultiplier => 1.2f;
    public float EasyInvestmentEfficiency => 0.9f;

    // Normal: 標準
    public float NormalRewardMultiplier => 1.0f;
    public float NormalInvestmentEfficiency => 1.0f;

    // Hard: 報酬-20%, 投資効率+20%
    public float HardRewardMultiplier => 0.8f;
    public float HardInvestmentEfficiency => 1.2f;

    /// <summary>現在の難易度に応じた報酬倍率を取得。</summary>
    public float GetRewardMultiplier()
    {
        return CurrentDifficulty switch
        {
            Difficulty.Easy => EasyRewardMultiplier,
            Difficulty.Hard => HardRewardMultiplier,
            _ => NormalRewardMultiplier,
        };
    }

    /// <summary>現在の難易度に応じた投資効率を取得。</summary>
    public float GetInvestmentEfficiency()
    {
        return CurrentDifficulty switch
        {
            Difficulty.Easy => EasyInvestmentEfficiency,
            Difficulty.Hard => HardInvestmentEfficiency,
            _ => NormalInvestmentEfficiency,
        };
    }

    /// <summary>難易度変更時に初期値を調整。</summary>
    public void ApplyDifficulty(Difficulty difficulty)
    {
        CurrentDifficulty = difficulty;
        switch (difficulty)
        {
            case Difficulty.Easy:
                WinRewardCredits = (int)(GameRules.WinRewardCredits * EasyRewardMultiplier);
                LossRewardCredits = (int)(GameRules.LossRewardCredits * EasyRewardMultiplier);
                KillRewardCredits = (int)(GameRules.KillRewardCredits * EasyRewardMultiplier);
                OptimalBossInvestment = (int)(GameRules.OptimalBossInvestment * EasyInvestmentEfficiency);
                break;
            case Difficulty.Normal:
                WinRewardCredits = GameRules.WinRewardCredits;
                LossRewardCredits = GameRules.LossRewardCredits;
                KillRewardCredits = GameRules.KillRewardCredits;
                OptimalBossInvestment = GameRules.OptimalBossInvestment;
                break;
            case Difficulty.Hard:
                WinRewardCredits = (int)(GameRules.WinRewardCredits * HardRewardMultiplier);
                LossRewardCredits = (int)(GameRules.LossRewardCredits * HardRewardMultiplier);
                KillRewardCredits = (int)(GameRules.KillRewardCredits * HardRewardMultiplier);
                OptimalBossInvestment = (int)(GameRules.OptimalBossInvestment * HardInvestmentEfficiency);
                break;
        }
    }

    /// <summary>バランス設定を文字列で出力。</summary>
    public string ToDebugString()
    {
        return $"[EconomyBalanceConfig]\n" +
               $"  難易度: {CurrentDifficulty}\n" +
               $"  開始クレジット: {StartingCredits}\n" +
               $"  勝利報酬: {WinRewardCredits}c\n" +
               $"  敗北報酬: {LossRewardCredits}c\n" +
               $"  キル報酬: {KillRewardCredits}c\n" +
               $"  目標報酬: {ObjectiveRewardCredits}c\n" +
               $"  ボス投資最適: {OptimalBossInvestment}c\n" +
               $"  投資返還倍率: {BossPayoutMultiplier}x\n" +
               $"  ULT最大: {MaxUltPoints}\n" +
               $"  初期BP: {InitialBuildPoints}\n" +
               $"  クレジットインフレ: {CreditInflationRate}\n" +
               $"  BPインフレ: {BuildPointInflationRate}\n" +
               $"  報酬倍率: {GetRewardMultiplier()}\n" +
               $"  投資効率: {GetInvestmentEfficiency()}";
    }
}
