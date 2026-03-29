using UnityEditor;
using UnityEngine;

public class LevelEditorGridDrawer
{
    public void Draw(LevelEditor editor, LevelData levelData)
    {
        EditorGUILayout.LabelField("Grid (click to place, click same to remove)", EditorStyles.boldLabel);

        DrawColumnHeaders();
        DrawRows(editor, levelData);
    }

    private void DrawColumnHeaders()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
        GUILayout.Space(27);
        for (int c = 0; c < LevelEditor.GridCols; c++)
        {
            var rect = GUILayoutUtility.GetRect(30, 15, GUILayout.Width(30), GUILayout.ExpandWidth(false));
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(rect, c.ToString(), style);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawRows(LevelEditor editor, LevelData levelData)
    {
        for (int r = 0; r < LevelEditor.GridRows; r++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(r.ToString(), EditorStyles.centeredGreyMiniLabel,
                GUILayout.Width(25), GUILayout.Height(30), GUILayout.ExpandWidth(false));

            for (int c = 0; c < LevelEditor.GridCols; c++)
                DrawCell(editor, levelData, r, c);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawCell(LevelEditor editor, LevelData levelData, int r, int c)
    {
        var existing = levelData.GetColorAt(r, c);
        bool isPath = levelData.IsCellActive(r, c);
        bool hasSpawner = levelData.HasSpawnerAt(r, c);
        bool isHidden = editor.hiddenMode && levelData.IsStickmanHidden(r, c);
        var rect = GUILayoutUtility.GetRect(30, 30, GUILayout.Width(30));

        DrawCellBackground(editor, rect, existing, isPath, hasSpawner, isHidden);
        DrawCellBorder(rect);
        DrawCellLabel(rect, existing, isPath, hasSpawner, isHidden, levelData, r, c);

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            HandleCellClick(editor, levelData, r, c, existing, hasSpawner);
    }

    private void DrawCellBackground(LevelEditor editor, Rect rect, StickmanColor? existing,
        bool isPath, bool hasSpawner, bool isHidden)
    {
        Color cellColor;
        if (hasSpawner)
            cellColor = new Color(0.5f, 0.2f, 0.5f);
        else if (existing.HasValue && isHidden)
            cellColor = editor.gameConfig != null ? editor.gameConfig.hiddenStickmanColor : Color.black;
        else if (existing.HasValue)
            cellColor = LevelEditorUtils.GetColor(editor, existing.Value);
        else if (isPath)
            cellColor = new Color(0.6f, 0.6f, 0.6f);
        else
            cellColor = new Color(0.25f, 0.25f, 0.25f);

        EditorGUI.DrawRect(rect, cellColor);
    }

    private void DrawCellBorder(Rect rect)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.black);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Color.black);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), Color.black);
        EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), Color.black);
    }

    private void DrawCellLabel(Rect rect, StickmanColor? existing, bool isPath,
        bool hasSpawner, bool isHidden, LevelData levelData, int r, int c)
    {
        string cellLabel;
        int fontSize;

        if (hasSpawner)
        {
            var sp = levelData.GetSpawnerAt(r, c).Value;
            string arrow = sp.direction switch
            {
                SpawnerDirection.Up => "\u2191",
                SpawnerDirection.Down => "\u2193",
                SpawnerDirection.Left => "\u2190",
                SpawnerDirection.Right => "\u2192",
                _ => "S"
            };
            int qCount = sp.colorQueue != null ? sp.colorQueue.Length : 0;
            cellLabel = $"{arrow}{qCount}";
            fontSize = 9;
        }
        else if (existing.HasValue)
        {
            string letter = existing.Value.ToString()[0].ToString();
            cellLabel = isHidden ? letter.ToLower() : letter;
            fontSize = 10;
        }
        else if (isPath)
        {
            cellLabel = "\u00b7";
            fontSize = 16;
        }
        else
        {
            cellLabel = "";
            fontSize = 10;
        }

        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            normal = { textColor = Color.white }
        };
        GUI.Label(rect, cellLabel, style);
    }

    private void HandleCellClick(LevelEditor editor, LevelData levelData,
        int r, int c, StickmanColor? existing, bool hasSpawner)
    {
        Undo.RecordObject(levelData, "Edit Grid Cell");

        if (editor.spawnerMode)
        {
            if (hasSpawner)
                editor.selectedSpawnerCell = new Vector2Int(r, c);
            else if (!existing.HasValue)
            {
                editor.Visuals.PlaceSpawner(r, c, SpawnerDirection.Up, new StickmanColor[0]);
                editor.selectedSpawnerCell = new Vector2Int(r, c);
            }
        }
        else if (editor.pathMode)
        {
            if (!hasSpawner)
            {
                bool wasActive = levelData.IsCellActive(r, c);
                editor.Visuals.TogglePathCell(r, c);

                if (wasActive && existing.HasValue)
                {
                    LevelEditorUtils.RemovePlacement(levelData, r, c);
                    editor.Visuals.RemoveStickman(r, c);
                }
            }
        }
        else if (!hasSpawner)
        {
            if (existing.HasValue && existing.Value == editor.selectedColor)
            {
                LevelEditorUtils.RemovePlacement(levelData, r, c);
                editor.Visuals.RemoveStickman(r, c);
            }
            else
            {
                LevelEditorUtils.SetPlacement(levelData, r, c, editor.selectedColor, editor.hiddenMode);
                editor.Visuals.SpawnStickman(r, c, editor.selectedColor, editor.hiddenMode);
            }
        }

        EditorUtility.SetDirty(levelData);
    }
}
