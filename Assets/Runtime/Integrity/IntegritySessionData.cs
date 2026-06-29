using System;
using UnityEngine;

namespace RYZECHo.Integrity
{
    /// <summary>
    /// セッションの状態を表すEnum
    /// </summary>
    public enum IntegritySessionState
    {
        Running,
        Paused,
        Completed,
        Failed,
        Corrupted,
    }

    /// <summary>
    /// セッションデータ — セッションID、開始/終了時刻、状態を保持
    /// </summary>
    [Serializable]
    public class IntegritySessionData
    {
        /// <summary>セッションID</summary>
        public string SessionId { get; private set; }

        /// <summary>セッションの開始時刻 (UTC)</summary>
        public DateTime StartTime { get; private set; }

        /// <summary>セッションの終了時刻 (UTC)</summary>
        public DateTime? EndTime { get; private set; }

        /// <summary>セッションの状態</summary>
        public IntegritySessionState State { get; private set; }

        /// <summary>セッションの継続時間</summary>
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;

        /// <summary>セッションのバージョン</summary>
        public string Version { get; set; }

        /// <summary>セッショントークン（改ざん検知用）</summary>
        public string IntegrityToken { get; set; }

        public IntegritySessionData()
        {
            SessionId = Guid.NewGuid().ToString("N");
            StartTime = DateTime.UtcNow;
            EndTime = null;
            State = IntegritySessionState.Running;
            Version = "1.0.0";
            IntegrityToken = string.Empty;
        }

        public IntegritySessionData(string sessionId)
        {
            SessionId = sessionId ?? Guid.NewGuid().ToString("N");
            StartTime = DateTime.UtcNow;
            EndTime = null;
            State = IntegritySessionState.Running;
            Version = "1.0.0";
            IntegrityToken = string.Empty;
        }

        /// <summary>セッションを終了状態に設定</summary>
        public void Complete()
        {
            State = IntegritySessionState.Completed;
            EndTime = DateTime.UtcNow;
        }

        /// <summary>セッションを失敗状態に設定</summary>
        public void Fail()
        {
            State = IntegritySessionState.Failed;
            EndTime = DateTime.UtcNow;
        }

        /// <summary>セッションを破損状態に設定</summary>
        public void Corrupt()
        {
            State = IntegritySessionState.Corrupted;
            EndTime = DateTime.UtcNow;
        }

        /// <summary>セッションを一時停止</summary>
        public void Pause()
        {
            State = IntegritySessionState.Paused;
        }

        /// <summary>セッションを再開</summary>
        public void Resume()
        {
            if (State == IntegritySessionState.Paused)
            {
                State = IntegritySessionState.Running;
            }
        }

        /// <summary>セッションの有効性をチェック</summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SessionId)
                && State != IntegritySessionState.Corrupted
                && StartTime <= DateTime.UtcNow;
        }
    }
}
