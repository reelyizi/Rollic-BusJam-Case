using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelEditor))]
public class LevelEditorInspector : Editor
{
    private readonly Dictionary<StickmanColor, Color> fallbackColors = new()
    {
        { StickmanColor.Red, Color.red },
        { StickmanColor.Blue, Color.blue },
        { StickmanColor.Green, Color.green },
        { StickmanColor.Yellow, Color.yellow },
        { StickmanColor.Pink, new Color(1f, 0.41f, 0.71f) },
        { StickmanColor.Orange, new Color(1f, 0.55f, 0f) }
    };

    private Color GetColor(LevelEditor editor, StickmanColor c)
    {
        if (editor.colorConfig != null)
            return editor.colorConfig.GetRenderColor(c);
        return fallbackColors.TryGetValue(c, out var col) ? col : Color.white;
    }

    public override void OnInspectorGUI()
    {
        var editor = (LevelEditor)target;

        EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
        editor.colorConfig = (ColorConfig)EditorGUILayout.ObjectField("Color Config", editor.colorConfig, typeof(ColorConfig), false);
        editor.gameConfig = (GameConfig)EditorGUILayout.ObjectField("Game Config", editor.gameConfig, typeof(GameConfig), false);
        editor.stickmanPrefab = (GameObject)EditorGUILayout.ObjectField("Stickman Prefab", editor.stickmanPrefab, typeof(GameObject), false);
        editor.busPrefab = (GameObject)EditorGUILayout.ObjectField("Bus Prefab", editor.busPrefab, typeof(GameObject), false);
        editor.wallPrefab = (GameObject)EditorGUILayout.ObjectField("Wall Prefab", editor.wallPrefab, typeof(GameObject), false);
        editor.gridParent = (Transform)EditorGUILayout.ObjectField("Grid Parent", editor.gridParent, typeof(Transform), true);
        editor.busSpawnOrigin = (Transform)EditorGUILayout.ObjectField("Bus Spawn Origin", editor.busSpawnOrigin, typeof(Transform), true);

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Level Data", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        var prevSource = editor.sourceLevel;
        editor.sourceLevel = (LevelData)EditorGUILayout.ObjectField(editor.sourceLevel, typeof(LevelData), false);

        if (editor.sourceLevel != prevSource || (editor.sourceLevel != null && editor.editData == null))
            editor.LoadLevel();

        if (GUILayout.Button("New", GUILayout.Width(50)))
            CreateNewLevel(editor);
        EditorGUILayout.EndHorizontal();

        if (editor.editData == null)
        {
            EditorGUILayout.HelpBox("Assign or create a Level Data asset.", MessageType.Info);
            return;
        }

        var levelData = editor.editData;
        levelData.gridRows = LevelEditor.GridRows;
        levelData.gridCols = LevelEditor.GridCols;

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
        levelData.levelNumber = EditorGUILayout.IntField("Level Number", levelData.levelNumber);
        levelData.timerDuration = EditorGUILayout.FloatField("Timer (seconds)", levelData.timerDuration);
        levelData.busStopSlotCount = EditorGUILayout.IntField("Bus Stop Slots", levelData.busStopSlotCount);

        EditorGUILayout.Space(10);

        // Color picker
        EditorGUILayout.LabelField("Brush Color", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        foreach (StickmanColor c in System.Enum.GetValues(typeof(StickmanColor)))
        {
            var btnColor = GetColor(editor, c);
            bool isSelected = editor.selectedColor == c;

            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, btnColor);
            tex.Apply();

            var style = new GUIStyle(GUI.skin.button);
            style.normal.background = tex;
            style.hover.background = tex;
            style.active.background = tex;
            style.normal.textColor = Color.black;
            style.hover.textColor = Color.black;
            style.active.textColor = Color.black;
            style.fontStyle = FontStyle.Bold;
            style.border = new RectOffset(0, 0, 0, 0);

            string label = isSelected && !editor.pathMode ? $"[{c}]" : c.ToString();
            if (GUILayout.Button(label, style, GUILayout.Height(30)))
            {
                editor.selectedColor = c;
                editor.pathMode = false;
            }
        }
        EditorGUILayout.EndHorizontal();

        // Path mode toggle
        var pathBg = GUI.backgroundColor;
        GUI.backgroundColor = editor.pathMode ? Color.cyan : Color.grey;
        if (GUILayout.Button(editor.pathMode ? "[Path Mode]" : "Path Mode", GUILayout.Height(25)))
            editor.pathMode = !editor.pathMode;
        GUI.backgroundColor = pathBg;

        EditorGUILayout.Space(5);

        // Grid controls
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill All"))
            FillAll(editor, levelData);
        if (GUILayout.Button("Clear All"))
            ClearAll(editor, levelData);
        if (GUILayout.Button("Refresh Scene"))
            editor.RebuildScene();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        DrawGrid(editor, levelData);

        EditorGUILayout.Space(15);

        DrawBusSequence(levelData, editor);

        EditorGUILayout.Space(10);
        DrawValidation(levelData, editor);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(levelData);
            EditorUtility.SetDirty(editor);
            SceneView.RepaintAll();
        }
    }

    private void DrawValidation(LevelData levelData, LevelEditor editor)
    {
        int stickmanCount = levelData.stickmanPlacements != null ? levelData.stickmanPlacements.Length : 0;
        int busCount = levelData.busSequence != null ? levelData.busSequence.Length : 0;
        int totalBusCapacity = 0;

        if (levelData.busSequence != null)
            for (int i = 0; i < levelData.busSequence.Length; i++)
                totalBusCapacity += levelData.busSequence[i].capacity;

        // Count stickmen per color
        var colorCounts = new Dictionary<StickmanColor, int>();
        if (levelData.stickmanPlacements != null)
        {
            for (int i = 0; i < levelData.stickmanPlacements.Length; i++)
            {
                var c = levelData.stickmanPlacements[i].color;
                if (!colorCounts.ContainsKey(c)) colorCounts[c] = 0;
                colorCounts[c]++;
            }
        }

        // Count bus capacity per color
        var busCapacityPerColor = new Dictionary<StickmanColor, int>();
        if (levelData.busSequence != null)
        {
            for (int i = 0; i < levelData.busSequence.Length; i++)
            {
                var c = levelData.busSequence[i].color;
                if (!busCapacityPerColor.ContainsKey(c)) busCapacityPerColor[c] = 0;
                busCapacityPerColor[c] += levelData.busSequence[i].capacity;
            }
        }

        // Warnings
        if (stickmanCount == 0)
            EditorGUILayout.HelpBox("No stickmen placed.", MessageType.Warning);

        if (busCount == 0 && stickmanCount > 0)
            EditorGUILayout.HelpBox("No buses! Add buses to match stickmen.", MessageType.Error);

        if (stickmanCount > totalBusCapacity && busCount > 0)
            EditorGUILayout.HelpBox($"Too many stickmen ({stickmanCount}) for bus capacity ({totalBusCapacity}). Add more buses.", MessageType.Error);

        if (totalBusCapacity > stickmanCount && busCount > 0)
            EditorGUILayout.HelpBox($"Too many bus seats ({totalBusCapacity}) for {stickmanCount} stickmen. Remove some buses.", MessageType.Warning);

        // Per-color mismatch
        foreach (var kvp in colorCounts)
        {
            busCapacityPerColor.TryGetValue(kvp.Key, out int cap);
            if (cap == 0)
                EditorGUILayout.HelpBox($"{kvp.Key}: {kvp.Value} stickmen but no bus for this color!", MessageType.Error);
            else if (kvp.Value > cap)
                EditorGUILayout.HelpBox($"{kvp.Key}: {kvp.Value} stickmen but only {cap} bus seats.", MessageType.Error);
            else if (kvp.Value < cap)
                EditorGUILayout.HelpBox($"{kvp.Key}: {kvp.Value} stickmen but {cap} bus seats (extra seats).", MessageType.Warning);
        }

        foreach (var kvp in busCapacityPerColor)
        {
            if (!colorCounts.ContainsKey(kvp.Key))
                EditorGUILayout.HelpBox($"{kvp.Key}: bus exists but no stickmen of this color!", MessageType.Warning);
        }

        // Path validation — ignore other stickmen since they'll move during gameplay
        if (stickmanCount > 0)
        {
            PathFinder.BuildGrids(levelData, out var walkable, out _);
            int rows = levelData.gridRows;
            int cols = levelData.gridCols;
            var empty = new bool[rows, cols]; // no occupied cells — check walkable connectivity only
            var blocked = new List<string>();

            for (int i = 0; i < levelData.stickmanPlacements.Length; i++)
            {
                var p = levelData.stickmanPlacements[i];
                if (!PathFinder.HasPathToTop(walkable, empty, p.row, p.col, rows, cols))
                    blocked.Add($"{p.color} at [{p.row},{p.col}]");
            }

            if (blocked.Count > 0)
                EditorGUILayout.HelpBox($"Isolated stickmen (no walkable path to top):\n{string.Join("\n", blocked)}", MessageType.Error);
            else
                EditorGUILayout.HelpBox("All stickmen can reach the top row.", MessageType.Info);
        }

        // All good check
        bool isValid = stickmanCount > 0 && busCount > 0 && stickmanCount == totalBusCapacity;

        if (isValid)
        {
            foreach (var kvp in colorCounts)
            {
                busCapacityPerColor.TryGetValue(kvp.Key, out int cap);
                if (kvp.Value != cap) { isValid = false; break; }
            }
        }

        if (isValid)
        {
            PathFinder.BuildGrids(levelData, out var w, out _);
            var empty = new bool[levelData.gridRows, levelData.gridCols];
            for (int i = 0; i < levelData.stickmanPlacements.Length; i++)
            {
                var p = levelData.stickmanPlacements[i];
                if (!PathFinder.HasPathToTop(w, empty, p.row, p.col, levelData.gridRows, levelData.gridCols))
                { isValid = false; break; }
            }
        }

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

    private void DrawGrid(LevelEditor editor, LevelData levelData)
    {
        EditorGUILayout.LabelField("Grid (click to place, click same to remove)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
        GUILayout.Space(27);
        for (int c = 0; c < LevelEditor.GridCols; c++)
        {
            var rect = GUILayoutUtility.GetRect(30, 15, GUILayout.Width(30), GUILayout.ExpandWidth(false));
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(rect, c.ToString(), style);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        for (int r = 0; r < LevelEditor.GridRows; r++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(r.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(25), GUILayout.Height(30), GUILayout.ExpandWidth(false));

            for (int c = 0; c < LevelEditor.GridCols; c++)
            {
                var existing = levelData.GetColorAt(r, c);
                bool isPath = levelData.IsCellActive(r, c);
                var rect = GUILayoutUtility.GetRect(30, 30, GUILayout.Width(30));

                Color cellColor;
                if (existing.HasValue)
                    cellColor = GetColor(editor, existing.Value);
                else if (isPath)
                    cellColor = new Color(0.6f, 0.6f, 0.6f);
                else
                    cellColor = new Color(0.25f, 0.25f, 0.25f);

                EditorGUI.DrawRect(rect, cellColor);

                // Border
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.black);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Color.black);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), Color.black);
                EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), Color.black);

                string label = existing.HasValue ? existing.Value.ToString()[0].ToString() : (isPath ? "·" : "");
                var style = new GUIStyle(EditorStyles.miniLabel);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = Color.white;
                style.fontSize = isPath && !existing.HasValue ? 16 : 10;
                GUI.Label(rect, label, style);

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    Undo.RecordObject(levelData, "Edit Grid Cell");

                    if (editor.pathMode)
                    {
                        bool wasActive = levelData.IsCellActive(r, c);
                        editor.TogglePathCell(r, c);

                        if (wasActive && existing.HasValue)
                        {
                            RemovePlacement(levelData, r, c);
                            editor.RemoveVisual(r, c);
                        }
                    }
                    else if (existing.HasValue && existing.Value == editor.selectedColor)
                    {
                        RemovePlacement(levelData, r, c);
                        editor.RemoveVisual(r, c);
                    }
                    else
                    {
                        SetPlacement(levelData, r, c, editor.selectedColor);
                        editor.SpawnVisual(r, c, editor.selectedColor);
                    }

                    EditorUtility.SetDirty(levelData);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawBusSequence(LevelData levelData, LevelEditor editor)
    {
        EditorGUILayout.LabelField("Bus Sequence", EditorStyles.boldLabel);

        if (levelData.busSequence == null)
            levelData.busSequence = new BusDefinition[0];

        for (int i = 0; i < levelData.busSequence.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Bus {i + 1}", GUILayout.Width(45));

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = GetColor(editor, levelData.busSequence[i].color);
            var prevColor = levelData.busSequence[i].color;
            levelData.busSequence[i].color = (StickmanColor)EditorGUILayout.EnumPopup(
                levelData.busSequence[i].color, GUILayout.Width(80));
            GUI.backgroundColor = prevBg;

            if (levelData.busSequence[i].color != prevColor)
                editor.RebuildBuses();

            levelData.busSequence[i].capacity = 3;
            EditorGUILayout.LabelField("x3", EditorStyles.boldLabel, GUILayout.Width(25));

            if (GUILayout.Button("▲", GUILayout.Width(22)) && i > 0)
            {
                (levelData.busSequence[i], levelData.busSequence[i - 1]) =
                    (levelData.busSequence[i - 1], levelData.busSequence[i]);
                editor.RebuildBuses();
            }

            if (GUILayout.Button("▼", GUILayout.Width(22)) && i < levelData.busSequence.Length - 1)
            {
                (levelData.busSequence[i], levelData.busSequence[i + 1]) =
                    (levelData.busSequence[i + 1], levelData.busSequence[i]);
                editor.RebuildBuses();
            }

            if (GUILayout.Button("X", GUILayout.Width(22)))
            {
                var list = new List<BusDefinition>(levelData.busSequence);
                list.RemoveAt(i);
                levelData.busSequence = list.ToArray();
                editor.RebuildBuses();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Add Bus"))
        {
            var list = new List<BusDefinition>(levelData.busSequence);
            list.Add(new BusDefinition { color = StickmanColor.Red, capacity = 3 });
            levelData.busSequence = list.ToArray();
            editor.RebuildBuses();
        }
    }

    private void SetPlacement(LevelData levelData, int row, int col, StickmanColor color)
    {
        var list = new List<StickmanPlacement>(levelData.stickmanPlacements ?? new StickmanPlacement[0]);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].row == row && list[i].col == col)
            {
                list[i] = new StickmanPlacement { row = row, col = col, color = color };
                levelData.stickmanPlacements = list.ToArray();
                return;
            }
        }
        list.Add(new StickmanPlacement { row = row, col = col, color = color });
        levelData.stickmanPlacements = list.ToArray();
    }

    private void RemovePlacement(LevelData levelData, int row, int col)
    {
        var list = new List<StickmanPlacement>(levelData.stickmanPlacements ?? new StickmanPlacement[0]);
        list.RemoveAll(p => p.row == row && p.col == col);
        levelData.stickmanPlacements = list.ToArray();
    }

    private void FillAll(LevelEditor editor, LevelData levelData)
    {
        Undo.RecordObject(levelData, "Fill All");
        var list = new List<StickmanPlacement>();
        for (int r = 0; r < LevelEditor.GridRows; r++)
            for (int c = 0; c < LevelEditor.GridCols; c++)
                list.Add(new StickmanPlacement { row = r, col = c, color = editor.selectedColor });
        levelData.stickmanPlacements = list.ToArray();
        EditorUtility.SetDirty(levelData);
        editor.RebuildScene();
    }

    private void ClearAll(LevelEditor editor, LevelData levelData)
    {
        Undo.RecordObject(levelData, "Clear All");
        levelData.stickmanPlacements = new StickmanPlacement[0];
        EditorUtility.SetDirty(levelData);
        editor.ClearScene();
    }

    private void CreateNewLevel(LevelEditor editor)
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
