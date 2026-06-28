using System;
using System.Collections.Generic;

namespace RYZECHo;

/// <summary>
/// 全通貨の残高管理・インフレ/デフレーション管理を行うマネージャー。
/// AP, BP, クレジットの各通貨を一元管理する。
/// </summary>
public sealed class CurrencyManager
{
    private readonly Dictionary<string, CurrencyEntry> _currencies = new();

    // 通貨ID
    public const string CreditId = "credits";
    public const string BuildPointId = "buildPoints";
    public const string AgentCreditId = "agentCredits";

    /// <summary>全通貨の登録状況。</summary>
    public int Count => _currencies.Count;

    /// <summary>登録済みかどうか。</summary>
    public bool HasCurrency(string id) => _currencies.ContainsKey(id);

    /// <summary>通貨を登録。</summary>
    public void Register(string id, int initialBalance, int maxValue, int minValue = 0)
    {
        if (_currencies.ContainsKey(id))
        {
            return;
        }
        _currencies[id] = new CurrencyEntry(id, initialBalance, maxValue, minValue);
    }

    /// <summary>通貨を登録（既存を上書き）。</summary>
    public void RegisterOrOverwrite(string id, int initialBalance, int maxValue, int minValue = 0)
    {
        _currencies[id] = new CurrencyEntry(id, initialBalance, maxValue, minValue);
    }

    /// <summary>通貨の残高を取得。</summary>
    public int GetBalance(string id)
    {
        _currencies.TryGetValue(id, out var entry);
        return entry?.Balance ?? 0;
    }

    /// <summary>通貨の残高を設定。</summary>
    public void SetBalance(string id, int value)
    {
        if (_currencies.TryGetValue(id, out var entry))
        {
            entry.Set(value);
        }
    }

    /// <summary>通貨の残高を増加。</summary>
    public int AddBalance(string id, int amount)
    {
        if (!_currencies.TryGetValue(id, out var entry)) return 0;
        return entry.Add(amount);
    }

    /// <summary>通貨の残高を減少。成功すればtrue。</summary>
    public bool RemoveBalance(string id, int amount)
    {
        if (!_currencies.TryGetValue(id, out var entry)) return false;
        return entry.Remove(amount);
    }

    /// <summary>通貨の残高が十分か。</summary>
    public bool HasEnough(string id, int amount)
    {
        if (!_currencies.TryGetValue(id, out var entry)) return false;
        return entry.HasEnough(amount);
    }

    /// <summary>最大値を取得。</summary>
    public int GetMaxValue(string id)
    {
        _currencies.TryGetValue(id, out var entry);
        return entry?.MaxValue ?? 0;
    }

    /// <summary>最小値を取得。</summary>
    public int GetMinValue(string id)
    {
        _currencies.TryGetValue(id, out var entry);
        return entry?.MinValue ?? 0;
    }

    /// <summary>インフレ率を設定。</summary>
    public void SetInflationRate(string id, float rate)
    {
        if (_currencies.TryGetValue(id, out var entry))
        {
            entry.InflationRate = Math.Max(0f, rate);
        }
    }

    /// <summary>インフレ率を取得。</summary>
    public float GetInflationRate(string id)
    {
        _currencies.TryGetValue(id, out var entry);
        return entry?.InflationRate ?? 1.0f;
    }

    /// <summary>全通貨を初期値にリセット。</summary>
    public void ResetAll(int startingCredits, int initialBuildPoints)
    {
        if (_currencies.TryGetValue(CreditId, out var creditEntry))
        {
            creditEntry.ResetTo(startingCredits);
        }
        if (_currencies.TryGetValue(BuildPointId, out var bpEntry))
        {
            bpEntry.ResetTo(initialBuildPoints);
        }
    }

    /// <summary>全通貨を最大値にリセット。</summary>
    public void ResetAllToMax()
    {
        foreach (var entry in _currencies.Values)
        {
            entry.ResetToMax();
        }
    }

    /// <summary>通貨を削除。</summary>
    public void Unregister(string id)
    {
        _currencies.Remove(id);
    }

    /// <summary>全通貨のリストを取得。</summary>
    public IReadOnlyDictionary<string, CurrencyEntry> GetAllCurrencies() => _currencies;

    /// <summary>通貨の状態を文字列で取得。</summary>
    public string GetStatusString()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in _currencies)
        {
            sb.AppendLine($"  {kvp.Value}");
        }
        return sb.ToString();
    }
}
