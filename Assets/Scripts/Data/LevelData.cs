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
}

[Serializable]
public struct StickmanPlacement
{
    public int row;
    public int col;
    public StickmanColor color;
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
