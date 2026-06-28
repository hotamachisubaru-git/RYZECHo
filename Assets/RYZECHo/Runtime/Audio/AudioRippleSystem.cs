using System;
using UnityEngine;

namespace RYZECHo.Audio
{
    /// <summary>
    /// 音波エフェクトの最小限の実装（Unity対応）
    /// 既存のAudioRippleSystem（45行）を最適化
    /// </summary>
    public sealed class AudioRippleSystem : IDisposable
    {
        private readonly AudioEventManager _eventManager;
        private readonly Audio3DManager _audio3D;
        private readonly RippleProfile[] _profiles;
        private readonly float _minInterval;
        private readonly System.Collections.Generic.Dictionary<string, float> _lastPlayed;
        private bool _disposed;

        public AudioRippleSystem(AudioEventManager eventManager, Audio3DManager audio3D = null)
        {
            _eventManager = eventManager;
            _audio3D = audio3D;
            _profiles = new RippleProfile[]
            {
                new RippleProfile(RippleKind.Footstep, "SFX/Footstep/hard_step", 0.34f, 0.055f),
                new RippleProfile(RippleKind.Breathing, "SFX/Footstep/hard_step", 0.16f, 0.45f),
                new RippleProfile(RippleKind.Gunshot, "SFX/Weapon/rifle_fire", 0.72f, 0.035f),
                new RippleProfile(RippleKind.Skill, "SFX/UI/confirm_beep", 0.42f, 0.08f),
                new RippleProfile(RippleKind.Default, "SFX/UI/confirm_beep", 0.32f, 0.08f),
            };
            _minInterval = 0.02f;
            _lastPlayed = new System.Collections.Generic.Dictionary<string, float>();
        }

        /// <summary>音の波紋を再生</summary>
        public void Play(RippleKind kind, float volume, float pan, Vector3? position = null)
        {
            if (_disposed) return;

            var profile = kind switch
            {
                RippleKind.Footstep => _profiles[0],
                RippleKind.Breathing => _profiles[1],
                RippleKind.Gunshot => _profiles[2],
                RippleKind.Skill => _profiles[3],
                _ => _profiles[4],
            };

            var effectiveVolume = Mathf.Clamp(volume * profile.VolumeScale, 0.015f, 1f);
            if (effectiveVolume <= 0.015f) return;

            var key = $"{kind}_{Mathf.FloorToInt(pan * 100)}";
            var now = Time.time;
            if (_lastPlayed.TryGetValue(key, out var last) && now - last < profile.MinIntervalSeconds)
                return;
            _lastPlayed[key] = now;

            var clampedPan = Mathf.Clamp(pan, -1f, 1f);
            var clip = SoundEffectCatalog.GetClip(kind);

            if (position.HasValue && clip != null)
            {
                if (_audio3D != null)
                {
                    _audio3D.PlayAt(position.Value, clip, effectiveVolume, 1f);
                }
                else
                {
                    _eventManager.Trigger(kind.ToString(), clip, effectiveVolume, 1f, position);
                }
            }
            else if (clip != null)
            {
                _eventManager.Trigger(kind.ToString(), clip, effectiveVolume, clampedPan, position);
            }
        }

        /// <summary>全音の波紋をクリア</summary>
        public void ClearCooldowns() => _lastPlayed.Clear();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _lastPlayed.Clear();
        }

        private readonly record struct RippleProfile(
            RippleKind Kind,
            string Key,
            float VolumeScale,
            float MinIntervalSeconds);
    }

    /// <summary>音の波紋の種類</summary>
    public enum RippleKind
    {
        Footstep,
        Breathing,
        Gunshot,
        Skill,
        Default
    }

    /// <summary>音源カタログ用拡張</summary>
    public static class SoundEffectCatalogExtensions
    {
        public static AudioClip GetClip(RippleKind kind)
        {
            return kind switch
            {
                RippleKind.Footstep => null,
                RippleKind.Breathing => null,
                RippleKind.Gunshot => null,
                RippleKind.Skill => null,
                _ => null,
            };
        }
    }
}
