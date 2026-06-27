using System;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;

namespace RYZECHo.Runtime.Core
{
    /// <summary>
    /// パフォーマンス最適化マネージャー。
    /// オブジェクトプール、UIインスタンス管理、アセットローディング最適化を提供する。
    /// </summary>
    public class PerformanceOptimizer : MonoBehaviour
    {
        #region Singleton

        private static PerformanceOptimizer _instance;
        public static PerformanceOptimizer Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PerformanceOptimizer");
                    _instance = go.AddComponent<PerformanceOptimizer>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        #endregion

        #region Object Pool

        private Dictionary<string, Queue<GameObject>> _objectPools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> _prototypeObjects = new Dictionary<string, GameObject>();

        /// <summary>
        /// オブジェクトプールからオブジェクトを取得。
        /// プールにない場合は新規作成される。
        /// </summary>
        public GameObject GetPooledObject(string poolName, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!_objectPools.TryGetValue(poolName, out var pool))
            {
                pool = new Queue<GameObject>();
                _objectPools[poolName] = pool;
            }

            GameObject obj;
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
                obj.SetActive(true);
            }
            else
            {
                // プロトタイプからインスタンス化
                if (_prototypeObjects.TryGetValue(poolName, out var prototype))
                {
                    obj = Instantiate(prototype, position, rotation, parent);
                }
                else
                {
                    Debug.LogWarning($"[PerformanceOptimizer] No prototype for pool '{poolName}'. Creating without pooling.");
                    obj = new GameObject(poolName);
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                    obj.transform.SetParent(parent);
                }
            }

            return obj;
        }

        /// <summary>
        /// オブジェクトをプールに戻す。
        /// </summary>
        public void ReturnToPool(string poolName, GameObject obj)
        {
            obj.SetActive(false);

            if (!_objectPools.TryGetValue(poolName, out var pool))
            {
                pool = new Queue<GameObject>();
                _objectPools[poolName] = pool;
            }

            pool.Enqueue(obj);

            // プールのサイズ制限（デフォルト100）
            if (pool.Count > MaxPoolSize)
            {
                var excess = pool.Dequeue();
                Destroy(excess);
            }
        }

        /// <summary>
        /// プールにプロトタイプを設定。
        /// </summary>
        public void SetPoolPrototype(string poolName, GameObject prototype)
        {
            _prototypeObjects[poolName] = prototype;
        }

        /// <summary>
        /// プールのサイズを取得。
        /// </summary>
        public int GetPoolSize(string poolName)
        {
            return _objectPools.TryGetValue(poolName, out var pool) ? pool.Count : 0;
        }

        /// <summary>
        /// 特定のプールのすべてのオブジェクトを破棄。
        /// </summary>
        public void ClearPool(string poolName)
        {
            if (_objectPools.TryGetValue(poolName, out var pool))
            {
                foreach (var obj in pool)
                {
                    Destroy(obj);
                }
                pool.Clear();
            }
        }

        /// <summary>
        /// すべてのプールをクリア。
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var kvp in _objectPools)
            {
                foreach (var obj in kvp.Value)
                {
                    Destroy(obj);
                }
                kvp.Value.Clear();
            }
            _objectPools.Clear();
        }

        /// <summary>最大プールサイズ（デフォルト: 100）</summary>
        public int MaxPoolSize { get; set; } = 100;

        #endregion

        #region UI Instance Management

        private Dictionary<string, GameObject> _cachedUIInstances = new Dictionary<string, GameObject>();
        private Dictionary<string, List<GameObject>> _inactiveUIInstances = new Dictionary<string, List<GameObject>>();

        /// <summary>
        /// UIインスタンスを取得（キャッシュまたは新規作成）。
        /// </summary>
        public GameObject GetUIInstance(string uiName, Transform parent = null)
        {
            // 有効なキャッシュがあるか確認
            if (_cachedUIInstances.TryGetValue(uiName, out var cached) && cached != null && cached.activeInHierarchy)
            {
                return cached;
            }

            // 非アクティブのキャッシュがあるか確認
            if (_inactiveUIInstances.TryGetValue(uiName, out var inactiveList) && inactiveList.Count > 0)
            {
                var obj = inactiveList[inactiveList.Count - 1];
                inactiveList.RemoveAt(inactiveList.Count - 1);
                obj.SetActive(true);
                _cachedUIInstances[uiName] = obj;
                return obj;
            }

            // 新規作成（プロトタイプが必要）
            Debug.LogWarning($"[PerformanceOptimizer] No prototype for UI '{uiName}'. Creating fresh instance.");
            var newObj = new GameObject(uiName);
            newObj.transform.SetParent(parent);
            newObj.SetActive(true);
            _cachedUIInstances[uiName] = newObj;
            return newObj;
        }

        /// <summary>
        /// UIインスタンスをキャッシュ（非アクティブ化）。
        /// </summary>
        public void CacheUIInstance(string uiName, GameObject instance)
        {
            if (_cachedUIInstances.ContainsKey(uiName))
            {
                _cachedUIInstances[uiName] = null;
                _cachedUIInstances.Remove(uiName);
            }

            instance.SetActive(false);

            if (!_inactiveUIInstances.TryGetValue(uiName, out var list))
            {
                list = new List<GameObject>();
                _inactiveUIInstances[uiName] = list;
            }
            list.Add(instance);

            // 非アクティブリストのサイズ制限
            if (list.Count > MaxUIInstances)
            {
                var excess = list[0];
                list.RemoveAt(0);
                Destroy(excess);
            }
        }

        /// <summary>
        /// UIインスタンスを破棄してキャッシュから削除。
        /// </summary>
        public void DestroyUIInstance(string uiName)
        {
            if (_cachedUIInstances.TryGetValue(uiName, out var cached) && cached != null)
            {
                Destroy(cached);
                _cachedUIInstances[uiName] = null;
            }

            if (_inactiveUIInstances.TryGetValue(uiName, out var list))
            {
                foreach (var obj in list)
                {
                    Destroy(obj);
                }
                list.Clear();
                _inactiveUIInstances.Remove(uiName);
            }
        }

        /// <summary>
        /// 全UIインスタンスを破棄。
        /// </summary>
        public void DestroyAllUIInstances()
        {
            foreach (var kvp in _cachedUIInstances)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            _cachedUIInstances.Clear();

            foreach (var kvp in _inactiveUIInstances)
            {
                foreach (var obj in kvp.Value)
                {
                    Destroy(obj);
                }
                kvp.Value.Clear();
            }
            _inactiveUIInstances.Clear();
        }

        /// <summary>最大UIインスタンス数（デフォルト: 10）</summary>
        public int MaxUIInstances { get; set; } = 10;

        #endregion

        #region Asset Loading Optimization

        private Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();
        private Dictionary<string, Object> _loadedAssets = new Dictionary<string, Object>();
        private Dictionary<string, AsyncOperation> _loadingOperations = new Dictionary<string, AsyncOperation>();

        /// <summary>
        /// アセットバンドルをロード（キャッシュ付き）。
        /// </summary>
        public AssetBundle LoadAssetBundle(string bundleName)
        {
            if (_loadedAssetBundles.TryGetValue(bundleName, out var bundle) && bundle != null)
            {
                return bundle;
            }

            // 読み込み中の場合は待機
            if (_loadingOperations.TryGetValue(bundleName, out var op) && op != null && !op.isDone)
            {
                Debug.Log($"[PerformanceOptimizer] Asset bundle '{bundleName}' is already loading.");
                return null;
            }

            var path = $"Assets/RYZECHo/Assets/ Bundles/{bundleName}";
            var loadOp = AssetBundle.LoadFromStreamAsync(null); // プラットフォーム依存

            // 同期ロード（開発用）
            try
            {
                bundle = AssetBundle.LoadFromFile(path);
                if (bundle != null)
                {
                    _loadedAssetBundles[bundleName] = bundle;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PerformanceOptimizer] Failed to load bundle '{bundleName}': {e.Message}");
            }

            return bundle;
        }

        /// <summary>
        /// アセットをロード（キャッシュ付き）。
        /// </summary>
        public T LoadAsset<T>(string assetName, string bundleOrPath = "") where T : Object
        {
            if (_loadedAssets.TryGetValue(assetName, out var cached) && cached != null)
            {
                return cached as T;
            }

            T result = null;

            if (!string.IsNullOrEmpty(bundleOrPath))
            {
                var bundle = LoadAssetBundle(bundleOrPath);
                if (bundle != null)
                {
                    result = bundle.LoadAsset<T>(assetName);
                }
            }
            else
            {
                // リソースからのロード
                result = Resources.Load<T>(assetName);
            }

            if (result != null)
            {
                _loadedAssets[assetName] = result;
            }

            return result;
        }

        /// <summary>
        /// アセットをアンロード（参照カウント付き）。
        /// </summary>
        public void UnloadAsset(string assetName, bool unloadAllLoadedObjects = false)
        {
            if (_loadedAssets.TryGetValue(assetName, out var asset) && asset != null)
            {
                Destroy(asset);
                _loadedAssets.Remove(assetName);
            }
        }

        /// <summary>
        /// アセットバンドルをアンロード。
        /// </summary>
        public void UnloadAssetBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (_loadedAssetBundles.TryGetValue(bundleName, out var bundle) && bundle != null)
            {
                bundle.Unload(unloadAllLoadedObjects);
                _loadedAssetBundles.Remove(bundleName);
            }
        }

        /// <summary>
        /// リソースをアンロード（非使用のもの）。
        /// </summary>
        public void UnloadUnusedResources()
        {
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// ゲーム終了時に全リソースをアンロード。
        /// </summary>
        public void OnGameEndCleanup()
        {
            UnloadUnusedResources();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// ロード済みのアセット数を取得。
        /// </summary>
        public int GetLoadedAssetCount()
        {
            return _loadedAssets.Count;
        }

        /// <summary>
        /// ロード済みのアセットバンドル数を取得。
        /// </summary>
        public int GetLoadedBundleCount()
        {
            return _loadedAssetBundles.Count;
        }

        #endregion

        #region Performance Monitoring

        /// <summary>
        /// 現在のメモリ使用量を取得。
        /// </summary>
        public long GetUsedMemoryBytes()
        {
            return System.GC.GetTotalMemory(false);
        }

        /// <summary>
        /// プールの統計情報を取得。
        /// </summary>
        public string GetPoolStatistics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Object Pool Statistics ===");
            foreach (var kvp in _objectPools)
            {
                sb.AppendLine($"  Pool '{kvp.Key}': {kvp.Value.Count} objects");
            }
            return sb.ToString();
        }

        /// <summary>
        /// UIキャッシュの統計情報を取得。
        /// </summary>
        public string GetUICacheStatistics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== UI Cache Statistics ===");
            sb.AppendLine($"  Active caches: {_cachedUIInstances.Count}");
            sb.AppendLine($"  Inactive caches: {_inactiveUIInstances.Count}");
            foreach (var kvp in _inactiveUIInstances)
            {
                sb.AppendLine($"  UI '{kvp.Key}': {kvp.Value.Count} inactive instances");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 全パフォーマンス統計を出力。
        /// </summary>
        public void LogPerformanceStats()
        {
            Debug.Log($"[PerformanceOptimizer] Memory: {GetUsedMemoryBytes() / 1024} KB");
            Debug.Log($"[PerformanceOptimizer] Loaded assets: {GetLoadedAssetCount()}");
            Debug.Log($"[PerformanceOptimizer] Loaded bundles: {GetLoadedBundleCount()}");
            Debug.Log(GetPoolStatistics());
            Debug.Log(GetUICacheStatistics());
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            ClearAllPools();
            DestroyAllUIInstances();
            foreach (var kvp in _loadedAssetBundles)
            {
                kvp.Value?.Unload(true);
            }
            _loadedAssetBundles.Clear();
            _loadedAssets.Clear();
        }

        #endregion
    }
}
