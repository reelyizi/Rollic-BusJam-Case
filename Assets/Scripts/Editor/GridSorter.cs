using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class GridSorter
{
    [MenuItem("BusJam/Sort Grid Children")]
    public static void SortSelectedGridChildren()
    {
        var selected = Selection.activeTransform;
        if (selected == null || selected.childCount == 0)
        {
            Debug.LogWarning("[GridSorter] Select the grid parent object first.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(selected.gameObject, "Sort Grid Children");

        var children = new List<Transform>();
        for (int i = 0; i < selected.childCount; i++)
            children.Add(selected.GetChild(i));

        // Top-left first: highest Z first, then lowest X
        var sorted = children
            .OrderByDescending(t => Mathf.Round(t.position.z * 100f) / 100f)
            .ThenBy(t => Mathf.Round(t.position.x * 100f) / 100f)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
            sorted[i].SetSiblingIndex(i);

        EditorUtility.SetDirty(selected.gameObject);
    }
}
