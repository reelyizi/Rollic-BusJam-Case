using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject stickmanPrefab;
    [SerializeField] private GameObject spawnerPrefab;
    [SerializeField] private float cellSize = 1.2f;
    [SerializeField] private Transform gridOrigin;

    private bool[,] occupied;
    private bool[,] walkable;
    private Stickman[,] stickmen;
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

        PathFinder.BuildGrids(levelData, out walkable, out var _);
        occupied = new bool[rows, cols];

        if (levelData.stickmanPlacements != null)
        {
            for (int i = 0; i < levelData.stickmanPlacements.Length; i++)
            {
                var placement = levelData.stickmanPlacements[i];
                SpawnStickman(placement.row, placement.col, placement.color);
            }
        }

        if (levelData.spawnerPlacements != null)
        {
            for (int i = 0; i < levelData.spawnerPlacements.Length; i++)
                InitSpawner(levelData.spawnerPlacements[i]);
        }
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

    private void SpawnStickman(int row, int col, StickmanColor color)
    {
        Vector3 worldPos = GridToWorldPosition(row, col);
        var obj = ObjectPool.Instance.Get(stickmanPrefab, worldPos, Quaternion.identity);
        var stickman = obj.GetComponent<Stickman>();
        stickman.Initialize(color, row, col, colorConfig);

        occupied[row, col] = true;
        stickmen[row, col] = stickman;
    }

    public void SpawnStickmanAt(int row, int col, StickmanColor color)
    {
        SpawnStickman(row, col, color);
    }

    public Vector3 GridToWorldPosition(int row, int col)
    {
        Vector3 origin = gridOrigin != null ? gridOrigin.position : Vector3.zero;
        float x = origin.x + col * cellSize;
        float z = origin.z - row * cellSize;
        return new Vector3(x, origin.y, z);
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
