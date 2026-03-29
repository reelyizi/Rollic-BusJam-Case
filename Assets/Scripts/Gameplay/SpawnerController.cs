using System.Collections.Generic;
using UnityEngine;

public class SpawnerController : MonoBehaviour
{
    private Queue<StickmanColor> colorQueue;
    private int targetRow;
    private int targetCol;
    private GridManager gridManager;
    private ColorConfig colorConfig;
    private float checkInterval = 0.2f;
    private float timer;

    public bool IsExhausted => colorQueue == null || colorQueue.Count == 0;
    public int RemainingCount => colorQueue != null ? colorQueue.Count : 0;

    public void Initialize(SpawnerPlacement placement, GridManager gm, ColorConfig config)
    {
        gridManager = gm;
        colorConfig = config;

        var offset = LevelData.GetDirectionOffset(placement.direction);
        targetRow = placement.row + offset.x;
        targetCol = placement.col + offset.y;

        colorQueue = new Queue<StickmanColor>();
        if (placement.colorQueue != null)
        {
            for (int i = 0; i < placement.colorQueue.Length; i++)
                colorQueue.Enqueue(placement.colorQueue[i]);
        }
    }

    private void Update()
    {
        if (IsExhausted || gridManager == null) return;

        timer += Time.deltaTime;
        if (timer < checkInterval) return;

        timer = 0f;

        if (gridManager.IsCellEmpty(targetRow, targetCol))
        {
            var color = colorQueue.Dequeue();
            gridManager.SpawnStickmanAt(targetRow, targetCol, color);
            gridManager.RefreshAllPaths();
        }
    }
}
