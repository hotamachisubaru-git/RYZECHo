namespace RYZECHo;

internal readonly record struct BossBuffProfile(
    float CoreFactor,
    float MoveBonusPercent,
    float FireRateBonusPercent);

internal readonly record struct BossInvestmentReturn(
    string InvestorName,
    int InvestedCredits,
    int ReturnedCredits);

internal readonly record struct BossRoundPayout(
    int TotalInvestedCredits,
    int TotalReturnedCredits,
    bool InvestmentReturned,
    string Reason,
    IReadOnlyList<BossInvestmentReturn> Returns);

internal readonly record struct UltAwardResult(
    string ActorName,
    int Before,
    int After,
    int Granted,
    string Reason);

internal static class BossEconomyRules
{
    private const float PeakMoveBonusPercent = 0.12f;
    private const float PeakFireRateBonusPercent = 0.18f;

    public static BossBuffProfile CalculateBuff(int investmentCredits, int optimalInvestmentCredits)
    {
        var coreFactor = CalculateNegativeQuadraticCore(investmentCredits, optimalInvestmentCredits);
        return new BossBuffProfile(
            coreFactor,
            PeakMoveBonusPercent * coreFactor,
            PeakFireRateBonusPercent * coreFactor);
    }

    public static BossRoundPayout CalculateRoundPayout(
        IReadOnlyDictionary<string, int> investmentLedger,
        bool roundWon,
        bool bossAlive,
        int bossKillCount,
        int payoutMultiplier)
    {
        var investments = investmentLedger
            .Where(pair => pair.Value > 0)
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new BossInvestmentReturn(pair.Key, pair.Value, 0))
            .ToArray();

        var invested = investments.Sum(entry => entry.InvestedCredits);
        if (invested <= 0)
        {
            return new BossRoundPayout(0, 0, false, "投資なし", investments);
        }

        if (!roundWon)
        {
            return new BossRoundPayout(invested, 0, false, "敗北により投資返還なし", investments);
        }

        if (!bossAlive)
        {
            return new BossRoundPayout(invested, 0, false, "ボス撃破により投資没収", investments);
        }

        if (bossKillCount <= 0)
        {
            return new BossRoundPayout(invested, 0, false, "ボス無撃破のため投資返還なし", investments);
        }

        var returns = investments
            .Select(entry => entry with
            {
                ReturnedCredits = entry.InvestedCredits * Math.Max(0, payoutMultiplier),
            })
            .ToArray();
        var returned = returns.Sum(entry => entry.ReturnedCredits);
        return new BossRoundPayout(invested, returned, true, "ボス生存勝利により投資返還", returns);
    }

    public static int CalculateKillDividend(int livingFriendlyCount, int dividendPerLivingMember)
    {
        return Math.Max(0, livingFriendlyCount) * Math.Max(0, dividendPerLivingMember);
    }

    public static UltAwardResult CalculateUltAward(
        string actorName,
        int currentPoints,
        int addedPoints,
        int maxPoints,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(actorName) || addedPoints <= 0 || maxPoints <= 0)
        {
            return new UltAwardResult(actorName, currentPoints, currentPoints, 0, reason);
        }

        var before = Math.Clamp(currentPoints, 0, maxPoints);
        var after = Math.Clamp(before + addedPoints, 0, maxPoints);
        return new UltAwardResult(actorName, before, after, after - before, reason);
    }

    private static float CalculateNegativeQuadraticCore(int investmentCredits, int optimalInvestmentCredits)
    {
        if (investmentCredits <= 0 || optimalInvestmentCredits <= 0)
        {
            return 0f;
        }

        var offsetFromPeak = (investmentCredits - optimalInvestmentCredits) / (float)optimalInvestmentCredits;
        return Math.Clamp(1f - (offsetFromPeak * offsetFromPeak), 0f, 1f);
    }
}
