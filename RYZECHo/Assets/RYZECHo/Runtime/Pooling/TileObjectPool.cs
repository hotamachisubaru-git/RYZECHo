using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo.Runtime.Pooling
{
    /// <summary>
    /// タイル用GameObjectプール。
    /// 盤面タイル（ボードタイル・壁タイル）をプール管理し、毎フレームの生成・破棄を回避する。
    /// </summary>
    public class TileObjectPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private GameObject _tilePrefab;
        [SerializeField] private GameObject _perimeterTilePrefab;
        [SerializeField] private GameObject _raisedBlockPrefab;
        [SerializeField] private int _initialPoolSize = 100;
        [SerializeField] private int _maxPoolSize = 300;
        [SerializeField] private Transform _tileParent;

        private ObjectPool<SpriteRenderer> _tilePool;
        private ObjectPool<SpriteRenderer> _perimeterTilePool;
        private ObjectPool<SpriteRenderer> _raisedBlockPool;

        private readonly Dictionary<Vector2, List<SpriteRenderer>> _activeTiles = new();

        public static TileObjectPool Instance { get; private set; }

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
            if (_tilePrefab == null || _perimeterTilePrefab == null || _raisedBlockPrefab == null)
            {
                Debug.LogWarning("[TileObjectPool] Prefabs not assigned. Use Unity Inspector to set them.");
                return;
            }

            _tilePool = new ObjectPool<SpriteRenderer>(_tilePrefab, _initialPoolSize, _maxPoolSize, _tileParent);
            _perimeterTilePool = new ObjectPool<SpriteRenderer>(_perimeterTilePrefab, _initialPoolSize, _maxPoolSize, _tileParent);
            _raisedBlockPool = new ObjectPool<SpriteRenderer>(_raisedBlockPrefab, _initialPoolSize, _maxPoolSize, _tileParent);
        }

        /// <summary>
        /// 通常タイルをプールから取得して配置。
        /// </summary>
        public SpriteRenderer GetTile(Vector2 cellPosition, Sprite sprite, Color color, float size)
        {
            var renderer = _tilePool.Get(cellPosition * size, Quaternion.identity);
            if (sprite != null) renderer.sprite = sprite;
            renderer.color = color;
            renderer.gameObject.name = $"Tile_{cellPosition.x}_{cellPosition.y}";

            if (!_activeTiles.TryGetValue(cellPosition, out var list))
            {
                list = new List<SpriteRenderer>();
                _activeTiles[cellPosition] = list;
            }
            list.Add(renderer);

            return renderer;
        }

        /// <summary>
        /// 外周タイルをプールから取得して配置。
        /// </summary>
        public SpriteRenderer GetPerimeterTile(Vector2 cellPosition, Sprite sprite, Color color, float size)
        {
            var renderer = _perimeterTilePool.Get(cellPosition * size, Quaternion.identity);
            if (sprite != null) renderer.sprite = sprite;
            renderer.color = color;
            renderer.gameObject.name = $"PerimeterTile_{cellPosition.x}_{cellPosition.y}";

            if (!_activeTiles.TryGetValue(cellPosition, out var list))
            {
                list = new List<SpriteRenderer>();
                _activeTiles[cellPosition] = list;
            }
            list.Add(renderer);

            return renderer;
        }

        /// <summary>
        /// 浮き上がりブロックをプールから取得して配置。
        /// </summary>
        public SpriteRenderer GetRaisedBlock(Vector2 cellPosition, Sprite sprite, Color topColor, Color sideColor, float size)
        {
            var renderer = _raisedBlockPool.Get(cellPosition * size, Quaternion.identity);
            if (sprite != null) renderer.sprite = sprite;
            renderer.color = topColor;
            renderer.gameObject.name = $"RaisedBlock_{cellPosition.x}_{cellPosition.y}";

            if (!_activeTiles.TryGetValue(cellPosition, out var list))
            {
                list = new List<SpriteRenderer>();
                _activeTiles[cellPosition] = list;
            }
            list.Add(renderer);

            return renderer;
        }

        /// <summary>
        /// タイルをプールに戻す（非アクティブ化）。
        /// </summary>
        public void ReturnTile(Vector2 cellPosition, SpriteRenderer renderer)
        {
            _activeTiles.TryGetValue(cellPosition, out var list);
            list?.Remove(renderer);

            if (renderer == null) return;

            var go = renderer.gameObject;
            var name = go.name;
            go.SetActive(false);
            go.transform.SetParent(null);

            if (name.StartsWith("RaisedBlock_"))
                _raisedBlockPool.Return(renderer);
            else if (name.StartsWith("PerimeterTile_"))
                _perimeterTilePool.Return(renderer);
            else
                _tilePool.Return(renderer);
        }

        /// <summary>
        /// 指定セルの全タイルをプールに戻す。
        /// </summary>
        public void ReturnAllTilesAt(Vector2 cellPosition)
        {
            if (_activeTiles.TryGetValue(cellPosition, out var list))
            {
                foreach (var r in list)
                {
                    ReturnTile(cellPosition, r);
                }
                list.Clear();
            }
        }

        /// <summary>
        /// 全タイルを解放。
        /// </summary>
        public void ReturnAllTiles()
        {
            _activeTiles.Clear();
            _tilePool?.Clear();
            _perimeterTilePool?.Clear();
            _raisedBlockPool?.Clear();
        }

        /// <summary>
        /// プールの統計情報をログ出力。
        /// </summary>
        public void LogStats()
        {
            _tilePool?.LogStats("Tile");
            _perimeterTilePool?.LogStats("PerimeterTile");
            _raisedBlockPool?.LogStats("RaisedBlock");
        }

        private void OnDestroy()
        {
            ReturnAllTiles();
        }
    }
}
