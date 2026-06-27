using System;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo.Runtime.Pooling
{
    /// <summary>
    /// ジネリックオブジェクトプール。
    /// 毎フレームのGameObject生成・破棄を回避し、GCアルロケーションを削減する。
    /// </summary>
    /// <typeparam name="T">プールするGameObjectのComponent型</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> _pool = new();
        private readonly GameObject _prototype;
        private readonly Transform _parent;
        private readonly int _maxSize;

        public int Count => _pool.Count;
        public int TotalInstances { get; private set; }

        /// <param name="prototype">プールするオブジェクトのプロトタイプ（Prefab Instantiate用）</param>
        /// <param name="initialSize">初期プリファッチ数（0 = 遅延作成）</param>
        /// <param name="maxSize">最大プールサイズ（デフォルト: 200）</param>
        /// <param name="parent">プールされたオブジェクトの親Transform（null = 独立）</param>
        public ObjectPool(GameObject prototype, int initialSize = 0, int maxSize = 200, Transform parent = null)
        {
            _prototype = prototype ?? throw new ArgumentNullException(nameof(prototype));
            _maxSize = maxSize;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNew();
                _pool.Enqueue(obj);
            }
        }

        /// <summary>
        /// プールからオブジェクトを取得。
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation = default)
        {
            T component;

            if (_pool.Count > 0)
            {
                component = _pool.Dequeue();
                component.gameObject.SetActive(true);
            }
            else
            {
                component = CreateNew();
            }

            component.gameObject.transform.position = position;
            component.gameObject.transform.rotation = rotation;
            if (_parent != null)
            {
                component.gameObject.transform.SetParent(_parent);
            }

            return component;
        }

        /// <summary>
        /// オブジェクトをプールに戻す（非アクティブ化）。
        /// </summary>
        public void Return(T component)
        {
            if (component == null) return;

            var go = component.gameObject;
            go.SetActive(false);
            go.transform.SetParent(null);

            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(component);
            }
            else
            {
                TotalInstances--;
                GameObject.Destroy(go);
            }
        }

        /// <summary>
        /// すべてのオブジェクトを破棄してプールをクリア。
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                GameObject.Destroy(obj.gameObject);
            }
            TotalInstances = 0;
        }

        /// <summary>
        /// プールの統計情報をログ出力。
        /// </summary>
        public void LogStats(string name)
        {
            Debug.Log($"[ObjectPool<{typeof(T).Name}> '{name}']: Available={_pool.Count}, Total={TotalInstances}");
        }

        private T CreateNew()
        {
            var go = GameObject.Instantiate(_prototype);
            go.SetActive(false);
            if (_parent != null) go.transform.SetParent(_parent);
            TotalInstances++;
            return go.GetComponent<T>();
        }
    }
}
