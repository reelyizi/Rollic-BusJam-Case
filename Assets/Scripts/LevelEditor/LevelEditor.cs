using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LevelEditor : MonoBehaviour
{
    public const int GridRows = 8;
    public const int GridCols = 8;

    [Header("Data")]
    public LevelData sourceLevel;
    public ColorConfig colorConfig;

    [Header("Scene")]
    public GameObject stickmanPrefab;
    public GameObject busPrefab;
    public GameObject wallPrefab;
    public GameObject spawnerPrefab;
    public Transform gridParent;
    public Transform busSpawnOrigin;
    public GameConfig gameConfig;

    [HideInInspector] public StickmanColor selectedColor = StickmanColor.Red;
    [HideInInspector] public bool pathMode;
    [HideInInspector] public bool spawnerMode;
    [HideInInspector] public Vector2Int selectedSpawnerCell = new(-1, -1);
    [HideInInspector] public LevelData editData;

    private Transform[] gridCells;
    private readonly Dictionary<Vector2Int, GameObject> spawnedVisuals = new();
    private readonly Dictionary<Vector2Int, GameObject> spawnedWalls = new();
    private readonly Dictionary<Vector2Int, GameObject> spawnedSpawners = new();
    private readonly List<GameObject> spawnedBuses = new();

    public void LoadLevel()
    {
        if (sourceLevel == null) return;

        editData = ScriptableObject.CreateInstance<LevelData>();
        CopyLevelData(sourceLevel, editData);
        RebuildScene();
    }

    public void SaveLevel()
    {
        if (sourceLevel == null || editData == null) return;

        CopyLevelData(editData, sourceLevel);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(sourceLevel);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[LevelEditor] Saved {sourceLevel.name}");
#endif
    }

    private static void CopyLevelData(LevelData from, LevelData to)
    {
        to.levelNumber = from.levelNumber;
        to.gridRows = from.gridRows;
        to.gridCols = from.gridCols;
        to.timerDuration = from.timerDuration;
        to.busStopSlotCount = from.busStopSlotCount;

        if (from.stickmanPlacements != null)
        {
            to.stickmanPlacements = new StickmanPlacement[from.stickmanPlacements.Length];
            System.Array.Copy(from.stickmanPlacements, to.stickmanPlacements, from.stickmanPlacements.Length);
        }
        else to.stickmanPlacements = null;

        if (from.busSequence != null)
        {
            to.busSequence = new BusDefinition[from.busSequence.Length];
            System.Array.Copy(from.busSequence, to.busSequence, from.busSequence.Length);
        }
        else to.busSequence = null;

        if (from.activeCells != null)
        {
            to.activeCells = new GridCell[from.activeCells.Length];
            System.Array.Copy(from.activeCells, to.activeCells, from.activeCells.Length);
        }
        else to.activeCells = null;

        if (from.spawnerPlacements != null)
        {
            to.spawnerPlacements = new SpawnerPlacement[from.spawnerPlacements.Length];
            for (int i = 0; i < from.spawnerPlacements.Length; i++)
            {
                to.spawnerPlacements[i] = from.spawnerPlacements[i];
                if (from.spawnerPlacements[i].colorQueue != null)
                    to.spawnerPlacements[i].colorQueue = (StickmanColor[])from.spawnerPlacements[i].colorQueue.Clone();
            }
        }
        else to.spawnerPlacements = null;
    }

    public void CacheGridCells()
    {
        if (gridParent == null) return;

        gridCells = new Transform[gridParent.childCount];
        for (int i = 0; i < gridParent.childCount; i++)
            gridCells[i] = gridParent.GetChild(i);
    }

    public Vector3 GetCellPosition(int row, int col)
    {
        CacheGridCells();

        int index = row * GridCols + col;
        if (gridCells == null || index < 0 || index >= gridCells.Length)
            return Vector3.zero;

        return gridCells[index].position;
    }

    public void RebuildScene()
    {
        ClearScene();
        CacheGridCells();

        if (editData == null) return;

        bool[,] active = new bool[GridRows, GridCols];

        if (editData.activeCells != null)
        {
            for (int i = 0; i < editData.activeCells.Length; i++)
            {
                var c = editData.activeCells[i];
                active[c.row, c.col] = true;
                SetCellActive(c.row, c.col, true);
            }
        }

        if (editData.stickmanPlacements != null)
        {
            for (int i = 0; i < editData.stickmanPlacements.Length; i++)
            {
                var p = editData.stickmanPlacements[i];
                active[p.row, p.col] = true;
                SpawnVisual(p.row, p.col, p.color);
            }
        }

        if (editData.spawnerPlacements != null)
        {
            for (int i = 0; i < editData.spawnerPlacements.Length; i++)
            {
                var s = editData.spawnerPlacements[i];
                active[s.row, s.col] = true;
                SpawnSpawnerVisual(s.row, s.col, s.direction);
            }
        }

        for (int r = 0; r < GridRows; r++)
            for (int c = 0; c < GridCols; c++)
                if (!active[r, c])
                    SpawnWall(r, c);

        RebuildBuses();
    }

    public void TogglePathCell(int row, int col)
    {
        if (editData == null) return;

        bool isActive = editData.IsCellActive(row, col);

        var list = new List<GridCell>(editData.activeCells ?? new GridCell[0]);

        if (isActive)
        {
            list.RemoveAll(c => c.row == row && c.col == col);
            SetCellActive(row, col, false);
            SpawnWall(row, col);
        }
        else
        {
            list.Add(new GridCell { row = row, col = col });
            SetCellActive(row, col, true);
            RemoveWall(row, col);
        }

        editData.activeCells = list.ToArray();
    }

    public void PlaceSpawner(int row, int col, SpawnerDirection direction, StickmanColor[] colorQueue)
    {
        if (editData == null) return;

        var list = new List<SpawnerPlacement>(editData.spawnerPlacements ?? new SpawnerPlacement[0]);
        list.RemoveAll(s => s.row == row && s.col == col);
        list.Add(new SpawnerPlacement
        {
            row = row,
            col = col,
            direction = direction,
            colorQueue = colorQueue
        });
        editData.spawnerPlacements = list.ToArray();

        SetCellActive(row, col, true);
        RemoveWall(row, col);
        SpawnSpawnerVisual(row, col, direction);
    }

    public void UpdateSpawner(int row, int col, SpawnerDirection direction, StickmanColor[] colorQueue)
    {
        if (editData == null) return;

        var list = new List<SpawnerPlacement>(editData.spawnerPlacements ?? new SpawnerPlacement[0]);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].row == row && list[i].col == col)
            {
                var updated = list[i];
                updated.direction = direction;
                updated.colorQueue = colorQueue;
                list[i] = updated;
                break;
            }
        }
        editData.spawnerPlacements = list.ToArray();
        SpawnSpawnerVisual(row, col, direction);
    }

    public void RemoveSpawner(int row, int col)
    {
        if (editData == null) return;

        var list = new List<SpawnerPlacement>(editData.spawnerPlacements ?? new SpawnerPlacement[0]);
        list.RemoveAll(s => s.row == row && s.col == col);
        editData.spawnerPlacements = list.ToArray();

        RemoveSpawnerVisual(row, col);
        SetCellActive(row, col, false);
        SpawnWall(row, col);
    }

    public void ClearScene()
    {
        spawnedVisuals.Clear();
        spawnedWalls.Clear();
        spawnedSpawners.Clear();
        ClearBuses();

        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        HideAllCells();
    }

    private void HideAllCells()
    {
        CacheGridCells();
        if (gridCells == null) return;
        for (int i = 0; i < gridCells.Length; i++)
            gridCells[i].gameObject.SetActive(false);
    }

    public void SpawnVisual(int row, int col, StickmanColor color)
    {
        var key = new Vector2Int(row, col);

        if (spawnedVisuals.ContainsKey(key) && spawnedVisuals[key] != null)
            DestroyImmediate(spawnedVisuals[key]);

        SetCellActive(row, col, true);
        RemoveWall(row, col);
        Vector3 pos = GetCellPosition(row, col);
        GameObject obj;

        if (stickmanPrefab != null)
        {
#if UNITY_EDITOR
            obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(stickmanPrefab, transform);
#else
            obj = Instantiate(stickmanPrefab, transform);
#endif
            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.SetParent(transform);
            obj.transform.position = pos;
            obj.transform.localScale = Vector3.one * 0.4f;
        }

        obj.name = $"Stickman_{row}_{col}_{color}";
        obj.hideFlags = HideFlags.DontSave;

        var renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Color renderColor = colorConfig != null ? colorConfig.GetRenderColor(color) : Color.white;
            var propBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", renderColor);
            renderer.SetPropertyBlock(propBlock);
        }

        spawnedVisuals[key] = obj;
    }

    public void RemoveVisual(int row, int col)
    {
        var key = new Vector2Int(row, col);
        if (spawnedVisuals.TryGetValue(key, out var obj) && obj != null)
            DestroyImmediate(obj);

        spawnedVisuals.Remove(key);
        SetCellActive(row, col, false);
        SpawnWall(row, col);
    }

    public void SpawnSpawnerVisual(int row, int col, SpawnerDirection direction)
    {
        var key = new Vector2Int(row, col);
        RemoveSpawnerVisual(row, col);

        SetCellActive(row, col, true);
        RemoveWall(row, col);
        Vector3 pos = GetCellPosition(row, col);
        float yOffset = gameConfig != null ? gameConfig.spawnerYOffset : 0f;
        pos.y += yOffset;
        float yRot = LevelData.GetDirectionYRotation(direction);
        GameObject obj;

        if (spawnerPrefab != null)
        {
#if UNITY_EDITOR
            obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(spawnerPrefab, transform);
#else
            obj = Instantiate(spawnerPrefab, transform);
#endif
            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.SetParent(transform);
            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
            obj.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var arrowObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrowObj.transform.SetParent(obj.transform);
            arrowObj.transform.localPosition = new Vector3(0f, 0.3f, 0.5f);
            arrowObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.5f);
        }

        obj.name = $"Spawner_{row}_{col}_{direction}";
        obj.hideFlags = HideFlags.DontSave;
        spawnedSpawners[key] = obj;
    }

    private void RemoveSpawnerVisual(int row, int col)
    {
        var key = new Vector2Int(row, col);
        if (spawnedSpawners.TryGetValue(key, out var obj) && obj != null)
            DestroyImmediate(obj);
        spawnedSpawners.Remove(key);
    }

    private Vector3 GetCellSize()
    {
        CacheGridCells();
        if (gridCells == null || gridCells.Length < 2) return Vector3.one;

        float gapX = Mathf.Abs(GetCellPosition(0, 1).x - GetCellPosition(0, 0).x);
        float gapZ = Mathf.Abs(GetCellPosition(1, 0).z - GetCellPosition(0, 0).z);

        if (gapX == 0) gapX = 1f;
        if (gapZ == 0) gapZ = 1f;

        return new Vector3(gapX, 1f, gapZ);
    }

    private void SpawnWall(int row, int col)
    {
        var key = new Vector2Int(row, col);
        RemoveWall(row, col);

        Vector3 pos = GetCellPosition(row, col);
        float yOffset = gameConfig != null ? gameConfig.wallYOffset : -0.5f;
        pos.y += yOffset;
        Vector3 size = GetCellSize();
        GameObject obj;

        if (wallPrefab != null)
        {
#if UNITY_EDITOR
            obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(wallPrefab, transform);
#else
            obj = Instantiate(wallPrefab, transform);
#endif
            obj.transform.position = pos;
            obj.transform.localScale = size;
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.SetParent(transform);
            obj.transform.position = pos;
            obj.transform.localScale = size;
        }

        obj.name = $"Wall_{row}_{col}";
        obj.hideFlags = HideFlags.DontSave;
        spawnedWalls[key] = obj;
    }

    private void RemoveWall(int row, int col)
    {
        var key = new Vector2Int(row, col);
        if (spawnedWalls.TryGetValue(key, out var obj) && obj != null)
            DestroyImmediate(obj);
        spawnedWalls.Remove(key);

        // Fallback: find by name in case dictionary lost track
        string wallName = $"Wall_{row}_{col}";
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i).name == wallName)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    private void SetCellActive(int row, int col, bool active)
    {
        CacheGridCells();
        int index = row * GridCols + col;
        if (gridCells != null && index >= 0 && index < gridCells.Length)
            gridCells[index].gameObject.SetActive(active);
    }

    public void RebuildBuses()
    {
        ClearBuses();

        if (editData == null || editData.busSequence == null || busPrefab == null) return;

        float gap = gameConfig != null ? gameConfig.busGap : 2f;
        Vector3 origin = busSpawnOrigin != null ? busSpawnOrigin.position : Vector3.zero;

        for (int i = 0; i < editData.busSequence.Length; i++)
        {
            var def = editData.busSequence[i];
            Vector3 pos = origin - new Vector3(i * gap, 0f, 0f);

#if UNITY_EDITOR
            var obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(busPrefab, transform);
#else
            var obj = Instantiate(busPrefab, transform);
#endif
            obj.transform.position = pos;
            obj.name = $"Bus_{i}_{def.color}";
            obj.hideFlags = HideFlags.DontSave;

            var busVisual = obj.GetComponent<BusVisual>();
            if (busVisual != null)
            {
                Color busColor = colorConfig != null ? colorConfig.GetRenderColor(def.color) : Color.white;
                busVisual.SetColor(busColor);
            }

            spawnedBuses.Add(obj);
        }
    }

    private void ClearBuses()
    {
        for (int i = spawnedBuses.Count - 1; i >= 0; i--)
        {
            if (spawnedBuses[i] != null)
                DestroyImmediate(spawnedBuses[i]);
        }
        spawnedBuses.Clear();
    }
}
