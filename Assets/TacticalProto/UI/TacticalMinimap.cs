using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RYZECHo.TacticalProto.UI
{
    /// <summary>グリッドベースのミニマップ表示</summary>
    public class TacticalMinimap : MonoBehaviour
    {
        public const int DefaultGridWidth = 20;
        public const int DefaultGridHeight = 20;
        public const float DefaultCellSize = 1f;

        private int gridWidth;
        private int gridHeight;
        private float cellSize;
        private GridCell[,] gridCells;
        private Dictionary<Vector2Int, GameObject> cellMap;

        [Header("セル表示設定")]
        public GameObject cellPrefab;
        public GameObject cellPlayer, cellAlly, cellEnemy, cellStructure, cellObjective;

        [Header("表示設定")]
        public float zoomLevel = 1f;
        public Vector2 centerOffset = Vector2.zero;

        private void Awake()
        {
            gridWidth = DefaultGridWidth;
            gridHeight = DefaultGridHeight;
            cellSize = DefaultCellSize;
            gridCells = new GridCell[gridWidth, gridHeight];
            cellMap = new Dictionary<Vector2Int, GameObject>();
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    gridCells[x, y] = GridCell.Empty;
        }

        public void SetGridSize(int width, int height)
        {
            gridWidth = width; gridHeight = height;
            gridCells = new GridCell[gridWidth, gridHeight];
            cellMap.Clear(); InitializeGrid(); ClearDisplay();
        }

        public int Width => gridWidth;
        public int Height => gridHeight;
        public (int width, int height) GetGridSize() => (gridWidth, gridHeight);

        public GridCell GetCell(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return GridCell.OutOfBounds;
            return gridCells[x, y];
        }

        public void SetCell(int x, int y, GridCell cellType)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            gridCells[x, y] = cellType;
            UpdateCellDisplay(x, y);
        }

        public bool IsValidCell(int x, int y) => x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;

        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3((x - gridWidth / 2f) * cellSize, (y - gridHeight / 2f) * cellSize, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / cellSize + gridWidth / 2f),
                Mathf.RoundToInt(worldPos.y / cellSize + gridHeight / 2f));
        }

        private void UpdateCellDisplay(int x, int y)
        {
            var key = new Vector2Int(x, y);
            var cellType = gridCells[x, y];
            if (cellMap.TryGetValue(key, out var existing))
            {
                if (cellType == GridCell.Empty) { Destroy(existing); cellMap.Remove(key); }
                else { existing.SetActive(true); UpdateCellSprite(existing, cellType); }
            }
            else if (cellType != GridCell.Empty)
            {
                var go = InstantiateCell(cellType);
                go.transform.position = GridToWorld(x, y);
                cellMap[key] = go;
            }
        }

        private GameObject InstantiateCell(GridCell cellType)
        {
            var prefab = cellType switch
            {
                GridCell.Player => cellPlayer, GridCell.Ally => cellAlly,
                GridCell.Enemy => cellEnemy, GridCell.Structure => cellStructure,
                GridCell.Objective => cellObjective, _ => cellPrefab,
            };
            var go = prefab != null ? Instantiate(prefab) : new GameObject();
            go.SetActive(false); return go;
        }

        private void UpdateCellSprite(GameObject cell, GridCell cellType)
        {
            var img = cell.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = GetCellColor(cellType);
        }

        private static Color GetCellColor(GridCell cellType)
        {
            return cellType switch
            {
                GridCell.Empty => new Color(0.15f, 0.15f, 0.15f, 0.5f),
                GridCell.Wall => new Color(0.3f, 0.3f, 0.35f, 0.8f),
                GridCell.Player => new Color(0.2f, 0.6f, 1f, 0.9f),
                GridCell.Ally => new Color(0.2f, 0.8f, 0.2f, 0.9f),
                GridCell.Enemy => new Color(1f, 0.2f, 0.2f, 0.9f),
                GridCell.Structure => new Color(0.8f, 0.6f, 0.2f, 0.8f),
                GridCell.Objective => new Color(0.8f, 0.4f, 1f, 0.9f),
                _ => Color.clear,
            };
        }

        public void ClearDisplay()
        {
            foreach (var kvp in cellMap) Destroy(kvp.Value);
            cellMap.Clear();
        }

        /// <summary>エージェント表示（タイプ指定）</summary>
        public void ShowAgent(int x, int y, GridCell type)
        {
            if (!IsValidCell(x, y)) return;
            SetCell(x, y, type);
        }

        public void ShowPlayer(int x, int y) => ShowAgent(x, y, GridCell.Player);
        public void ShowAlly(int x, int y) => ShowAgent(x, y, GridCell.Ally);
        public void ShowEnemy(int x, int y) => ShowAgent(x, y, GridCell.Enemy);
        public void ShowStructure(int x, int y) => ShowAgent(x, y, GridCell.Structure);
        public void ShowObjective(int x, int y) => ShowAgent(x, y, GridCell.Objective);
        public void ShowWall(int x, int y) => ShowAgent(x, y, GridCell.Wall);

        /// <summary>フェーズに応じたミニマップ表示を初期化</summary>
        public void InitializeForPhase(GamePhase phase)
        {
            ClearDisplay();
            switch (phase)
            {
                case GamePhase.Construct: DrawMapWalls(); break;
                case GamePhase.Bet: DrawObjectives(); break;
                case GamePhase.Hunt: DrawCombatEntities(); break;
                case GamePhase.RoundResult:
                case GamePhase.Victory:
                case GamePhase.Defeat: DrawResultMap(); break;
            }
        }

        private void DrawMapWalls()
        {
            for (int x = 0; x < gridWidth; x++)
            { SetCell(x, 0, GridCell.Wall); SetCell(x, gridHeight - 1, GridCell.Wall); }
            for (int y = 0; y < gridHeight; y++)
            { SetCell(0, y, GridCell.Wall); SetCell(gridWidth - 1, y, GridCell.Wall); }
        }

        private void DrawObjectives()
        {
            SetCell(5, 10, GridCell.Objective);
            SetCell(gridWidth - 6, 10, GridCell.Objective);
        }

        private void DrawCombatEntities()
        {
            SetCell(gridWidth / 2, gridHeight / 2, GridCell.Player);
            SetCell(gridWidth / 2 - 3, gridHeight / 2 + 2, GridCell.Ally);
            SetCell(gridWidth / 2 + 3, gridHeight / 2 - 2, GridCell.Ally);
            SetCell(gridWidth / 2 - 7, gridHeight / 2 + 5, GridCell.Enemy);
            SetCell(gridWidth / 2 + 7, gridHeight / 2 - 5, GridCell.Enemy);
            SetCell(gridWidth / 2, gridHeight / 2 + 8, GridCell.Enemy);
        }

        private void DrawResultMap()
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    SetCell(x, y, GridCell.Wall);
        }

        public void SetZoom(float zoom)
        {
            zoomLevel = Mathf.Clamp(zoom, 0.5f, 3f);
            var rt = GetComponent<RectTransform>();
            if (rt != null) rt.localScale = new Vector3(zoomLevel, zoomLevel, 1f);
        }

        public void SetCenter(Vector2 offset) => centerOffset = offset;
    }
}
