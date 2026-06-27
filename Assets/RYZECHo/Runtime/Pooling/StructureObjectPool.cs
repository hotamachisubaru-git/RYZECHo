using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo.Runtime.Pooling
{
    /// <summary>
    /// 構造物用GameObjectプール。
    /// 爆発ドア・罠・シールドリレー等の構造物をプール管理する。
    /// </summary>
    public class StructureObjectPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private GameObject _blastDoorPrefab;
        [SerializeField] private GameObject _honeyTrapPrefab;
        [SerializeField] private GameObject _staticNestPrefab;
        [SerializeField] private GameObject _reconBeaconPrefab;
        [SerializeField] private GameObject _shieldRelayPrefab;
        [SerializeField] private GameObject _portableCoverPrefab;
        [SerializeField] private GameObject _visorWallPrefab;
        [SerializeField] private GameObject _holoDecoyPrefab;
        [SerializeField] private int _initialPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 200;
        [SerializeField] private Transform _structureParent;

        private readonly Dictionary<StructureKind, ObjectPool<SpriteRenderer>> _pools = new();
        private readonly Dictionary<Vector2, List<SpriteRenderer>> _activeStructures = new();

        public static StructureObjectPool Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            InitializePools();
        }

        private void InitializePools()
        {
            var protoMap = new Dictionary<StructureKind, GameObject>
            {
                [StructureKind.BlastDoor] = _blastDoorPrefab,
                [StructureKind.HoneyTrap] = _honeyTrapPrefab,
                [StructureKind.StaticNest] = _staticNestPrefab,
                [StructureKind.ReconBeacon] = _reconBeaconPrefab,
                [StructureKind.ShieldRelay] = _shieldRelayPrefab,
                [StructureKind.PortableCover] = _portableCoverPrefab,
                [StructureKind.VisorWall] = _visorWallPrefab,
                [StructureKind.HoloDecoy] = _holoDecoyPrefab,
            };

            foreach (var kvp in protoMap)
            {
                if (kvp.Value != null)
                {
                    _pools[kvp.Key] = new ObjectPool<SpriteRenderer>(kvp.Value, _initialPoolSize, _maxPoolSize, _structureParent);
                }
            }
        }

        /// <summary>
        /// 構造物をプールから取得して配置。
        /// </summary>
        public SpriteRenderer GetStructure(StructureKind kind, Vector2 cellPosition, Sprite sprite, Color color, float size)
        {
            if (!_pools.TryGetValue(kind, out var pool) || pool == null)
            {
                Debug.LogWarning($"[StructureObjectPool] No pool for {kind}");
                return null;
            }

            var renderer = pool.Get(cellPosition * size, Quaternion.identity);
            if (sprite != null) renderer.sprite = sprite;
            renderer.color = color;
            renderer.gameObject.name = $"{kind}_{cellPosition.x}_{cellPosition.y}";

            if (!_activeStructures.TryGetValue(cellPosition, out var list))
            {
                list = new List<SpriteRenderer>();
                _activeStructures[cellPosition] = list;
            }
            list.Add(renderer);

            return renderer;
        }

        /// <summary>
        /// 構造物をプールに戻す。
        /// </summary>
        public void ReturnStructure(Vector2 cellPosition, SpriteRenderer renderer)
        {
            _activeStructures.TryGetValue(cellPosition, out var list);
            list?.Remove(renderer);

            if (renderer == null) return;

            var go = renderer.gameObject;
            var name = go.name;

            // 名前からStructureKindを判定
            if (Enum.TryParse<StructureKind>(name.Split('_')[0], out var kind) && _pools.TryGetValue(kind, out var pool))
            {
                go.SetActive(false);
                go.transform.SetParent(null);
                pool.Return(renderer);
            }
        }

        /// <summary>
        /// 指定セルの全構造物を解放。
        /// </summary>
        public void ReturnAllStructuresAt(Vector2 cellPosition)
        {
            if (_activeStructures.TryGetValue(cellPosition, out var list))
            {
                foreach (var r in list)
                {
                    ReturnStructure(cellPosition, r);
                }
                list.Clear();
            }
        }

        /// <summary>
        /// 全構造物を解放。
        /// </summary>
        public void ReturnAllStructures()
        {
            _activeStructures.Clear();
            foreach (var pool in _pools.Values)
            {
                pool?.Clear();
            }
        }

        /// <summary>
        /// プールの統計情報をログ出力。
        /// </summary>
        public void LogStats()
        {
            foreach (var kvp in _pools)
            {
                kvp.Value?.LogStats(kvp.Key.ToString());
            }
        }

        private void OnDestroy()
        {
            ReturnAllStructures();
        }
    }
}
