using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelEditor))]
public class LevelEditorInspector : Editor
{
    private LevelEditorGridDrawer gridDrawer;
    private LevelEditorBusDrawer busDrawer;
    private LevelEditorSpawnerDrawer spawnerDrawer;
    private LevelEditorValidation validation;

    private void OnEnable()
    {
        gridDrawer = new LevelEditorGridDrawer();
        busDrawer = new LevelEditorBusDrawer();
        spawnerDrawer = new LevelEditorSpawnerDrawer();
        validation = new LevelEditorValidation();
    }

    public override void OnInspectorGUI()
    {
        var editor = (LevelEditor)target;

        DrawSetup(editor);

        EditorGUILayout.Space(10);

        if (!DrawLevelSelector(editor))
            return;

        var levelData = editor.editData;
        levelData.gridRows = LevelEditor.GridRows;
        levelData.gridCols = LevelEditor.GridCols;

        EditorGUILayout.Space(10);

        DrawLevelSettings(levelData);

        EditorGUILayout.Space(10);

        DrawModeSelector(editor);

        EditorGUILayout.Space(5);

        DrawGridControls(editor, levelData);

        EditorGUILayout.Space(5);

        gridDrawer.Draw(editor, levelData);

        spawnerDrawer.DrawSelectedSpawner(editor, levelData);

        EditorGUILayout.Space(15);

        busDrawer.Draw(editor, levelData);

        EditorGUILayout.Space(10);

        validation.Draw(editor, levelData);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(levelData);
            EditorUtility.SetDirty(editor);
            SceneView.RepaintAll();
        }
    }

    private void DrawSetup(LevelEditor editor)
    {
        EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
        editor.colorConfig = (ColorConfig)EditorGUILayout.ObjectField("Color Config", editor.colorConfig, typeof(ColorConfig), false);
        editor.gameConfig = (GameConfig)EditorGUILayout.ObjectField("Game Config", editor.gameConfig, typeof(GameConfig), false);
        editor.stickmanPrefab = (GameObject)EditorGUILayout.ObjectField("Stickman Prefab", editor.stickmanPrefab, typeof(GameObject), false);
        editor.busPrefab = (GameObject)EditorGUILayout.ObjectField("Bus Prefab", editor.busPrefab, typeof(GameObject), false);
        editor.wallPrefab = (GameObject)EditorGUILayout.ObjectField("Wall Prefab", editor.wallPrefab, typeof(GameObject), false);
        editor.spawnerPrefab = (GameObject)EditorGUILayout.ObjectField("Spawner Prefab", editor.spawnerPrefab, typeof(GameObject), false);
        editor.gridParent = (Transform)EditorGUILayout.ObjectField("Grid Parent", editor.gridParent, typeof(Transform), true);
        editor.busSpawnOrigin = (Transform)EditorGUILayout.ObjectField("Bus Spawn Origin", editor.busSpawnOrigin, typeof(Transform), true);
    }

    private bool DrawLevelSelector(LevelEditor editor)
    {
        EditorGUILayout.LabelField("Level Data", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        var prevSource = editor.sourceLevel;
        editor.sourceLevel = (LevelData)EditorGUILayout.ObjectField(editor.sourceLevel, typeof(LevelData), false);

        if (editor.sourceLevel != prevSource || (editor.sourceLevel != null && editor.editData == null))
            editor.LoadLevel();

        if (GUILayout.Button("New", GUILayout.Width(50)))
            LevelEditorUtils.CreateNewLevel(editor);

        EditorGUILayout.EndHorizontal();

        if (editor.editData == null)
        {
            EditorGUILayout.HelpBox("Assign or create a Level Data asset.", MessageType.Info);
            return false;
        }
        return true;
    }

    private void DrawLevelSettings(LevelData levelData)
    {
        EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
        levelData.levelNumber = EditorGUILayout.IntField("Level Number", levelData.levelNumber);
        levelData.timerDuration = EditorGUILayout.FloatField("Timer (seconds)", levelData.timerDuration);
        levelData.busStopSlotCount = EditorGUILayout.IntField("Bus Stop Slots", levelData.busStopSlotCount);
    }

    private void DrawModeSelector(LevelEditor editor)
    {
        EditorGUILayout.LabelField("Brush Color", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        foreach (StickmanColor c in System.Enum.GetValues(typeof(StickmanColor)))
        {
            var btnColor = LevelEditorUtils.GetColor(editor, c);
            bool isSelected = editor.selectedColor == c && !editor.pathMode && !editor.spawnerMode;

            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, btnColor);
            tex.Apply();

            var style = new GUIStyle(GUI.skin.button)
            {
                normal = { background = tex, textColor = Color.black },
                hover = { background = tex, textColor = Color.black },
                active = { background = tex, textColor = Color.black },
                fontStyle = FontStyle.Bold,
                border = new RectOffset(0, 0, 0, 0)
            };

            string label = isSelected ? $"[{c}]" : c.ToString();
            if (GUILayout.Button(label, style, GUILayout.Height(30)))
            {
                editor.selectedColor = c;
                editor.pathMode = false;
                editor.spawnerMode = false;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        var prevBg = GUI.backgroundColor;

        GUI.backgroundColor = editor.pathMode ? Color.cyan : Color.grey;
        if (GUILayout.Button(editor.pathMode ? "[Path Mode]" : "Path Mode", GUILayout.Height(25)))
        {
            editor.pathMode = !editor.pathMode;
            if (editor.pathMode) editor.spawnerMode = false;
        }

        GUI.backgroundColor = editor.spawnerMode ? Color.magenta : Color.grey;
        if (GUILayout.Button(editor.spawnerMode ? "[Spawner Mode]" : "Spawner Mode", GUILayout.Height(25)))
        {
            editor.spawnerMode = !editor.spawnerMode;
            if (editor.spawnerMode) editor.pathMode = false;
        }

        GUI.backgroundColor = prevBg;
        EditorGUILayout.EndHorizontal();

        // Hidden mode toggle
        bool prevHidden = editor.hiddenMode;
        editor.hiddenMode = EditorGUILayout.Toggle("Hidden Stickman Mode", editor.hiddenMode);
        if (editor.hiddenMode != prevHidden)
            editor.Visuals.UpdateHiddenStickmen();

        if (editor.spawnerMode)
            EditorGUILayout.HelpBox("Click empty cell to place spawner. Click existing spawner to select it.", MessageType.None);
        if (editor.hiddenMode)
            EditorGUILayout.HelpBox("Placed stickmen will be hidden (black) until their path opens.", MessageType.None);
    }

    private void DrawGridControls(LevelEditor editor, LevelData levelData)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill All"))
        {
            Undo.RecordObject(levelData, "Fill All");
            LevelEditorUtils.FillAll(editor, levelData);
        }
        if (GUILayout.Button("Clear All"))
        {
            Undo.RecordObject(levelData, "Clear All");
            LevelEditorUtils.ClearAll(editor, levelData);
        }
        if (GUILayout.Button("Refresh Scene"))
            editor.Visuals.RebuildScene();
        EditorGUILayout.EndHorizontal();
    }
}
