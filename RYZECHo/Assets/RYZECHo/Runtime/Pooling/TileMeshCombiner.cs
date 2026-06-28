using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo.Runtime.Pooling
{
    /// <summary>
    /// 隣接する同種タイルをメッシュ結合してDrawCallを削減するコンポーネント。
    /// カードinal方向（上下左右）に隣接する同じタイルを1つのメッシュに結合する。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TileMeshCombiner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _tileSize = 64f;
        [SerializeField] private int _maxGridWidth = 16;
        [SerializeField] private int _maxGridHeight = 16;
        [SerializeField] private bool _enableDiagonalCut = false;

        private Mesh _combinedMesh;
        private Dictionary<int, List<MeshData>> _meshGroups = new();

        private struct MeshData
        {
            public Vector3[] vertices;
            public int[] triangles;
            public Vector2[] uv;
            public UnityEngine.Color[] colors;
            public Vector3[] normals;
        }

        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        private static readonly Vector2Int[] DiagonalDirections =
        {
            new(-1, 1), new(1, 1), new(-1, -1), new(1, -1)
        };

        /// <summary>
        /// タイルグリッドからメッシュを結合して生成。
        /// </summary>
        /// <param name="tiles">キー: cell position, 値: タイルタイプID</param>
        /// <param name="tileSize">タイルサイズ</param>
        /// <param name="spriteTexture">Spriteのテクスチャ（UV計算用）</param>
        public void CombineMesh(Dictionary<Vector2Int, int> tiles, float tileSize = 0f, Texture2D spriteTexture = null)
        {
            if (tiles == null || tiles.Count == 0) return;

            if (tileSize > 0f) _tileSize = tileSize;

            // タイルタイプごとにグループ化
            _meshGroups.Clear();
            foreach (var kvp in tiles)
            {
                if (!_meshGroups.TryGetValue(kvp.Value, out var list))
                {
                    list = new List<MeshData>();
                    _meshGroups[kvp.Value] = list;
                }

                var data = new MeshData
                {
                    vertices = new Vector3[4],
                    triangles = new int[6],
                    uv = new Vector2[4],
                    colors = new UnityEngine.Color[4],
                    normals = new Vector3[4] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward }
                };

                var center = kvp.Key;
                var halfSize = _tileSize * 0.5f;

                // 隣接チェックで四隅を決定
                var corners = GetTileCorners(center, tiles);
                var vertexCount = 0;

                for (int i = 0; i < 4; i++)
                {
                    if (corners[i])
                    {
                        var cornerWorld = GetCornerWorldPos(center, i);
                        data.vertices[vertexCount] = cornerWorld;

                        // UV計算
                        var uvX = (cornerWorld.x + _tileSize * _maxGridWidth * 0.5f) / (_tileSize * _maxGridWidth);
                        var uvY = (cornerWorld.y + _tileSize * _maxGridHeight * 0.5f) / (_tileSize * _maxGridHeight);
                        data.uv[vertexCount] = new Vector2(uvX, uvY);

                        // カラーはデフォルト白
                        data.colors[vertexCount] = UnityEngine.Color.white;
                        vertexCount++;
                    }
                }

                // トリアングル作成（頂点数に応じて）
                if (vertexCount >= 3)
                {
                    if (vertexCount == 4)
                    {
                        data.triangles[0] = 0;
                        data.triangles[1] = 1;
                        data.triangles[2] = 2;
                        data.triangles[3] = 0;
                        data.triangles[4] = 2;
                        data.triangles[5] = 3;
                    }
                    else if (vertexCount == 3)
                    {
                        data.triangles[0] = 0;
                        data.triangles[1] = 1;
                        data.triangles[2] = 2;
                    }
                }

                list.Add(data);
            }

            // グループごとにメッシュを生成
            ApplyCombinedMeshes();
        }

        /// <summary>
        /// タイルの四隅が描画対象かどうかを判定（隣接タイルがあるか）。
        /// </summary>
        private bool[] GetTileCorners(Vector2Int center, Dictionary<Vector2Int, int> tiles)
        {
            return new bool[4]
            {
                tiles.ContainsKey(center + new Vector2Int(-1, 1)),  // Top-Left
                tiles.ContainsKey(center + new Vector2Int(1, 1)),   // Top-Right
                tiles.ContainsKey(center + new Vector2Int(1, -1)),  // Bottom-Right
                tiles.ContainsKey(center + new Vector2Int(-1, -1)), // Bottom-Left
            };
        }

        private Vector3 GetCornerWorldPos(Vector2Int center, int cornerIndex)
        {
            var half = _tileSize * 0.5f;
            return cornerIndex switch
            {
                0 => new Vector3((center.x - 0.5f) * _tileSize, (center.y + 0.5f) * _tileSize, 0f),
                1 => new Vector3((center.x + 0.5f) * _tileSize, (center.y + 0.5f) * _tileSize, 0f),
                2 => new Vector3((center.x + 0.5f) * _tileSize, (center.y - 0.5f) * _tileSize, 0f),
                3 => new Vector3((center.x - 0.5f) * _tileSize, (center.y - 0.5f) * _tileSize, 0f),
                _ => Vector3.zero,
            };
        }

        private void ApplyCombinedMeshes()
        {
            // 既存メッシュをクリア
            if (_combinedMesh != null)
            {
                Destroy(_combinedMesh);
            }

            // タイプごとに別メッシュとして生成（マテリアルごとに分離）
            var meshFilters = GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length == 0) return;

            var meshFilter = meshFilters[0];
            _combinedMesh = new Mesh();
            _combinedMesh.name = "CombinedTileMesh";

            // 全グループの頂点を結合（単一メッシュ）
            var allVerts = new List<Vector3>();
            var allTris = new List<int>();
            var allUvs = new List<Vector2>();
            var allColors = new List<UnityEngine.Color>();
            var allNormals = new List<Vector3>();
            var vertexOffset = 0;

            foreach (var kvp in _meshGroups)
            {
                foreach (var data in kvp.Value)
                {
                    for (int i = 0; i < data.vertices.Length; i++)
                    {
                        allVerts.Add(data.vertices[i]);
                        allUvs.Add(data.uv[i]);
                        allColors.Add(data.colors[i]);
                        allNormals.Add(data.normals[i]);
                    }

                    for (int i = 0; i < data.triangles.Length; i++)
                    {
                        allTris.Add(data.triangles[i] + vertexOffset);
                    }

                    vertexOffset += data.vertices.Length;
                }
            }

            _combinedMesh.SetVertices(allVerts);
            _combinedMesh.SetUVs(0, allUvs);
            _combinedMesh.SetColors(allColors);
            _combinedMesh.SetNormals(allNormals);
            _combinedMesh.SetTriangles(allTris, 0);
            _combinedMesh.RecalculateBounds();

            meshFilter.sharedMesh = _combinedMesh;
        }

        /// <summary>
        /// メッシュをクリアして解放。
        /// </summary>
        public void Clear()
        {
            if (_combinedMesh != null)
            {
                Destroy(_combinedMesh);
                _combinedMesh = null;
            }
            _meshGroups.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
