using System.Collections.Generic;
using UnityEngine;

namespace RYZECHo.Runtime.Effects
{
    /// <summary>
    /// エフェクト用GameObjectのオブジェクトプール。
    /// ParticleSystemなどのエフェクトオブジェクトを効率的に管理する。
    /// </summary>
    public class EffectPool
    {
        private readonly Queue<GameObject> _pool = new();
        private readonly GameObject _prototype;
        private readonly Transform _parent;
        private readonly int _maxSize;

        public int AvailableCount => _pool.Count;
        public int TotalInstances { get; private set; }

        public EffectPool(GameObject prototype, int initialSize = 10, int maxSize = 50, Transform parent = null)
        {
            _prototype = prototype ?? throw new System.ArgumentNullException(nameof(prototype));
            _maxSize = maxSize;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                var go = InstantiateAndDisable();
                _pool.Enqueue(go);
            }
        }

        /// <summary>
        /// プールからGameObjectを取得
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation = default)
        {
            GameObject go;

            if (_pool.Count > 0)
            {
                go = _pool.Dequeue();
                go.SetActive(true);
            }
            else
            {
                go = InstantiateAndDisable();
            }

            go.transform.position = position;
            go.transform.rotation = rotation;
            if (_parent != null) go.transform.SetParent(_parent);

            return go;
        }

        /// <summary>
        /// エフェクトが完了したらプールに戻す
        /// </summary>
        public bool Return(GameObject go)
        {
            if (go == null) return false;

            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null && ps.IsPlaying())
            {
                // まだ再生中なら戻さない（後で手動でReturn）
                return false;
            }

            go.SetActive(false);
            go.transform.SetParent(null);

            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(go);
                return true;
            }
            else
            {
                TotalInstances--;
                GameObject.Destroy(go);
                return true;
            }
        }

        /// <summary>
        /// 全オブジェクトを破棄してプールをクリア
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var go = _pool.Dequeue();
                GameObject.Destroy(go);
            }
            TotalInstances = 0;
        }

        /// <summary>
        /// プールの統計情報をログ出力
        /// </summary>
        public void LogStats(string name)
        {
            Debug.Log($"[EffectPool '{name}']: Available={_pool.Count}, Total={TotalInstances}, Max={_maxSize}");
        }

        private GameObject InstantiateAndDisable()
        {
            var go = GameObject.Instantiate(_prototype);
            go.SetActive(false);
            if (_parent != null) go.transform.SetParent(_parent);
            TotalInstances++;
            return go;
        }
    }
}
