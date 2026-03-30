using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject stickmanPrefab;
    [SerializeField] private GameObject spawnerPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameConfig gameConfig;

    private bool[,] occupied;
    private bool[,] walkable;
    private Stickman[,] stickmen;
    private Transform[] gridCells;
    private int rows;
    private int cols;
    private ColorConfig colorConfig;
    private readonly List<SpawnerController> spawners = new();

    public int Rows => rows;
    public int Cols => cols;

    public void Initialize(LevelData levelData, ColorConfig config)
    {
        colorConfig = config;
        rows = levelData.gridRows;
        cols = levelData.gridCols;
        stickmen = new Stickman[rows, cols];

        CacheGridCells();
        PathFinder.BuildGrids(levelData, out walkable, out var _);
        occupied = new bool[rows, cols];

        if (levelData.stickmanPlacements != null)
        {
            for (int i = 0; i < levelData.stickmanPlacements.Length; i++)
            {
                var placement = levelData.stickmanPlacements[i];
                SpawnStickman(placement.row, placement.col, placement.color, placement.isHidden, placement.isReserved);
            }
        }

        if (levelData.spawnerPlacements != null)
        {
            for (int i = 0; i < levelData.spawnerPlacements.Length; i++)
                InitSpawner(levelData.spawnerPlacements[i]);
        }

        SetupGridVisuals(levelData);
    }

    private void SetupGridVisuals(LevelData levelData)
    {
        var active = new bool[rows, cols];

        if (levelData.activeCells != null)
            for (int i = 0; i < levelData.activeCells.Length; i++)
                active[levelData.activeCells[i].row, levelData.activeCells[i].col] = true;

        if (levelData.stickmanPlacements != null)
            for (int i = 0; i < levelData.stickmanPlacements.Length; i++)
                active[levelData.stickmanPlacements[i].row, levelData.stickmanPlacements[i].col] = true;

        if (levelData.spawnerPlacements != null)
            for (int i = 0; i < levelData.spawnerPlacements.Length; i++)
                active[levelData.spawnerPlacements[i].row, levelData.spawnerPlacements[i].col] = true;

        float yOffset = gameConfig != null ? gameConfig.wallYOffset : -0.5f;
        Vector3 cellSize = GetCellSize();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;
                if (gridCells == null || index >= gridCells.Length) continue;

                if (active[r, c])
                {
                    gridCells[index].gameObject.SetActive(true);
                }
                else
                {
                    gridCells[index].gameObject.SetActive(false);

                    if (wallPrefab != null)
                    {
                        var pos = gridCells[index].position;
                        pos.y += yOffset;
                        var wall = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
                        wall.transform.localScale = cellSize;
                        wall.name = $"Wall_{r}_{c}";
                    }
                }
            }
        }
    }

    private void CacheGridCells()
    {
        if (gridParent == null) return;
        gridCells = new Transform[gridParent.childCount];
        for (int i = 0; i < gridParent.childCount; i++)
            gridCells[i] = gridParent.GetChild(i);
    }

    public Vector3 GridToWorldPosition(int row, int col)
    {
        int index = row * cols + col;
        if (gridCells != null && index >= 0 && index < gridCells.Length)
            return gridCells[index].position;
        return Vector3.zero;
    }

    private void InitSpawner(SpawnerPlacement placement)
    {
        Vector3 worldPos = GridToWorldPosition(placement.row, placement.col);
        float yRot = LevelData.GetDirectionYRotation(placement.direction);

        GameObject obj;
        if (spawnerPrefab != null)
            obj = ObjectPool.Instance.Get(spawnerPrefab, worldPos, Quaternion.Euler(0f, yRot, 0f));
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = worldPos;
            obj.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
            obj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }

        var controller = obj.GetComponent<SpawnerController>();
        if (controller == null)
            controller = obj.AddComponent<SpawnerController>();

        controller.Initialize(placement, this, colorConfig);
        spawners.Add(controller);

        occupied[placement.row, placement.col] = true;
    }

    private void SpawnStickman(int row, int col, StickmanColor color, bool isHidden = false, bool isReserved = false)
    {
        Vector3 worldPos = GridToWorldPosition(row, col);
        var obj = ObjectPool.Instance.Get(stickmanPrefab, worldPos, Quaternion.identity);
        var stickman = obj.GetComponent<Stickman>();
        stickman.Initialize(color, row, col, colorConfig, isHidden, gameConfig, isReserved);

        occupied[row, col] = true;
        stickmen[row, col] = stickman;
    }

    public void SpawnStickmanAt(int row, int col, StickmanColor color)
    {
        SpawnStickman(row, col, color, false);
    }

    private Vector3 GetCellSize()
    {
        if (gridCells == null || gridCells.Length < 2) return Vector3.one;

        float gapX = Mathf.Abs(GridToWorldPosition(0, 1).x - GridToWorldPosition(0, 0).x);
        float gapZ = Mathf.Abs(GridToWorldPosition(1, 0).z - GridToWorldPosition(0, 0).z);
        return new Vector3(gapX == 0 ? 1f : gapX, 1f, gapZ == 0 ? 1f : gapZ);
    }

    public bool IsCellEmpty(int row, int col)
    {
        if (row < 0 || row >= rows || col < 0 || col >= cols) return false;
        return !occupied[row, col];
    }

    public Stickman GetOccupant(int row, int col)
    {
        if (row < 0 || row >= rows || col < 0 || col >= cols) return null;
        return stickmen[row, col];
    }

    public void ClearCell(int row, int col)
    {
        occupied[row, col] = false;
        stickmen[row, col] = null;
    }

    public void SetCellOccupant(int row, int col, Stickman stickman)
    {
        occupied[row, col] = true;
        stickmen[row, col] = stickman;
        stickman.GridRow = row;
        stickman.GridCol = col;
    }

    public int FindClosestEmptyTopCol(int preferredCol)
    {
        if (!occupied[0, preferredCol]) return preferredCol;

        for (int offset = 1; offset < cols; offset++)
        {
            int left = preferredCol - offset;
            int right = preferredCol + offset;

            if (left >= 0 && !occupied[0, left]) return left;
            if (right < cols && !occupied[0, right]) return right;
        }

        return -1;
    }

    public bool[,] GetOccupiedGrid()
    {
        return occupied;
    }

    public bool[,] GetWalkableGrid()
    {
        return walkable;
    }

    public int StickmanCount()
    {
        int count = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (stickmen[r, c] != null) count++;
        return count;
    }

    public bool AllSpawnersExhausted()
    {
        for (int i = 0; i < spawners.Count; i++)
            if (!spawners[i].IsExhausted) return false;
        return true;
    }

    public void RefreshAllPaths()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (stickmen[r, c] == null) continue;

                bool hasPath = PathFinder.HasPathToTop(walkable, occupied, r, c, rows, cols);
                stickmen[r, c].SetHasPath(hasPath);
            }
        }
    }
}
