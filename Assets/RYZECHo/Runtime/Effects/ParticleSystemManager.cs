using System.Collections.Generic;
using UnityEngine;

namespace RYZECHo.Runtime.Effects
{
    /// <summary>
    /// Unity ParticleSystemのライフサイクル管理マネージャー。
    /// エフェクトの生成・破棄・プールを一元管理する。
    /// </summary>
    public class ParticleSystemManager : MonoBehaviour
    {
        [System.Serializable]
        public struct EffectPrefab
        {
            public string name;
            public GameObject prefab;
            public int poolSize;
        }

        [Header("Effect Prefabs")]
        [SerializeField] private List<EffectPrefab> effectPrefabs = new();

        [Header("Global Settings")]
        [SerializeField] private Transform effectParent;
        [SerializeField] private int maxActiveEffects = 100;

        private Dictionary<string, EffectPool> _pools = new();
        private List<ParticleSystem> _activeSystems = new();

        public static ParticleSystemManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var ep in effectPrefabs)
            {
                if (ep.prefab == null) continue;
                _pools[ep.name] = new EffectPool(ep.prefab, ep.poolSize, 50, effectParent);
            }
        }

        /// <summary>
        /// 名前付きエフェクトを再生
        /// </summary>
        public void PlayEffect(string effectName, Vector3 position, Quaternion rotation = default)
        {
            if (!_pools.TryGetValue(effectName, out var pool))
            {
                Debug.LogWarning($"[ParticleSystemManager] Effect '{effectName}' not found.");
                return;
            }

            var ps = pool.Get(position, rotation);
            if (ps != null)
            {
                ps.Play();
                _activeSystems.Add(ps);
            }
        }

        /// <summary>
        /// カスタムプレハブでエフェクトを再生
        /// </summary>
        public void PlayCustomEffect(GameObject prefab, Vector3 position, Quaternion rotation = default)
        {
            if (prefab == null) return;

            var go = GameObject.Instantiate(prefab, position, rotation);
            go.transform.SetParent(effectParent);

            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                _activeSystems.Add(ps);
            }

            // 全ParticleSystemsが止まったら破棄
            Destroy(go, 10f);
        }

        /// <summary>
        /// エフェクトを停止してプールに戻す
        /// </summary>
        public void StopEffect(ParticleSystem ps)
        {
            if (ps == null) return;

            ps.Stop();
            ps.Clear();

            // プールに戻す
            foreach (var kvp in _pools)
            {
                var pool = kvp.Value;
                if (pool.Return(ps))
                {
                    _activeSystems.Remove(ps);
                    return;
                }
            }

            // プールにない場合は破棄
            _activeSystems.Remove(ps);
            GameObject.Destroy(ps.gameObject);
        }

        /// <summary>
        /// 全エフェクトをクリア
        /// </summary>
        public void ClearAllEffects()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _activeSystems.Clear();
        }

        /// <summary>
        /// 全エフェクトの統計情報をログ出力
        /// </summary>
        public void LogStats()
        {
            foreach (var kvp in _pools)
            {
                kvp.Value.LogStats(kvp.Key);
            }
            Debug.Log($"[ParticleSystemManager] ActiveEffects={_activeSystems.Count}, MaxAllowed={maxActiveEffects}");
        }

        private void OnDestroy()
        {
            ClearAllEffects();
            if (Instance == this) Instance = null;
        }
    }
}
