using System;
using System.Collections.Generic;

namespace RYZECHo;

/// <summary>
/// 単一通貨の残高・制限・インフレ管理を行うクラス。
/// 各行列は独立して管理され、追加/削除/チェックが安全に実行される。
/// </summary>
public sealed class CurrencyEntry
{
    public string Id { get; }
    public int Balance { get; private set; }
    public int MaxValue { get; }
    public int MinValue { get; }
    public float InflationRate { get; set; } // 1.0 = 標準、>1.0 = インフレ、<1.0 = デフレ

    public CurrencyEntry(string id, int initialBalance, int maxValue, int minValue = 0)
    {
        Id = id;
        Balance = initialBalance;
        MaxValue = maxValue;
        MinValue = minValue;
        InflationRate = 1.0f;
    }

    /// <summary>残高を amount だけ増加。制限を適用。</summary>
    public int Add(int amount)
    {
        if (amount == 0) return Balance;
        var adjusted = (int)(amount * InflationRate);
        Balance = Math.Clamp(Balance + adjusted, MinValue, MaxValue);
        return adjusted;
    }

    /// <summary>残高を amount だけ減少。制限を適用。</summary>
    public bool Remove(int amount)
    {
        if (amount <= 0) return false;
        if (Balance < amount) return false;
        var adjusted = (int)(amount * InflationRate);
        Balance = Math.Clamp(Balance - adjusted, MinValue, MaxValue);
        return true;
    }

    /// <summary>残高を設定。制限を適用。</summary>
    public void Set(int value)
    {
        Balance = Math.Clamp(value, MinValue, MaxValue);
    }

    /// <summary>残高をチェック（十分か）。</summary>
    public bool HasEnough(int amount) => Balance >= amount;

    /// <summary>残高をゼロリセット。</summary>
    public void ResetTo(int value) => Balance = Math.Clamp(value, MinValue, MaxValue);

    /// <summary>残高を最大値にリセット。</summary>
    public void ResetToMax() => Balance = MaxValue;

    public override string ToString() => $"{Id}: {Balance}/{MaxValue}";
}
