using System;
using System.Collections.Generic;

namespace RYZECHo;

/// <summary>
/// 【非推奨】IEventBus への後方互換アダプタ。
/// 新規コードでは IEventBus インターフェースを使用してください。
/// </summary>
internal static class GameEventBus
{
    private static readonly GameEventBusAdapter _instance = new();

    internal static int SubscriberCount => _instance.SubscriberCount;

    internal static void Subscribe(Action<GameEvent> handler) => _instance.Subscribe(handler);

    internal static void Subscribe<T>(Action<T> handler) where T : GameEvent => _instance.Subscribe(handler);

    internal static void Unsubscribe(Action<GameEvent> handler) => _instance.Unsubscribe(handler);

    internal static void Unsubscribe<T>(Action<T> handler) where T : GameEvent => _instance.Unsubscribe(handler);

    internal static void Emit(GameEvent evt) => _instance.Emit(evt);

    internal static void ClearAllSubscribers() => _instance.ClearAllSubscribers();

    internal static int GetTypedSubscriberCount<T>() where T : GameEvent => _instance.GetTypedSubscriberCount<T>();
}

/// <summary>
/// GameEventBus の IEventBus 互換アダプタ。
/// GameEventBus (static) から IEventBus への移行を容易にする。
/// </summary>
/// <summary>
/// IEventBus のデフォルト実装。
/// GameEventBus (static) の singleton 依存を解消し、DI / テスト時に明示的なインスタンスを渡せる。
/// </summary>
internal sealed class GameEventBusAdapter : IEventBus
{
    /// <summary>GameEventBus の static singleton 参照（後方互換用）。</summary>
    internal static readonly GameEventBusAdapter Instance = new();

    // 全イベント購読者（型依存なしの万能サブスク）
    private readonly List<Action<GameEvent>> _subscribers = new();

    // 型指定購読者
    private readonly Dictionary<Type, List<Delegate>> _typedSubscribers = new();

    public int SubscriberCount => _subscribers.Count;

    public void Subscribe(Action<GameEvent> handler)
    {
        if (!_subscribers.Contains(handler))
        {
            _subscribers.Add(handler);
        }
    }

    public void Subscribe<T>(Action<T> handler) where T : GameEvent
    {
        var type = typeof(T);
        if (!_typedSubscribers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _typedSubscribers[type] = list;
        }

        if (!list.Contains(handler))
        {
            list.Add(handler);
        }
    }

    public void Unsubscribe(Action<GameEvent> handler)
    {
        _subscribers.Remove(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : GameEvent
    {
        var type = typeof(T);
        if (_typedSubscribers.TryGetValue(type, out var list))
        {
            list.Remove(handler);
            if (list.Count == 0)
            {
                _typedSubscribers.Remove(type);
            }
        }
    }

    public void Emit(GameEvent evt)
    {
        // タイムスタンプを記録
        evt.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 全サブスクライバーに配信
        for (int i = 0; i < _subscribers.Count; i++)
        {
            _subscribers[i]?.Invoke(evt);
        }

        // 型一致する購読者に配信
        if (_typedSubscribers.TryGetValue(evt.GetType(), out var typedList))
        {
            for (int i = 0; i < typedList.Count; i++)
            {
                typedList[i]?.DynamicInvoke(evt);
            }
        }
    }

    public void ClearAllSubscribers()
    {
        _subscribers.Clear();
        _typedSubscribers.Clear();
    }

    public int GetTypedSubscriberCount<T>() where T : GameEvent
    {
        var type = typeof(T);
        _typedSubscribers.TryGetValue(type, out var list);
        return list?.Count ?? 0;
    }
}
