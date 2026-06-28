using System;
using System.Collections.Generic;

namespace RYZECHo;

/// <summary>
/// メインエコノミマネージャー。
/// AP/BP/クレジットの管理、通貨の追加/削除/チェック、永続化（セーブ/ロード）を行う。
/// </summary>
public sealed class EconomyManager
{
    private readonly CurrencyManager _currencyManager;
    private readonly EconomyEventManager _eventManager;
    private readonly ProgressProfile _profile;

    // 定数
    private const int CreditsMaxValue = 999999;
    private const int BuildPointsMaxValue = 999;
    private const int AgentCreditsMaxValue = 999999;

    // 現在の状態
    private int _currentBuildPoints;
    private int _currentCredits;
    private int _selectedBet;
    private readonly Dictionary<string, int> _ultPoints = new();
    private readonly Dictionary<string, int> _bossInvestments = new();

    // イベント
    public event Action<int>? OnCreditsChanged;
    public event Action<int>? OnBuildPointsChanged;
    public event Action<string, int, int>? OnCurrencyChanged; // (id, old, new)

    public EconomyManager(ProgressProfile profile)
    {
        _profile = profile;
        _currencyManager = new CurrencyManager();
        _eventManager = new EconomyEventManager(_currencyManager);

        // 通貨を登録
        _currencyManager.Register(CurrencyManager.CreditId, GameRules.StartingCredits, CreditsMaxValue);
        _currencyManager.Register(CurrencyManager.BuildPointId, GameRules.InitialBuildPoints, BuildPointsMaxValue);
        _currencyManager.Register(CurrencyManager.AgentCreditId, profile.AgentCredits, AgentCreditsMaxValue);

        _currentBuildPoints = GameRules.InitialBuildPoints;
        _currentCredits = GameRules.StartingCredits;
        _selectedBet = GameRules.OptimalBossInvestment;
    }

    /// <summary>クレジット残高。</summary>
    public int Credits => _currentCredits;

    /// <summary>ビルドポイント残高。</summary>
    public int BuildPoints => _currentBuildPoints;

    /// <summary>エージェントクレジット残高。</summary>
    public int AgentCredits => _currencyManager.GetBalance(CurrencyManager.AgentCreditId);

    /// <summary>選択中の投資額。</summary>
    public int SelectedBet => _selectedBet;

    /// <summary>ULTポイントを取得。</summary>
    public int GetUltPoints(string actorName)
    {
        _ultPoints.TryGetValue(actorName, out var points);
        return points;
    }

    /// <summary>ULTポイントを設定。</summary>
    public void SetUltPoints(string actorName, int points)
    {
        _ultPoints[actorName] = Math.Clamp(points, 0, GameRules.MaxUltPoints);
    }

    /// <summary>ULTポイントを追加。</summary>
    public int AddUltPoints(string actorName, int amount)
    {
        var before = _ultPoints.TryGetValue(actorName, out var current) ? current : 0;
        var after = Math.Clamp(before + amount, 0, GameRules.MaxUltPoints);
        _ultPoints[actorName] = after;
        return after - before;
    }

    /// <summary>ボス投資額を取得。</summary>
    public int GetBossInvestment(string actorName)
    {
        _bossInvestments.TryGetValue(actorName, out var amount);
        return amount;
    }

    /// <summary>ボス投資額を設定。</summary>
    public void SetBossInvestment(string actorName, int amount)
    {
        EnsureBossInvestmentKey(actorName);
        _bossInvestments[actorName] = Math.Max(0, amount);
        SyncSelectedBetTotal();
    }

    /// <summary>ボス投資額を増加。</summary>
    public int AddBossInvestment(string actorName, int delta)
    {
        EnsureBossInvestmentKey(actorName);
        var current = _bossInvestments[actorName];
        var otherInvestments = TotalSelectedInvestment() - current;
        var maxInvestment = Math.Max(0, _currentCredits - GetWeaponCost() - GetSidearmWeaponCost() - otherInvestments);
        var next = Math.Clamp(current + delta, 0, maxInvestment);
        _bossInvestments[actorName] = next;
        SyncSelectedBetTotal();
        return next;
    }

    /// <summary>総投資額。</summary>
    public int TotalSelectedInvestment()
    {
        var total = 0;
        foreach (var v in _bossInvestments.Values)
        {
            total += v;
        }
        return total;
    }

    /// <summary>クレジットを追加。</summary>
    public int AddCredits(int amount)
    {
        var old = _currentCredits;
        _currentCredits = Math.Min(_currentCredits + amount, CreditsMaxValue);
        OnCreditsChanged?.Invoke(_currentCredits);
        OnCurrencyChanged?.Invoke(CurrencyManager.CreditId, old, _currentCredits);
        return _currentCredits - old;
    }

    /// <summary>クレジットを削除。成功すればtrue。</summary>
    public bool RemoveCredits(int amount)
    {
        if (_currentCredits < amount) return false;
        var old = _currentCredits;
        _currentCredits -= amount;
        OnCreditsChanged?.Invoke(_currentCredits);
        OnCurrencyChanged?.Invoke(CurrencyManager.CreditId, old, _currentCredits);
        return true;
    }

    /// <summary>ビルドポイントを追加。</summary>
    public int AddBuildPoints(int amount)
    {
        var old = _currentBuildPoints;
        _currentBuildPoints = Math.Min(_currentBuildPoints + amount, BuildPointsMaxValue);
        OnBuildPointsChanged?.Invoke(_currentBuildPoints);
        OnCurrencyChanged?.Invoke(CurrencyManager.BuildPointId, old, _currentBuildPoints);
        return _currentBuildPoints - old;
    }

    /// <summary>ビルドポイントを削除。成功すればtrue。</summary>
    public bool RemoveBuildPoints(int amount)
    {
        if (_currentBuildPoints < amount) return false;
        var old = _currentBuildPoints;
        _currentBuildPoints -= amount;
        OnBuildPointsChanged?.Invoke(_currentBuildPoints);
        OnCurrencyChanged?.Invoke(CurrencyManager.BuildPointId, old, _currentBuildPoints);
        return true;
    }

    /// <summary>クレジットが十分か。</summary>
    public bool HasEnoughCredits(int amount) => _currentCredits >= amount;

    /// <summary>ビルドポイントが十分か。</summary>
    public bool HasEnoughBuildPoints(int amount) => _currentBuildPoints >= amount;

    /// <summary>投資を行う（ラウンド開始時）。</summary>
    public bool Invest(int amount, string actorName)
    {
        if (!RemoveCredits(amount)) return false;
        SetBossInvestment(actorName, GetBossInvestment(actorName) + amount);
        _selectedBet = TotalSelectedInvestment();
        _eventManager.QueueEvent(EconomyEvent.Investment(actorName, amount, $"投資: {actorName} +{amount}c"));
        return true;
    }

    /// <summary>投資返還を行う。</summary>
    public void ReturnInvestment(string actorName, int amount)
    {
        AddCredits(amount);
        _eventManager.QueueEvent(EconomyEvent.InvestmentReturn(actorName, amount, $"投資返還: {actorName} +{amount}c"));
    }

    /// <summary>報酬を与える。</summary>
    public void AwardReward(string currencyId, int amount, string source, string message = "")
    {
        AddCredits(amount);
        _eventManager.QueueEvent(EconomyEvent.Reward(currencyId, amount, source, message));
    }

    /// <summary>ペナルティを適用。</summary>
    public bool ApplyPenalty(string currencyId, int amount, string source, string message = "")
    {
        var success = _currencyManager.RemoveBalance(currencyId, amount);
        if (success)
        {
            _eventManager.QueueEvent(EconomyEvent.Penalty(currencyId, amount, source, message));
        }
        return success;
    }

    /// <summary>ULT報酬を計算・適用。</summary>
    public void AwardUltPoints(string actorName, int amount, string reason)
    {
        var before = _ultPoints.TryGetValue(actorName, out var current) ? current : 0;
        var after = Math.Clamp(before + amount, 0, GameRules.MaxUltPoints);
        _ultPoints[actorName] = after;
        _eventManager.QueueEvent(new EconomyEvent(
            EconomyEventType.UltAward, CurrencyManager.CreditId, 0, actorName,
            $"{actorName} ULT {before}->{after} ({reason})"));
    }

    /// <summary>スキル購入コストを取得。</summary>
    public int GetSkillPurchaseCost() => 400;

    /// <summary>スキルを購入。</summary>
    public bool PurchaseSkill()
    {
        var cost = GetSkillPurchaseCost();
        if (_currentCredits < cost) return false;
        _currentCredits -= cost;
        OnCreditsChanged?.Invoke(_currentCredits);
        _eventManager.QueueEvent(EconomyEvent.SkillPurchase(CurrencyManager.CreditId, cost, "スキル購入"));
        return true;
    }

    /// <summary>ラウンド開始時の経済状態を初期化。</summary>
    public void StartRound(int startingCredits, int initialBuildPoints)
    {
        _currentCredits = startingCredits;
        _currentBuildPoints = initialBuildPoints;
        _selectedBet = GameRules.OptimalBossInvestment;
        OnCreditsChanged?.Invoke(_currentCredits);
        OnBuildPointsChanged?.Invoke(_currentBuildPoints);
        _eventManager.ClearQueue();
    }

    /// <summary>マッチリセット時の経済状態を初期化。</summary>
    public void ResetCampaign(int startingCredits, int initialBuildPoints)
    {
        _currentCredits = startingCredits;
        _currentBuildPoints = initialBuildPoints;
        _selectedBet = GameRules.OptimalBossInvestment;
        _ultPoints.Clear();
        _bossInvestments.Clear();
        OnCreditsChanged?.Invoke(_currentCredits);
        OnBuildPointsChanged?.Invoke(_currentBuildPoints);
        _eventManager.ClearQueue();
    }

    // ---- セーブ/ロード ----

    /// <summary>セーブデータを生成。</summary>
    public EconomySaveData CreateSaveData()
    {
        return new EconomySaveData
        {
            credits = _currentCredits,
            buildPoints = _currentBuildPoints,
            selectedBet = _selectedBet,
            agentCredits = _currencyManager.GetBalance(CurrencyManager.AgentCreditId),
            ultPoints = new Dictionary<string, int>(_ultPoints),
            bossInvestments = new Dictionary<string, int>(_bossInvestments),
        };
    }

    /// <summary>セーブデータからロード。</summary>
    public void LoadSaveData(EconomySaveData data)
    {
        _currentCredits = data.credits;
        _currentBuildPoints = data.buildPoints;
        _selectedBet = data.selectedBet;
        _currencyManager.SetBalance(CurrencyManager.AgentCreditId, data.agentCredits);
        _ultPoints.Clear();
        foreach (var kvp in data.ultPoints)
        {
            _ultPoints[kvp.Key] = kvp.Value;
        }
        _bossInvestments.Clear();
        foreach (var kvp in data.bossInvestments)
        {
            _bossInvestments[kvp.Key] = kvp.Value;
        }
        OnCreditsChanged?.Invoke(_currentCredits);
        OnBuildPointsChanged?.Invoke(_currentBuildPoints);
    }

    // ---- ヘルパー ----

    private void EnsureBossInvestmentKey(string actorName)
    {
        if (!_bossInvestments.ContainsKey(actorName))
        {
            _bossInvestments[actorName] = actorName == "Player" ? GameRules.OptimalBossInvestment : 0;
        }
    }

    private void SyncSelectedBetTotal()
    {
        _selectedBet = TotalSelectedInvestment();
    }

    private int GetWeaponCost() => 0; // GameModelから取得
    private int GetSidearmWeaponCost() => 0; // GameModelから取得

    /// <summary>イベントマネージャーを取得。</summary>
    public EconomyEventManager GetEventManager() => _eventManager;

    /// <summary>通貨マネージャーを取得。</summary>
    public CurrencyManager GetCurrencyManager() => _currencyManager;
}

/// <summary>
/// エコノミのセーブデータ。
/// </summary>
public record EconomySaveData
{
    public int credits;
    public int buildPoints;
    public int selectedBet;
    public int agentCredits;
    public Dictionary<string, int> ultPoints;
    public Dictionary<string, int> bossInvestments;
}
