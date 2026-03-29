using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LevelEditorUtils
{
    private static readonly Dictionary<StickmanColor, Color> FallbackColors = new()
    {
        { StickmanColor.Red, Color.red },
        { StickmanColor.Blue, Color.blue },
        { StickmanColor.Green, Color.green },
        { StickmanColor.Yellow, Color.yellow },
        { StickmanColor.Pink, new Color(1f, 0.41f, 0.71f) },
        { StickmanColor.Orange, new Color(1f, 0.55f, 0f) }
    };

    public static Color GetColor(LevelEditor editor, StickmanColor c)
    {
        if (editor.colorConfig != null)
            return editor.colorConfig.GetRenderColor(c);
        return FallbackColors.TryGetValue(c, out var col) ? col : Color.white;
    }

    public static void SetPlacement(LevelData levelData, int row, int col, StickmanColor color, bool isHidden = false)
    {
        var list = new List<StickmanPlacement>(levelData.stickmanPlacements ?? new StickmanPlacement[0]);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].row == row && list[i].col == col)
            {
                list[i] = new StickmanPlacement { row = row, col = col, color = color, isHidden = isHidden };
                levelData.stickmanPlacements = list.ToArray();
                return;
            }
        }
        list.Add(new StickmanPlacement { row = row, col = col, color = color, isHidden = isHidden });
        levelData.stickmanPlacements = list.ToArray();
    }

    public static void RemovePlacement(LevelData levelData, int row, int col)
    {
        var list = new List<StickmanPlacement>(levelData.stickmanPlacements ?? new StickmanPlacement[0]);
        list.RemoveAll(p => p.row == row && p.col == col);
        levelData.stickmanPlacements = list.ToArray();
    }

    public static void FillAll(LevelEditor editor, LevelData levelData)
    {
        var list = new List<StickmanPlacement>();
        for (int r = 0; r < LevelEditor.GridRows; r++)
            for (int c = 0; c < LevelEditor.GridCols; c++)
                list.Add(new StickmanPlacement { row = r, col = c, color = editor.selectedColor });
        levelData.stickmanPlacements = list.ToArray();
        EditorUtility.SetDirty(levelData);
        editor.Visuals.RebuildScene();
    }

    public static void ClearAll(LevelEditor editor, LevelData levelData)
    {
        levelData.stickmanPlacements = new StickmanPlacement[0];
        levelData.spawnerPlacements = new SpawnerPlacement[0];
        levelData.activeCells = new GridCell[0];
        EditorUtility.SetDirty(levelData);
        editor.Visuals.ClearScene();
    }

    public static void CreateNewLevel(LevelEditor editor)
    {
        var newLevel = ScriptableObject.CreateInstance<LevelData>();
        var guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/ScriptableObjects/LevelData" });
        int count = guids.Length + 1;

        newLevel.levelNumber = count;
        newLevel.gridRows = LevelEditor.GridRows;
        newLevel.gridCols = LevelEditor.GridCols;
        newLevel.timerDuration = 120f;
        newLevel.busStopSlotCount = 6;

        string path = $"Assets/ScriptableObjects/LevelData/Level_{count:D2}.asset";
        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();

        editor.sourceLevel = newLevel;
        editor.LoadLevel();
        Debug.Log($"[LevelEditor] Created {path}");
    }
}
