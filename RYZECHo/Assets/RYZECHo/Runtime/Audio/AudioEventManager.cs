using System;
using System.Collections.Generic;
using UnityEngine;

namespace RYZECHo.Audio
{
    /// <summary>
    /// イベントベースのオーディオ再生マネージャー
    /// イベント登録/解除、オーディオキュー管理
    /// </summary>
    public sealed class AudioEventManager : IDisposable
    {
        private readonly Dictionary<string, List<AudioEventEntry>> _eventHandlers = new();
        private readonly Queue<AudioPendingEvent> _eventQueue = new();
        private readonly int _maxQueueSize;
        private bool _paused;
        private bool _disposed;

        public int PendingEventCount => _eventQueue.Count;
        public bool IsPaused { get => _paused; set => _paused = value; }

        public AudioEventManager(int maxQueueSize = 64)
        {
            _maxQueueSize = maxQueueSize;
        }

        /// <summary>オーディオイベントを登録</summary>
        public void Register(string eventName, Action<AudioPlaybackRequest> handler)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            if (!_eventHandlers.TryGetValue(eventName, out var list))
            {
                list = new List<AudioEventEntry>();
                _eventHandlers[eventName] = list;
            }
            list.Add(new AudioEventEntry(handler));
        }

        /// <summary>オーディオイベントを解除</summary>
        public void Unregister(string eventName, Action<AudioPlaybackRequest> handler)
        {
            if (!_eventHandlers.TryGetValue(eventName, out var list)) return;
            list.RemoveAll(entry => entry.Handler == handler);
            if (list.Count == 0) _eventHandlers.Remove(eventName);
        }

        /// <summary>オーディオイベントをキューに追加</summary>
        public void Enqueue(string eventName, AudioClip clip, float volume = 1f, float pitch = 1f, Vector3? position = null)
        {
            if (string.IsNullOrEmpty(eventName) || clip == null) return;

            var evt = new AudioPendingEvent(eventName, clip, volume, pitch, position);
            if (_eventQueue.Count >= _maxQueueSize)
            {
                _eventQueue.Dequeue(); // 古いイベントから削除
            }
            _eventQueue.Enqueue(evt);
        }

        /// <summary>キュー内のイベントを再生（フレーム更新で呼び出し）</summary>
        public void ProcessQueue()
        {
            if (_paused || _eventQueue.Count == 0) return;

            var count = _eventQueue.Count;
            for (int i = 0; i < count; i++)
            {
                if (_eventQueue.Count == 0) break;
                var evt = _eventQueue.Dequeue();
                ProcessEvent(evt.EventName, evt.Clip, evt.Volume, evt.Pitch, evt.Position);
            }
        }

        /// <summary>イベントを即時再生</summary>
        public void Trigger(string eventName, AudioClip clip, float volume = 1f, float pitch = 1f, Vector3? position = null)
        {
            ProcessEvent(eventName, clip, volume, pitch, position);
        }

        /// <summary>イベント名に紐づく全ハンドラを解除</summary>
        public void ClearEvents(string eventName)
        {
            _eventHandlers.Remove(eventName);
        }

        /// <summary>全イベントを解除</summary>
        public void ClearAll()
        {
            _eventHandlers.Clear();
            _eventQueue.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _eventHandlers.Clear();
            _eventQueue.Clear();
        }

        private void ProcessEvent(string eventName, AudioClip clip, float volume, float pitch, Vector3? position)
        {
            if (!_eventHandlers.TryGetValue(eventName, out var handlers)) return;

            var request = new AudioPlaybackRequest(clip, volume, pitch, position);
            foreach (var handler in handlers)
            {
                try { handler.Handler(request); }
                catch (Exception e) { Debug.LogWarning($"[AudioEventManager] Event handler error: {e.Message}"); }
            }
        }

        private sealed class AudioEventEntry
        {
            public Action<AudioPlaybackRequest> Handler { get; }
            public AudioEventEntry(Action<AudioPlaybackRequest> handler) => Handler = handler;
        }

        private readonly record struct AudioPendingEvent(
            string EventName,
            AudioClip Clip,
            float Volume,
            float Pitch,
            Vector3? Position);
    }

    /// <summary>オーディオ再生リクエスト</summary>
    public readonly record struct AudioPlaybackRequest(
        AudioClip Clip,
        float Volume,
        float Pitch,
        Vector3? Position);
}
