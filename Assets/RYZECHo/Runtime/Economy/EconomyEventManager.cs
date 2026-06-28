using System;
using System.Collections.Generic;

namespace RYZECHo;

/// <summary>
/// 経済イベントの管理（登録/解除/トリガー/キュー）。
/// イベントキューを通じて非同期に処理される。
/// </summary>
public sealed class EconomyEventManager
{
    private readonly Queue<EconomyEvent> _eventQueue = new();
    private readonly List<Action<EconomyEvent>> _handlers = new();
    private readonly CurrencyManager _currencyManager;
    private readonly int _maxQueueSize;
    private int _totalProcessed;
    private int _totalDiscarded;

    public int QueueCount => _eventQueue.Count;
    public int TotalProcessed => _totalProcessed;
    public int TotalDiscarded => _totalDiscarded;
    public int MaxQueueSize => _maxQueueSize;

    public EconomyEventManager(CurrencyManager currencyManager, int maxQueueSize = 64)
    {
        _currencyManager = currencyManager;
        _maxQueueSize = maxQueueSize;
    }

    /// <summary>イベントハンドラを登録。</summary>
    public void RegisterHandler(Action<EconomyEvent> handler)
    {
        if (!_handlers.Contains(handler))
        {
            _handlers.Add(handler);
        }
    }

    /// <summary>イベントハンドラを解除。</summary>
    public void UnregisterHandler(Action<EconomyEvent> handler)
    {
        _handlers.Remove(handler);
    }

    /// <summary>イベントをキューに追加。</summary>
    public void QueueEvent(EconomyEvent e)
    {
        if (_eventQueue.Count >= _maxQueueSize)
        {
            _totalDiscarded++;
            _eventQueue.Dequeue(); // 古いものを破棄
        }
        _eventQueue.Enqueue(e);
    }

    /// <summary>キューの全イベントを処理。結果を返す。</summary>
    public List<EconomyEvent> ProcessAll()
    {
        var results = new List<EconomyEvent>();
        while (_eventQueue.Count > 0)
        {
            var e = _eventQueue.Dequeue();
            if (Process(e))
            {
                results.Add(e);
                _totalProcessed++;
            }
        }
        return results;
    }

    /// <summary>単一イベントを処理。</summary>
    public bool Process(EconomyEvent e)
    {
        switch (e.Type)
        {
            case EconomyEventType.Purchase:
                return _currencyManager.RemoveBalance(e.CurrencyId, -e.Amount);
            case EconomyEventType.Sale:
                _currencyManager.AddBalance(e.CurrencyId, e.Amount);
                return true;
            case EconomyEventType.Reward:
                _currencyManager.AddBalance(e.CurrencyId, e.Amount);
                return true;
            case EconomyEventType.Penalty:
                return _currencyManager.RemoveBalance(e.CurrencyId, -e.Amount);
            case EconomyEventType.Investment:
                return _currencyManager.RemoveBalance(e.CurrencyId, e.Amount);
            case EconomyEventType.InvestmentReturn:
                _currencyManager.AddBalance(e.CurrencyId, e.Amount);
                return true;
            case EconomyEventType.UltAward:
                // ULTは別管理（_ultPoints辞書）
                NotifyHandlers(e);
                return true;
            case EconomyEventType.SkillPurchase:
                return _currencyManager.RemoveBalance(e.CurrencyId, -e.Amount);
            default:
                return false;
        }
    }

    private void NotifyHandlers(EconomyEvent e)
    {
        foreach (var handler in _handlers)
        {
            handler?.Invoke(e);
        }
    }

    /// <summary>キューをクリア。</summary>
    public void ClearQueue() => _eventQueue.Clear();

    /// <summary>全カウンターをリセット。</summary>
    public void Reset()
    {
        ClearQueue();
        _totalProcessed = 0;
        _totalDiscarded = 0;
    }
}
