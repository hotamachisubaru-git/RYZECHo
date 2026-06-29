using System;
using System.Collections.Generic;

namespace RYZECHo;

/// <summary>
/// ゲーム内のイベントを配信する軽量イベントバス。
/// 型ごとにサブスクライバーを管理し、型安全なイベント配信を実現する。
/// </summary>
internal sealed class EventBus
{
    private readonly Dictionary<Type, object> _handlers = new();

    /// <summary>
    /// T タイプのイベントを配信する。
    /// </summary>
    public void Emit<T>(T eventArgs) where T : GameEvent
    {
        if (!_handlers.TryGetValue(typeof(T), out var raw))
            return;

        var handlers = (List<Action<T>>)raw;
        for (int i = 0; i < handlers.Count; i++)
        {
            handlers[i]?.Invoke(eventArgs);
        }
    }

    /// <summary>
    /// T タイプのイベントをサブスクライブする。
    /// </summary>
    public void Subscribe<T>(Action<T> handler) where T : GameEvent
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var raw))
        {
            raw = new List<Action<T>>();
            _handlers[type] = raw;
        }

        var handlers = (List<Action<T>>)raw;
        handlers.Add(handler);
    }

    /// <summary>
    /// T タイプのイベントのサブスクライブを解除する。
    /// </summary>
    public void Unsubscribe<T>(Action<T> handler) where T : GameEvent
    {
        if (!_handlers.TryGetValue(typeof(T), out var raw))
            return;

        var handlers = (List<Action<T>>)raw;
        handlers.Remove(handler);
    }

    /// <summary>
    /// T タイプのイベントにサブスクライブされているかチェックする。
    /// </summary>
    public bool HasSubscribers<T>() where T : GameEvent
    {
        if (!_handlers.TryGetValue(typeof(T), out var raw))
            return false;

        return ((List<Action<T>>)raw).Count > 0;
    }
}
