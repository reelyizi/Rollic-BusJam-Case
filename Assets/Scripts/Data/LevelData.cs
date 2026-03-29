using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "BusJam/Level Data")]
public class LevelData : ScriptableObject
{
    public int levelNumber;

    [Header("Grid")]
    public int gridRows = 8;
    public int gridCols = 8;
    public StickmanPlacement[] stickmanPlacements;
    public GridCell[] activeCells;
    public SpawnerPlacement[] spawnerPlacements;

    [Header("Bus Stop")]
    public int busStopSlotCount = 6;

    [Header("Buses")]
    public BusDefinition[] busSequence;

    [Header("Timer")]
    public float timerDuration = 120f;

    public int GetGridIndex(int row, int col) => row * gridCols + col;

    public StickmanColor? GetColorAt(int row, int col)
    {
        if (stickmanPlacements == null) return null;

        for (int i = 0; i < stickmanPlacements.Length; i++)
        {
            if (stickmanPlacements[i].row == row && stickmanPlacements[i].col == col)
                return stickmanPlacements[i].color;
        }
        return null;
    }

    public bool IsCellActive(int row, int col)
    {
        if (activeCells == null) return false;

        for (int i = 0; i < activeCells.Length; i++)
        {
            if (activeCells[i].row == row && activeCells[i].col == col)
                return true;
        }
        return false;
    }

    public bool IsStickmanHidden(int row, int col)
    {
        if (stickmanPlacements == null) return false;

        for (int i = 0; i < stickmanPlacements.Length; i++)
        {
            if (stickmanPlacements[i].row == row && stickmanPlacements[i].col == col)
                return stickmanPlacements[i].isHidden;
        }
        return false;
    }

    public bool HasSpawnerAt(int row, int col)
    {
        if (spawnerPlacements == null) return false;

        for (int i = 0; i < spawnerPlacements.Length; i++)
        {
            if (spawnerPlacements[i].row == row && spawnerPlacements[i].col == col)
                return true;
        }
        return false;
    }

    public SpawnerPlacement? GetSpawnerAt(int row, int col)
    {
        if (spawnerPlacements == null) return null;

        for (int i = 0; i < spawnerPlacements.Length; i++)
        {
            if (spawnerPlacements[i].row == row && spawnerPlacements[i].col == col)
                return spawnerPlacements[i];
        }
        return null;
    }

    public static Vector2Int GetDirectionOffset(SpawnerDirection dir)
    {
        return dir switch
        {
            SpawnerDirection.Up => new Vector2Int(-1, 0),
            SpawnerDirection.Down => new Vector2Int(1, 0),
            SpawnerDirection.Left => new Vector2Int(0, -1),
            SpawnerDirection.Right => new Vector2Int(0, 1),
            _ => Vector2Int.zero
        };
    }

    public static float GetDirectionYRotation(SpawnerDirection dir)
    {
        return dir switch
        {
            SpawnerDirection.Up => 0f,
            SpawnerDirection.Right => 90f,
            SpawnerDirection.Down => 180f,
            SpawnerDirection.Left => 270f,
            _ => 0f
        };
    }
}

[Serializable]
public struct StickmanPlacement
{
    public int row;
    public int col;
    public StickmanColor color;
    public bool isHidden;
}

[Serializable]
public struct GridCell
{
    public int row;
    public int col;
}

[Serializable]
public struct BusDefinition
{
    public StickmanColor color;
    public int capacity;
}

public enum SpawnerDirection
{
    Up,
    Down,
    Left,
    Right
}

[Serializable]
public struct SpawnerPlacement
{
    public int row;
    public int col;
    public SpawnerDirection direction;
    public StickmanColor[] colorQueue;
}
