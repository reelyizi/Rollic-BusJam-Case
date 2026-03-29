using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelEditorValidation
{
    private bool isValid;

    public void Draw(LevelEditor editor, LevelData levelData)
    {
        isValid = true;

        int placedCount = levelData.stickmanPlacements != null ? levelData.stickmanPlacements.Length : 0;
        int spawnerQueueCount = CountSpawnerQueue(levelData);
        int stickmanCount = placedCount + spawnerQueueCount;

        int busCount = levelData.busSequence != null ? levelData.busSequence.Length : 0;
        int totalBusCapacity = CountBusCapacity(levelData);

        var colorCounts = CountStickmenPerColor(levelData);
        var busCapacityPerColor = CountBusCapacityPerColor(levelData);

        ValidateCounts(stickmanCount, busCount, totalBusCapacity);
        ValidateColors(colorCounts, busCapacityPerColor);
        ValidateSpawnersAndPaths(levelData, stickmanCount);

        if (isValid)
            EditorGUILayout.HelpBox($"Level valid! {stickmanCount} stickmen, {busCount} buses.", MessageType.Info);

        EditorGUILayout.Space(5);

        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = isValid ? Color.green : new Color(0.8f, 0.3f, 0.3f);
        EditorGUI.BeginDisabledGroup(!isValid);
        if (GUILayout.Button("Save Level", GUILayout.Height(30)))
            editor.SaveLevel();
        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = prevBg;
    }

    private void ValidateCounts(int stickmanCount, int busCount, int totalBusCapacity)
    {
        if (stickmanCount == 0)
        {
            EditorGUILayout.HelpBox("No stickmen placed.", MessageType.Warning);
            isValid = false;
        }

        if (busCount == 0 && stickmanCount > 0)
        {
            EditorGUILayout.HelpBox("No buses! Add buses to match stickmen.", MessageType.Error);
            isValid = false;
        }

        if (stickmanCount > totalBusCapacity && busCount > 0)
        {
            EditorGUILayout.HelpBox($"Too many stickmen ({stickmanCount}) for bus capacity ({totalBusCapacity}).", MessageType.Error);
            isValid = false;
        }

        if (totalBusCapacity > stickmanCount && busCount > 0)
        {
            EditorGUILayout.HelpBox($"Too many bus seats ({totalBusCapacity}) for {stickmanCount} stickmen.", MessageType.Warning);
            isValid = false;
        }
    }

    private void ValidateColors(Dictionary<StickmanColor, int> colorCounts, Dictionary<StickmanColor, int> busCapacityPerColor)
    {
        foreach (var kvp in colorCounts)
        {
            busCapacityPerColor.TryGetValue(kvp.Key, out int cap);
            if (cap == 0)
            {
                EditorGUILayout.HelpBox($"{kvp.Key}: {kvp.Value} stickmen but no bus!", MessageType.Error);
                isValid = false;
            }
            else if (kvp.Value > cap)
            {
                EditorGUILayout.HelpBox($"{kvp.Key}: {kvp.Value} stickmen but only {cap} seats.", MessageType.Error);
                isValid = false;
            }
            else if (kvp.Value < cap)
            {
                EditorGUILayout.HelpBox($"{kvp.Key}: {kvp.Value} stickmen but {cap} seats (extra).", MessageType.Warning);
                isValid = false;
            }
        }

        foreach (var kvp in busCapacityPerColor)
        {
            if (!colorCounts.ContainsKey(kvp.Key))
            {
                EditorGUILayout.HelpBox($"{kvp.Key}: bus exists but no stickmen!", MessageType.Warning);
                isValid = false;
            }
        }
    }

    private void ValidateSpawnersAndPaths(LevelData levelData, int stickmanCount)
    {
        PathFinder.BuildGrids(levelData, out var walkable, out _);
        int rows = levelData.gridRows;
        int cols = levelData.gridCols;

        // Spawner target validation
        if (levelData.spawnerPlacements != null)
        {
            for (int i = 0; i < levelData.spawnerPlacements.Length; i++)
            {
                var sp = levelData.spawnerPlacements[i];
                var offset = LevelData.GetDirectionOffset(sp.direction);
                int tr = sp.row + offset.x;
                int tc = sp.col + offset.y;

                if (tr < 0 || tr >= rows || tc < 0 || tc >= cols)
                {
                    EditorGUILayout.HelpBox($"Spawner [{sp.row},{sp.col}] faces outside the grid!", MessageType.Error);
                    isValid = false;
                }
                else if (!walkable[tr, tc])
                {
                    EditorGUILayout.HelpBox($"Spawner [{sp.row},{sp.col}] faces a wall at [{tr},{tc}]!", MessageType.Error);
                    isValid = false;
                }
            }
        }

        // Stickman path validation
        if (stickmanCount > 0 && levelData.stickmanPlacements != null)
        {
            var empty = new bool[rows, cols];
            var blocked = new List<string>();

            for (int i = 0; i < levelData.stickmanPlacements.Length; i++)
            {
                var p = levelData.stickmanPlacements[i];
                if (!PathFinder.HasPathToTop(walkable, empty, p.row, p.col, rows, cols))
                    blocked.Add($"{p.color} at [{p.row},{p.col}]");
            }

            if (blocked.Count > 0)
            {
                EditorGUILayout.HelpBox($"Isolated stickmen:\n{string.Join("\n", blocked)}", MessageType.Error);
                isValid = false;
            }
            else
            {
                EditorGUILayout.HelpBox("All stickmen can reach the top row.", MessageType.Info);
            }
        }
    }

    private int CountSpawnerQueue(LevelData levelData)
    {
        int count = 0;
        if (levelData.spawnerPlacements == null) return count;
        for (int i = 0; i < levelData.spawnerPlacements.Length; i++)
            if (levelData.spawnerPlacements[i].colorQueue != null)
                count += levelData.spawnerPlacements[i].colorQueue.Length;
        return count;
    }

    private int CountBusCapacity(LevelData levelData)
    {
        int total = 0;
        if (levelData.busSequence == null) return total;
        for (int i = 0; i < levelData.busSequence.Length; i++)
            total += levelData.busSequence[i].capacity;
        return total;
    }

    private Dictionary<StickmanColor, int> CountStickmenPerColor(LevelData levelData)
    {
        var counts = new Dictionary<StickmanColor, int>();

        if (levelData.stickmanPlacements != null)
            for (int i = 0; i < levelData.stickmanPlacements.Length; i++)
                Increment(counts, levelData.stickmanPlacements[i].color);

        if (levelData.spawnerPlacements != null)
            for (int i = 0; i < levelData.spawnerPlacements.Length; i++)
                if (levelData.spawnerPlacements[i].colorQueue != null)
                    for (int q = 0; q < levelData.spawnerPlacements[i].colorQueue.Length; q++)
                        Increment(counts, levelData.spawnerPlacements[i].colorQueue[q]);

        return counts;
    }

    private Dictionary<StickmanColor, int> CountBusCapacityPerColor(LevelData levelData)
    {
        var counts = new Dictionary<StickmanColor, int>();
        if (levelData.busSequence == null) return counts;

        for (int i = 0; i < levelData.busSequence.Length; i++)
        {
            var c = levelData.busSequence[i].color;
            if (!counts.ContainsKey(c)) counts[c] = 0;
            counts[c] += levelData.busSequence[i].capacity;
        }
        return counts;
    }

    private static void Increment(Dictionary<StickmanColor, int> dict, StickmanColor key)
    {
        if (!dict.ContainsKey(key)) dict[key] = 0;
        dict[key]++;
    }
}
