using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelEditorBusDrawer
{
    public void Draw(LevelEditor editor, LevelData levelData)
    {
        EditorGUILayout.LabelField("Bus Sequence", EditorStyles.boldLabel);

        if (levelData.busSequence == null)
            levelData.busSequence = new BusDefinition[0];

        for (int i = 0; i < levelData.busSequence.Length; i++)
        {
            if (DrawBusEntry(editor, levelData, i))
                break;
        }

        if (GUILayout.Button("+ Add Bus"))
        {
            var list = new List<BusDefinition>(levelData.busSequence);
            list.Add(new BusDefinition { color = StickmanColor.Red, capacity = 3 });
            levelData.busSequence = list.ToArray();
            editor.Visuals.RebuildBuses();
        }
    }

    private bool DrawBusEntry(LevelEditor editor, LevelData levelData, int i)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Bus {i + 1}", GUILayout.Width(45));

        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = LevelEditorUtils.GetColor(editor, levelData.busSequence[i].color);
        var prevColor = levelData.busSequence[i].color;
        levelData.busSequence[i].color = (StickmanColor)EditorGUILayout.EnumPopup(
            levelData.busSequence[i].color, GUILayout.Width(80));
        GUI.backgroundColor = prevBg;

        if (levelData.busSequence[i].color != prevColor)
            editor.Visuals.RebuildBuses();

        levelData.busSequence[i].capacity = 3;
        EditorGUILayout.LabelField("x3", EditorStyles.boldLabel, GUILayout.Width(25));

        EditorGUILayout.LabelField("R:", GUILayout.Width(15));
        int prevReserved = levelData.busSequence[i].reservedSeats;
        levelData.busSequence[i].reservedSeats = EditorGUILayout.IntField(
            levelData.busSequence[i].reservedSeats, GUILayout.Width(25));
        levelData.busSequence[i].reservedSeats = Mathf.Clamp(levelData.busSequence[i].reservedSeats, 0, 3);
        if (levelData.busSequence[i].reservedSeats != prevReserved)
            editor.Visuals.RebuildBuses();

        if (GUILayout.Button("\u25b2", GUILayout.Width(22)) && i > 0)
        {
            (levelData.busSequence[i], levelData.busSequence[i - 1]) =
                (levelData.busSequence[i - 1], levelData.busSequence[i]);
            editor.Visuals.RebuildBuses();
        }

        if (GUILayout.Button("\u25bc", GUILayout.Width(22)) && i < levelData.busSequence.Length - 1)
        {
            (levelData.busSequence[i], levelData.busSequence[i + 1]) =
                (levelData.busSequence[i + 1], levelData.busSequence[i]);
            editor.Visuals.RebuildBuses();
        }

        bool removed = false;
        if (GUILayout.Button("X", GUILayout.Width(22)))
        {
            var list = new List<BusDefinition>(levelData.busSequence);
            list.RemoveAt(i);
            levelData.busSequence = list.ToArray();
            editor.Visuals.RebuildBuses();
            removed = true;
        }

        EditorGUILayout.EndHorizontal();
        return removed;
    }
}
