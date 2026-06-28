using System;

namespace RYZECHo;

internal interface IEventBus
{
    int SubscriberCount { get; }

    void Subscribe(Action<GameEvent> handler);

    void Subscribe<T>(Action<T> handler) where T : GameEvent;

    void Unsubscribe(Action<GameEvent> handler);

    void Unsubscribe<T>(Action<T> handler) where T : GameEvent;

    void Emit(GameEvent evt);

    void ClearAllSubscribers();

    int GetTypedSubscriberCount<T>() where T : GameEvent;
}
