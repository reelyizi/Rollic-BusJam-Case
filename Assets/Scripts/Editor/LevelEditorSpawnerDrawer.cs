using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelEditorSpawnerDrawer
{
    public void DrawSelectedSpawner(LevelEditor editor, LevelData levelData)
    {
        var sel = editor.selectedSpawnerCell;
        if (sel.x < 0 || levelData == null) return;

        var spawner = levelData.GetSpawnerAt(sel.x, sel.y);
        if (!spawner.HasValue)
        {
            editor.selectedSpawnerCell = new Vector2Int(-1, -1);
            return;
        }

        var sp = spawner.Value;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Spawner [{sel.x},{sel.y}]", EditorStyles.boldLabel);

        var newDir = (SpawnerDirection)EditorGUILayout.EnumPopup("Direction", sp.direction);
        bool changed = newDir != sp.direction;
        sp.direction = newDir;

        DrawQueue(editor, ref sp, ref changed);
        DrawQueueAddButtons(editor, ref sp, ref changed);
        DrawActions(editor, ref sp, sel, ref changed);

        if (changed)
        {
            editor.Visuals.UpdateSpawner(sel.x, sel.y, sp.direction, sp.colorQueue);
            EditorUtility.SetDirty(levelData);
        }
    }

    private void DrawQueue(LevelEditor editor, ref SpawnerPlacement sp, ref bool changed)
    {
        EditorGUILayout.LabelField("Stickman Queue:");
        var queue = new List<StickmanColor>(sp.colorQueue ?? new StickmanColor[0]);

        EditorGUILayout.BeginHorizontal();
        for (int q = 0; q < queue.Count; q++)
        {
            var qColor = LevelEditorUtils.GetColor(editor, queue[q]);
            var qRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24));

            EditorGUI.DrawRect(qRect, qColor);
            EditorGUI.DrawRect(new Rect(qRect.x, qRect.y, qRect.width, 1), Color.black);
            EditorGUI.DrawRect(new Rect(qRect.x, qRect.yMax - 1, qRect.width, 1), Color.black);
            EditorGUI.DrawRect(new Rect(qRect.x, qRect.y, 1, qRect.height), Color.black);
            EditorGUI.DrawRect(new Rect(qRect.xMax - 1, qRect.y, 1, qRect.height), Color.black);

            var xStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (GUI.Button(qRect, "x", xStyle))
            {
                queue.RemoveAt(q);
                sp.colorQueue = queue.ToArray();
                changed = true;
                break;
            }
        }

        if (queue.Count == 0)
            EditorGUILayout.LabelField("(empty)", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawQueueAddButtons(LevelEditor editor, ref SpawnerPlacement sp, ref bool changed)
    {
        var queue = new List<StickmanColor>(sp.colorQueue ?? new StickmanColor[0]);

        EditorGUILayout.BeginHorizontal();
        foreach (StickmanColor c in System.Enum.GetValues(typeof(StickmanColor)))
        {
            var btnColor = LevelEditorUtils.GetColor(editor, c);
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, btnColor);
            tex.Apply();

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = tex, textColor = Color.black },
                hover = { background = tex, textColor = Color.black },
                active = { background = tex, textColor = Color.black },
                fontStyle = FontStyle.Bold,
                border = new RectOffset(0, 0, 0, 0)
            };

            if (GUILayout.Button("+", btnStyle, GUILayout.Height(22), GUILayout.Width(30)))
            {
                queue.Add(c);
                sp.colorQueue = queue.ToArray();
                changed = true;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawActions(LevelEditor editor, ref SpawnerPlacement sp, Vector2Int sel, ref bool changed)
    {
        var queue = new List<StickmanColor>(sp.colorQueue ?? new StickmanColor[0]);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Queue"))
        {
            sp.colorQueue = new StickmanColor[0];
            changed = true;
        }

        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        if (GUILayout.Button("Delete Spawner", GUILayout.Width(120)))
        {
            editor.Visuals.DeleteSpawner(sel.x, sel.y);
            editor.selectedSpawnerCell = new Vector2Int(-1, -1);
            GUI.backgroundColor = prevBg;
            EditorGUILayout.EndHorizontal();
            return;
        }
        GUI.backgroundColor = prevBg;
        EditorGUILayout.EndHorizontal();
    }
}
