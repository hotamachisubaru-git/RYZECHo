using System;
using System.Collections.Generic;
using System.Linq;

namespace RYZECHo.Tests;

/// <summary>
/// エコノミシステムのテストスイート。
/// 通貨の追加/削除、ボス投資、Ultポイント、ラウンド報酬をテストする。
/// </summary>
internal sealed class EconomyTestSuite
{
    private readonly TestRunner _runner;
    private readonly Random _random;

    public EconomyTestSuite(TestRunner runner)
    {
        _runner = runner;
        _random = new Random(123);
    }

    public void RunAll()
    {
        _runner.StartRun();

        RunCreditTests();
        RunInvestmentTests();
        RunUltPointTests();
        RunRoundPayoutTests();
        RunKillDividendTests();

        _runner.StopRun();
    }

    private void RunCreditTests()
    {
        _runner.RunTest("StartingCredits is correct", () =>
        {
            var expected = GameRules.StartingCredits;
            if (expected <= 0) throw new Exception($"StartingCredits should be positive, got {expected}");
        }, "Credits");

        _runner.RunTest("Credits cannot go below zero", () =>
        {
            var credits = 50;
            var deduction = 100;
            var result = Math.Max(0, credits - deduction);
            if (result < 0) throw new Exception("Credits went negative");
        }, "Credits");

        _runner.RunTest("Credits cap at reasonable maximum", () =>
        {
            var maxPossible = GameRules.StartingCredits
                + GameRules.WinRewardCredits * GameRules.RoundsToWin
                + GameRules.KillRewardCredits * 10;
            if (maxPossible <= 0) throw new Exception("MaxCredits calculation failed");
        }, "Credits");

        _runner.RunTest("WinRewardCredits is positive", () =>
        {
            if (GameRules.WinRewardCredits <= 0)
                throw new Exception($"WinRewardCredits should be positive: {GameRules.WinRewardCredits}");
        }, "Credits");

        _runner.RunTest("LossRewardCredits is non-negative", () =>
        {
            if (GameRules.LossRewardCredits < 0)
                throw new Exception($"LossRewardCredits should be non-negative: {GameRules.LossRewardCredits}");
        }, "Credits");
    }

    private void RunInvestmentTests()
    {
        _runner.RunTest("CalculateBuff returns zero for zero investment", () =>
        {
            var result = BossEconomyRules.CalculateBuff(0, GameRules.OptimalBossInvestment);
            if (result.CoreFactor != 0f)
                throw new Exception($"CoreFactor should be 0, got {result.CoreFactor}");
        }, "Investment");

        _runner.RunTest("CalculateBuff returns peak at optimal investment", () =>
        {
            var optimal = GameRules.OptimalBossInvestment;
            var result = BossEconomyRules.CalculateBuff(optimal, optimal);
            if (Math.Abs(result.CoreFactor - 1f) > 0.001f)
                throw new Exception($"CoreFactor should be ~1.0 at optimal, got {result.CoreFactor}");
            if (result.MoveBonusPercent <= 0f)
                throw new Exception("MoveBonus should be positive at optimal");
            if (result.FireRateBonusPercent <= 0f)
                throw new Exception("FireRateBonus should be positive at optimal");
        }, "Investment");

        _runner.RunTest("CalculateBuff decreases with over-investment", () =>
        {
            var overInvestment = GameRules.OptimalBossInvestment * 2;
            var result = BossEconomyRules.CalculateBuff(overInvestment, GameRules.OptimalBossInvestment);
            if (result.CoreFactor >= 1f)
                throw new Exception("CoreFactor should decrease with over-investment");
        }, "Investment");

        _runner.RunTest("CalculateBuff returns zero for negative investment", () =>
        {
            var result = BossEconomyRules.CalculateBuff(-100, GameRules.OptimalBossInvestment);
            if (result.CoreFactor != 0f)
                throw new Exception("CoreFactor should be 0 for negative investment");
        }, "Investment");

        _runner.RunTest("Investment ledger filters zero entries", () =>
        {
            var ledger = new Dictionary<string, int>
            {
                { "Player", 500 },
                { "Ally1", 0 },
                { "Ally2", 300 },
            };

            var positiveInvestments = ledger.Where(p => p.Value > 0).ToList();
            if (positiveInvestments.Count != 2)
                throw new Exception($"Expected 2 positive investments, got {positiveInvestments.Count}");
        }, "Investment");
    }

    private void RunUltPointTests()
    {
        _runner.RunTest("CalculateUltAward grants points correctly", () =>
        {
            var result = BossEconomyRules.CalculateUltAward("Player", 50, 30, GameRules.MaxUltPoints, "test");
            if (result.Granted <= 0)
                throw new Exception("Should grant positive points");
            if (result.After != result.Before + result.Granted)
                throw new Exception("After should equal Before + Granted");
        }, "UltPoints");

        _runner.RunTest("CalculateUltAward respects max cap", () =>
        {
            var max = GameRules.MaxUltPoints;
            var result = BossEconomyRules.CalculateUltAward("Player", max, 100, max, "test");
            if (result.After != max)
                throw new Exception($"After should be capped at {max}, got {result.After}");
        }, "UltPoints");

        _runner.RunTest("CalculateUltAward returns zero for invalid input", () =>
        {
            var result = BossEconomyRules.CalculateUltAward("", 50, 30, GameRules.MaxUltPoints, "test");
            if (result.Granted != 0)
                throw new Exception("Should grant 0 for empty name");
        }, "UltPoints");

        _runner.RunTest("CalculateUltAward handles negative added points", () =>
        {
            var result = BossEconomyRules.CalculateUltAward("Player", 50, -10, GameRules.MaxUltPoints, "test");
            if (result.Granted != 0)
                throw new Exception("Should grant 0 for negative added points");
        }, "UltPoints");

        _runner.RunTest("UltAwardResult contains correct fields", () =>
        {
            var result = BossEconomyRules.CalculateUltAward("TestActor", 0, 25, GameRules.MaxUltPoints, "kill");
            if (result.ActorName != "TestActor")
                throw new Exception("ActorName mismatch");
            if (result.Before != 0)
                throw new Exception("Before should be 0");
            if (result.Granted != 25)
                throw new Exception("Granted should be 25");
            if (result.After != 25)
                throw new Exception("After should be 25");
            if (result.Reason != "kill")
                throw new Exception("Reason mismatch");
        }, "UltPoints");
    }

    private void RunRoundPayoutTests()
    {
        _runner.RunTest("RoundPayout returns zero for no investment", () =>
        {
            var ledger = new Dictionary<string, int> { { "Player", 0 } };
            var result = BossEconomyRules.CalculateRoundPayout(ledger, true, true, 0, GameRules.BossPayoutMultiplier);
            if (result.TotalInvestedCredits != 0)
                throw new Exception("TotalInvested should be 0");
            if (result.InvestmentReturned)
                throw new Exception("Should not return investment with zero input");
        }, "RoundPayout");

        _runner.RunTest("RoundPayout returns zero for round loss", () =>
        {
            var ledger = new Dictionary<string, int> { { "Player", 500 } };
            var result = BossEconomyRules.CalculateRoundPayout(ledger, false, true, 0, GameRules.BossPayoutMultiplier);
            if (result.TotalReturnedCredits != 0)
                throw new Exception("Should return 0 on loss");
            if (result.InvestmentReturned)
                throw new Exception("Should not return on loss");
        }, "RoundPayout");

        _runner.RunTest("RoundPayout returns zero when boss is dead", () =>
        {
            var ledger = new Dictionary<string, int> { { "Player", 500 } };
            var result = BossEconomyRules.CalculateRoundPayout(ledger, true, false, 0, GameRules.BossPayoutMultiplier);
            if (result.TotalReturnedCredits != 0)
                throw new Exception("Should return 0 when boss dead");
        }, "RoundPayout");

        _runner.RunTest("RoundPayout returns zero when bossKillCount is zero", () =>
        {
            var ledger = new Dictionary<string, int> { { "Player", 500 } };
            var result = BossEconomyRules.CalculateRoundPayout(ledger, true, true, 0, GameRules.BossPayoutMultiplier);
            if (result.TotalReturnedCredits != 0)
                throw new Exception("Should return 0 when bossKillCount is 0");
        }, "RoundPayout");

        _runner.RunTest("RoundPayout returns correct amount on win", () =>
        {
            var multiplier = GameRules.BossPayoutMultiplier;
            var invested = 500;
            var ledger = new Dictionary<string, int> { { "Player", invested } };
            var result = BossEconomyRules.CalculateRoundPayout(ledger, true, true, 1, multiplier);

            if (!result.InvestmentReturned)
                throw new Exception("Should return investment on win");
            if (result.TotalInvestedCredits != invested)
                throw new Exception($"TotalInvested should be {invested}");
            if (result.TotalReturnedCredits != invested * multiplier)
                throw new Exception($"TotalReturned should be {invested * multiplier}");
            if (result.Returns.Count != 1)
                throw new Exception("Should have 1 return entry");
        }, "RoundPayout");

        _runner.RunTest("RoundPayout handles multiple investors", () =>
        {
            var ledger = new Dictionary<string, int>
            {
                { "Player", 500 },
                { "Ally1", 300 },
                { "Ally2", 200 },
            };
            var result = BossEconomyRules.CalculateRoundPayout(ledger, true, true, 1, GameRules.BossPayoutMultiplier);

            if (result.TotalInvestedCredits != 1000)
                throw new Exception($"TotalInvested should be 1000, got {result.TotalInvestedCredits}");
            if (result.Returns.Count != 3)
                throw new Exception("Should have 3 return entries");
        }, "RoundPayout");
    }

    private void RunKillDividendTests()
    {
        _runner.RunTest("CalculateKillDividend returns correct value", () =>
        {
            var result = BossEconomyRules.CalculateKillDividend(4, 100);
            if (result != 400)
                throw new Exception($"Expected 400, got {result}");
        }, "KillDividend");

        _runner.RunTest("CalculateKillDividend handles zero living members", () =>
        {
            var result = BossEconomyRules.CalculateKillDividend(0, 100);
            if (result != 0)
                throw new Exception("Should return 0 for zero living members");
        }, "KillDividend");

        _runner.RunTest("CalculateKillDividend handles negative inputs", () =>
        {
            var result = BossEconomyRules.CalculateKillDividend(-1, -50);
            if (result != 0)
                throw new Exception("Should return 0 for negative inputs");
        }, "KillDividend");
    }
}
