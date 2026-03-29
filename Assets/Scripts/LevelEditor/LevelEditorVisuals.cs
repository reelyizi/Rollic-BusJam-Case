using System.Collections.Generic;
using UnityEngine;

public class LevelEditorVisuals
{
    private readonly LevelEditor editor;
    private Transform[] gridCells;
    private readonly Dictionary<Vector2Int, GameObject> stickmen = new();
    private readonly Dictionary<Vector2Int, GameObject> walls = new();
    private readonly Dictionary<Vector2Int, GameObject> spawners = new();
    private readonly List<GameObject> buses = new();

    public LevelEditorVisuals(LevelEditor editor)
    {
        this.editor = editor;
    }

    public void CacheGridCells()
    {
        if (editor.gridParent == null) return;
        gridCells = new Transform[editor.gridParent.childCount];
        for (int i = 0; i < editor.gridParent.childCount; i++)
            gridCells[i] = editor.gridParent.GetChild(i);
    }

    public Vector3 GetCellPosition(int row, int col)
    {
        CacheGridCells();
        int index = row * LevelEditor.GridCols + col;
        if (gridCells == null || index < 0 || index >= gridCells.Length)
            return Vector3.zero;
        return gridCells[index].position;
    }

    public void RebuildScene()
    {
        ClearScene();
        CacheGridCells();

        var data = editor.editData;
        if (data == null) return;

        var active = new bool[LevelEditor.GridRows, LevelEditor.GridCols];

        if (data.activeCells != null)
            for (int i = 0; i < data.activeCells.Length; i++)
            {
                var c = data.activeCells[i];
                active[c.row, c.col] = true;
                SetCellActive(c.row, c.col, true);
            }

        if (data.stickmanPlacements != null)
            for (int i = 0; i < data.stickmanPlacements.Length; i++)
            {
                var p = data.stickmanPlacements[i];
                active[p.row, p.col] = true;
                SpawnStickman(p.row, p.col, p.color);
            }

        if (data.spawnerPlacements != null)
            for (int i = 0; i < data.spawnerPlacements.Length; i++)
            {
                var s = data.spawnerPlacements[i];
                active[s.row, s.col] = true;
                SpawnSpawner(s.row, s.col, s.direction);
            }

        for (int r = 0; r < LevelEditor.GridRows; r++)
            for (int c = 0; c < LevelEditor.GridCols; c++)
                if (!active[r, c])
                    SpawnWall(r, c);

        RebuildBuses();
    }

    public void ClearScene()
    {
        stickmen.Clear();
        walls.Clear();
        spawners.Clear();
        ClearBuses();

        for (int i = editor.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(editor.transform.GetChild(i).gameObject);

        HideAllCells();
    }

    // --- Stickman ---

    public void SpawnStickman(int row, int col, StickmanColor color)
    {
        var key = new Vector2Int(row, col);
        DestroyIfExists(stickmen, key);

        SetCellActive(row, col, true);
        RemoveWall(row, col);

        var pos = GetCellPosition(row, col);
        var obj = InstantiatePrefab(editor.stickmanPrefab, pos, Quaternion.Euler(0f, 180f, 0f));
        obj.name = $"Stickman_{row}_{col}_{color}";

        ApplyColor(obj, editor.colorConfig != null ? editor.colorConfig.GetRenderColor(color) : Color.white);
        stickmen[key] = obj;
    }

    public void RemoveStickman(int row, int col)
    {
        DestroyIfExists(stickmen, new Vector2Int(row, col));
        stickmen.Remove(new Vector2Int(row, col));
        SetCellActive(row, col, false);
        SpawnWall(row, col);
    }

    // --- Spawner ---

    public void SpawnSpawner(int row, int col, SpawnerDirection direction)
    {
        var key = new Vector2Int(row, col);
        DestroyIfExists(spawners, key);

        SetCellActive(row, col, true);
        RemoveWall(row, col);

        var pos = GetCellPosition(row, col);
        float yOffset = editor.gameConfig != null ? editor.gameConfig.spawnerYOffset : 0f;
        pos.y += yOffset;
        float yRot = LevelData.GetDirectionYRotation(direction);

        var obj = InstantiatePrefab(editor.spawnerPrefab, pos, Quaternion.Euler(0f, yRot, 0f));

        if (editor.spawnerPrefab == null)
        {
            obj.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.transform.SetParent(obj.transform);
            arrow.transform.localPosition = new Vector3(0f, 0.3f, 0.5f);
            arrow.transform.localScale = new Vector3(0.3f, 0.3f, 0.5f);
        }

        obj.name = $"Spawner_{row}_{col}_{direction}";
        spawners[key] = obj;
    }

    public void RemoveSpawner(int row, int col)
    {
        DestroyIfExists(spawners, new Vector2Int(row, col));
        spawners.Remove(new Vector2Int(row, col));
        SetCellActive(row, col, false);
        SpawnWall(row, col);
    }

    // --- Wall ---

    public void SpawnWall(int row, int col)
    {
        var key = new Vector2Int(row, col);
        RemoveWall(row, col);

        var pos = GetCellPosition(row, col);
        float yOffset = editor.gameConfig != null ? editor.gameConfig.wallYOffset : -0.5f;
        pos.y += yOffset;
        var size = GetCellSize();

        var obj = InstantiatePrefab(editor.wallPrefab, pos, Quaternion.identity);
        obj.transform.localScale = size;
        obj.name = $"Wall_{row}_{col}";
        walls[key] = obj;
    }

    public void RemoveWall(int row, int col)
    {
        var key = new Vector2Int(row, col);
        DestroyIfExists(walls, key);
        walls.Remove(key);

        string wallName = $"Wall_{row}_{col}";
        for (int i = editor.transform.childCount - 1; i >= 0; i--)
            if (editor.transform.GetChild(i).name == wallName)
                Object.DestroyImmediate(editor.transform.GetChild(i).gameObject);
    }

    // --- Bus ---

    public void RebuildBuses()
    {
        ClearBuses();

        var data = editor.editData;
        if (data == null || data.busSequence == null || editor.busPrefab == null) return;

        float gap = editor.gameConfig != null ? editor.gameConfig.busGap : 2f;
        var origin = editor.busSpawnOrigin != null ? editor.busSpawnOrigin.position : Vector3.zero;

        for (int i = 0; i < data.busSequence.Length; i++)
        {
            var def = data.busSequence[i];
            var pos = origin - new Vector3(i * gap, 0f, 0f);

            var obj = InstantiatePrefab(editor.busPrefab, pos, Quaternion.Euler(0f, 90f, 0f));
            obj.name = $"Bus_{i}_{def.color}";

            var busVisual = obj.GetComponent<BusVisual>();
            if (busVisual != null)
            {
                Color busColor = editor.colorConfig != null ? editor.colorConfig.GetRenderColor(def.color) : Color.white;
                busVisual.SetColor(busColor);
            }

            buses.Add(obj);
        }
    }

    private void ClearBuses()
    {
        for (int i = buses.Count - 1; i >= 0; i--)
            if (buses[i] != null)
                Object.DestroyImmediate(buses[i]);
        buses.Clear();
    }

    // --- Grid Cells ---

    public void SetCellActive(int row, int col, bool active)
    {
        CacheGridCells();
        int index = row * LevelEditor.GridCols + col;
        if (gridCells != null && index >= 0 && index < gridCells.Length)
            gridCells[index].gameObject.SetActive(active);
    }

    private void HideAllCells()
    {
        CacheGridCells();
        if (gridCells == null) return;
        for (int i = 0; i < gridCells.Length; i++)
            gridCells[i].gameObject.SetActive(false);
    }

    // --- Path ---

    public void TogglePathCell(int row, int col)
    {
        var data = editor.editData;
        if (data == null) return;

        bool isActive = data.IsCellActive(row, col);
        var list = new List<GridCell>(data.activeCells ?? new GridCell[0]);

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

        data.activeCells = list.ToArray();
    }

    // --- Spawner Data ---

    public void PlaceSpawner(int row, int col, SpawnerDirection direction, StickmanColor[] colorQueue)
    {
        var data = editor.editData;
        if (data == null) return;

        var list = new List<SpawnerPlacement>(data.spawnerPlacements ?? new SpawnerPlacement[0]);
        list.RemoveAll(s => s.row == row && s.col == col);
        list.Add(new SpawnerPlacement { row = row, col = col, direction = direction, colorQueue = colorQueue });
        data.spawnerPlacements = list.ToArray();

        SpawnSpawner(row, col, direction);
    }

    public void UpdateSpawner(int row, int col, SpawnerDirection direction, StickmanColor[] colorQueue)
    {
        var data = editor.editData;
        if (data == null) return;

        var list = new List<SpawnerPlacement>(data.spawnerPlacements ?? new SpawnerPlacement[0]);
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
        data.spawnerPlacements = list.ToArray();
        SpawnSpawner(row, col, direction);
    }

    public void DeleteSpawner(int row, int col)
    {
        var data = editor.editData;
        if (data == null) return;

        var list = new List<SpawnerPlacement>(data.spawnerPlacements ?? new SpawnerPlacement[0]);
        list.RemoveAll(s => s.row == row && s.col == col);
        data.spawnerPlacements = list.ToArray();

        RemoveSpawner(row, col);
    }

    // --- Helpers ---

    private Vector3 GetCellSize()
    {
        CacheGridCells();
        if (gridCells == null || gridCells.Length < 2) return Vector3.one;

        float gapX = Mathf.Abs(GetCellPosition(0, 1).x - GetCellPosition(0, 0).x);
        float gapZ = Mathf.Abs(GetCellPosition(1, 0).z - GetCellPosition(0, 0).z);
        return new Vector3(gapX == 0 ? 1f : gapX, 1f, gapZ == 0 ? 1f : gapZ);
    }

    private GameObject InstantiatePrefab(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject obj;
        if (prefab != null)
        {
#if UNITY_EDITOR
            obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, editor.transform);
#else
            obj = Object.Instantiate(prefab, editor.transform);
#endif
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.SetParent(editor.transform);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }

        obj.hideFlags = HideFlags.DontSave;
        return obj;
    }

    private static void ApplyColor(GameObject obj, Color color)
    {
        var renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        var propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", color);
        renderer.SetPropertyBlock(propBlock);
    }

    private static void DestroyIfExists(Dictionary<Vector2Int, GameObject> dict, Vector2Int key)
    {
        if (dict.TryGetValue(key, out var obj) && obj != null)
            Object.DestroyImmediate(obj);
    }
}
