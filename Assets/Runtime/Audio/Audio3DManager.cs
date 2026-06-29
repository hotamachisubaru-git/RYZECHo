using System;
using System.Collections.Generic;
using UnityEngine;

namespace RYZECHo.Audio
{
    /// <summary>
    /// 3Dオーディオ管理（距離衰减、角度衰减、空間化）
    /// </summary>
    public sealed class Audio3DManager : IDisposable
    {
        public enum AttenuationModel
        {
            Linear,
            Exponential,
            Inverse
        }

        public enum SpatializationType
        {
            None,
            HRTF,
            SimpleStereo
        }

        private readonly Dictionary<Guid, Audio3DSource> _sources = new();
        private readonly Dictionary<AudioSource, Guid> _reverseMap = new();
        private AttenuationModel _defaultAttenuation = AttenuationModel.Exponential;
        private float _defaultMinDistance = 1f;
        private float _defaultMaxDistance = 50f;
        private SpatializationType _defaultSpatialization = SpatializationType.SimpleStereo;
        private float _angleThreshold = 30f;
        private float _angleAttenuation = 0.5f;
        private bool _disposed;

        public AttenuationModel DefaultAttenuation
        {
            get => _defaultAttenuation;
            set => _defaultAttenuation = value;
        }

        public float DefaultMinDistance
        {
            get => _defaultMinDistance;
            set => _defaultMinDistance = Mathf.Max(0f, value);
        }

        public float DefaultMaxDistance
        {
            get => _defaultMaxDistance;
            set => _defaultMaxDistance = Mathf.Max(0.001f, value);
        }

        public float AngleThreshold
        {
            get => _angleThreshold;
            set => _angleThreshold = Mathf.Max(0f, value);
        }

        public float AngleAttenuation
        {
            get => _angleAttenuation;
            set => _angleAttenuation = Mathf.Clamp01(value);
        }

        /// <summary>指定位置から3Dオーディオを再生</summary>
        public Guid PlayAt(Vector3 position, AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return Guid.Empty;

            var go = new GameObject($"Audio3D_{position:x8}") { hideFlags = HideFlags.HideInHierarchy };
            go.transform.position = position;

            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 1f;
            source.minDistance = _defaultMinDistance;
            source.maxDistance = _defaultMaxDistance;
            source.rolloffMode = AudioRolloffMode.Custom;

            ApplySpatialization(source);

            source.Play();
            var id = Guid.NewGuid();
            _sources[id] = new Audio3DSource(source, position);
            _reverseMap[source] = id;
            return id;
        }

        /// <summary>既存の3Dソースを更新</summary>
        public void UpdateSource(Guid id, Vector3 newPosition)
        {
            if (!_sources.TryGetValue(id, out var src)) return;
            src.Position = newPosition;
            src.GameObject.transform.position = newPosition;
        }

        /// <summary>3Dソースを停止して破棄</summary>
        public void Stop(Guid id)
        {
            if (!_sources.TryGetValue(id, out var src)) return;
            src.GameObject.SetActive(false);
            src.Dispose();
            _sources.Remove(id);
            _reverseMap.Remove(src.AudioSource);
        }

        /// <summary>全3Dオーディオを停止</summary>
        public void StopAll()
        {
            foreach (var src in _sources.Values)
            {
                src.GameObject.SetActive(false);
                src.Dispose();
            }
            _sources.Clear();
            _reverseMap.Clear();
        }

        /// <summary>3Dオーディオの再生中数を取得</summary>
        public int ActiveSourceCount => _sources.Count;

        /// <summary>距離衰减係数を計算</summary>
        public float CalculateDistanceAttenuation(float distance)
        {
            distance = Mathf.Max(0f, distance);
            return _defaultAttenuation switch
            {
                AttenuationModel.Linear => Mathf.Clamp01(1f - distance / _defaultMaxDistance),
                AttenuationModel.Exponential => Mathf.Clamp01(Mathf.Pow(distance / _defaultMinDistance, -1f)),
                AttenuationModel.Inverse => Mathf.Clamp01(1f / (1f + distance)),
                _ => 1f,
            };
        }

        /// <summary>角度衰减係数を計算</summary>
        public float CalculateAngleAttenuation(Vector3 listenerPos, Vector3 sourcePos, Vector3 listenerForward)
        {
            var toSource = (sourcePos - listenerPos).normalized;
            var angle = Vector3.Angle(listenerForward, toSource);
            if (angle <= _angleThreshold) return 1f;
            var ratio = (angle - _angleThreshold) / (180f - _angleThreshold);
            return Mathf.Lerp(1f, _angleAttenuation, ratio);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopAll();
        }

        private void ApplySpatialization(AudioSource source)
        {
            switch (_defaultSpatialization)
            {
                case SpatializationType.HRTF:
                    source.spatialize = true;
                    source.spatializePostEffects = true;
                    break;
                case SpatializationType.SimpleStereo:
                    source.spatialBlend = 1f;
                    break;
                default:
                    source.spatialBlend = 0f;
                    break;
            }
        }

        private sealed class Audio3DSource : IDisposable
        {
            public GameObject GameObject { get; }
            public AudioSource AudioSource { get; }
            public Vector3 Position { get; set; }
            private bool _disposed;

            public Audio3DSource(AudioSource source, Vector3 position)
            {
                GameObject = source.gameObject;
                AudioSource = source;
                Position = position;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                try { AudioSource.Stop(); AudioSource.clip = null; } catch { }
                if (GameObject != null) GameObject.SetActive(false);
            }
        }
    }
}
