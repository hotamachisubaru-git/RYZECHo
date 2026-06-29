using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo.Runtime.Pooling
{
    /// <summary>
    /// Unity URP対応タイルレンダラー。
    /// SpriteRendererとメッシュレンダリングのハイブリッド方式で描画する。
    /// オブジェクトプールとメッシュ統合を使用してパフォーマンスを最適化。
    /// </summary>
    public class TileRenderer : MonoBehaviour
    {
        [Header("Tile Settings")]
        [SerializeField] private float _tileSize = 64f;
        [SerializeField] private int _gridWidth = 16;
        [SerializeField] private int _gridHeight = 16;

        [Header("Material")]
        [SerializeField] private Material _tileMaterial;
        [SerializeField] private Sprite _defaultTileSprite;

        [Header("Mesh Combiner")]
        [SerializeField] private bool _enableMeshCombining = true;
        [SerializeField] private float _combineInterval = 0.5f;

        private TileMeshCombiner _meshCombiner;
        private Dictionary<Vector2Int, SpriteRenderer> _tileRenderers = new();
        private float _combineTimer;
        private bool _needsUpdate = true;

        // タイルタイプ定義（System.DrawingのDrawBoardTileと対応）
        public enum TileType
        {
            Normal,       // 通常タイル
            Perimeter,    // 外周タイル
            RaisedBlock,  // 浮き上がりブロック
            BoardTile,    // ボードタイル
        }

        public static TileRenderer Instance { get; private set; }

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

            if (_enableMeshCombining)
            {
                _meshCombiner = gameObject.AddComponent<TileMeshCombiner>();
            }
        }

        private void Update()
        {
            if (_needsUpdate)
            {
                UpdateTiles();
                _needsUpdate = false;
            }

            if (_enableMeshCombining && _meshCombiner != null)
            {
                _combineTimer += Time.deltaTime;
                if (_combineTimer >= _combineInterval)
                {
                    UpdateMeshCombination();
                    _combineTimer = 0f;
                }
            }
        }

        /// <summary>
        /// タイルを描画（プール使用）。
        /// </summary>
        public void DrawTile(Vector2Int cell, TileType type, Color color, Sprite sprite = null)
        {
            if (_tileRenderers.TryGetValue(cell, out var existing))
            {
                if (existing.gameObject.activeSelf)
                {
                    existing.color = color;
                    if (sprite != null) existing.sprite = sprite;
                    return;
                }
            }

            var pos = new Vector3(cell.x * _tileSize, cell.y * _tileSize, 0f);
            var renderer = TileObjectPool.Instance?.GetTile(cell, sprite ?? _defaultTileSprite, color, _tileSize);

            if (renderer != null)
            {
                renderer.gameObject.transform.SetParent(transform);
                renderer.gameObject.name = $"Tile_{cell.x}_{cell.y}";
                _tileRenderers[cell] = renderer;
            }
        }

        /// <summary>
        /// 外周タイルを描画。
        /// </summary>
        public void DrawPerimeterTile(Vector2Int cell, Color color, Sprite sprite = null)
        {
            var pos = new Vector3(cell.x * _tileSize, cell.y * _tileSize, 0f);
            var renderer = TileObjectPool.Instance?.GetPerimeterTile(cell, sprite ?? _defaultTileSprite, color, _tileSize);

            if (renderer != null)
            {
                renderer.gameObject.transform.SetParent(transform);
                renderer.gameObject.name = $"PerimeterTile_{cell.x}_{cell.y}";
                _tileRenderers[cell] = renderer;
            }
        }

        /// <summary>
        /// 浮き上がりブロックを描画。
        /// </summary>
        public void DrawRaisedBlock(Vector2Int cell, Color topColor, Sprite sprite = null)
        {
            var pos = new Vector3(cell.x * _tileSize, cell.y * _tileSize, 0f);
            var renderer = TileObjectPool.Instance?.GetRaisedBlock(cell, sprite ?? _defaultTileSprite, topColor, topColor, _tileSize);

            if (renderer != null)
            {
                renderer.gameObject.transform.SetParent(transform);
                renderer.gameObject.name = $"RaisedBlock_{cell.x}_{cell.y}";
                _tileRenderers[cell] = renderer;
            }
        }

        /// <summary>
        /// 構造物を描画（プール使用）。
        /// </summary>
        public void DrawStructure(Vector2Int cell, StructureKind kind, Color color, Sprite sprite = null)
        {
            var renderer = StructureObjectPool.Instance?.GetStructure(kind, cell, sprite ?? _defaultTileSprite, color, _tileSize);

            if (renderer != null)
            {
                renderer.gameObject.transform.SetParent(transform);
                renderer.gameObject.name = $"{kind}_{cell.x}_{cell.y}";
                _tileRenderers[cell] = renderer;
            }
        }

        /// <summary>
        /// 単一タイルを描画（プール使用、構造化されていない場合のフォールバック）。
        /// </summary>
        public SpriteRenderer DrawSingleTile(Vector2Int cell, Sprite sprite, Color color)
        {
            var pos = new Vector3(cell.x * _tileSize, cell.y * _tileSize, 0f);
            var renderer = TileObjectPool.Instance?.GetTile(cell, sprite, color, _tileSize);

            if (renderer != null)
            {
                renderer.gameObject.transform.SetParent(transform);
                renderer.gameObject.name = $"Tile_{cell.x}_{cell.y}";
                _tileRenderers[cell] = renderer;
            }

            return renderer;
        }

        /// <summary>
        /// タイルの更新をマーク。
        /// </summary>
        public void MarkDirty()
        {
            _needsUpdate = true;
        }

        private void UpdateTiles()
        {
            // 無効になったタイルをクリーンアップ
            var keysToRemove = new List<Vector2Int>();
            foreach (var kvp in _tileRenderers)
            {
                if (kvp.Value == null || !kvp.Value.gameObject.activeInHierarchy)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _tileRenderers.Remove(key);
            }
        }

        private void UpdateMeshCombination()
        {
            if (_meshCombiner == null || !_enableMeshCombining) return;

            var tileTypes = new Dictionary<Vector2Int, int>();
            foreach (var kvp in _tileRenderers)
            {
                if (kvp.Value != null && kvp.Value.gameObject.activeInHierarchy)
                {
                    // タイルタイプをIDとして使用
                    int typeId = kvp.Value.sprite != null ? kvp.Value.sprite.name.GetHashCode() : 0;
                    tileTypes[kvp.Key] = typeId;
                }
            }

            _meshCombiner.CombineMesh(tileTypes, _tileSize);
        }

        /// <summary>
        /// 全タイルをクリア。
        /// </summary>
        public void ClearAllTiles()
        {
            foreach (var kvp in _tileRenderers)
            {
                if (kvp.Value != null && kvp.Value.gameObject.activeInHierarchy)
                {
                    kvp.Value.gameObject.SetActive(false);
                }
            }
            _tileRenderers.Clear();
            _meshCombiner?.Clear();
        }

        private void OnDestroy()
        {
            ClearAllTiles();
        }
    }
}
