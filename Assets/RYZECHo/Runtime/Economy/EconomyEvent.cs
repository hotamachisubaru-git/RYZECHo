namespace RYZECHo;

/// <summary>
/// 経済イベントのタイプ。
/// </summary>
public enum EconomyEventType
{
    /// <summary>購入（通貨減少）</summary>
    Purchase,
    /// <summary>販売（通貨増加）</summary>
    Sale,
    /// <summary>報酬（通貨増加）</summary>
    Reward,
    /// <summary>ペナルティ（通貨減少）</summary>
    Penalty,
    /// <summary>投資（ボス投資）</summary>
    Investment,
    /// <summary>投資返還（ボス投資返還）</summary>
    InvestmentReturn,
    /// <summary>ULTポイント付与</summary>
    UltAward,
    /// <summary>スキル購入</summary>
    SkillPurchase,
}

/// <summary>
/// 経済イベントのデータ構造。
/// イベントタイプ、金額、発生元を保持する。
/// </summary>
public readonly record struct EconomyEvent
{
    /// <summary>イベントタイプ</summary>
    public EconomyEventType Type { get; }

    /// <summary>対象通貨ID（credits, buildPoints, agentCredits）</summary>
    public string CurrencyId { get; }

    /// <summary>金額（正: 増加、負: 減少）</summary>
    public int Amount { get; }

    /// <summary>イベント発生元（誰がトリガーしたか）</summary>
    public string Source { get; }

    /// <summary>詳細メッセージ</summary>
    public string Message { get; }

    /// <summary>ラウンド番号</summary>
    public int Round { get; }

    public EconomyEvent(EconomyEventType type, string currencyId, int amount, string source, string message = "", int round = 0)
    {
        Type = type;
        CurrencyId = currencyId;
        Amount = amount;
        Source = source;
        Message = message;
        Round = round;
    }

    /// <summary>購入イベントの作成。</summary>
    public static EconomyEvent Purchase(string currencyId, int amount, string source, string message = "")
        => new(EconomyEventType.Purchase, currencyId, -amount, source, message);

    /// <summary>報酬イベントの作成。</summary>
    public static EconomyEvent Reward(string currencyId, int amount, string source, string message = "")
        => new(EconomyEventType.Reward, currencyId, amount, source, message);

    /// <summary>ペナルティイベントの作成。</summary>
    public static EconomyEvent Penalty(string currencyId, int amount, string source, string message = "")
        => new(EconomyEventType.Penalty, currencyId, -amount, source, message);

    /// <summary>投資イベントの作成。</summary>
    public static EconomyEvent Investment(string source, int amount, string message = "")
        => new(EconomyEventType.Investment, CurrencyManager.CreditId, -amount, source, message);

    /// <summary>投資返還イベントの作成。</summary>
    public static EconomyEvent InvestmentReturn(string source, int amount, string message = "")
        => new(EconomyEventType.InvestmentReturn, CurrencyManager.CreditId, amount, source, message);
}
