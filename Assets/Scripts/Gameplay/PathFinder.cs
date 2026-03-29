using System.Collections.Generic;
using UnityEngine;

public static class PathFinder
{
    private static readonly Vector2Int[] Directions =
    {
        new(0, -1),  // left
        new(0, 1),   // right
        new(-1, 0),  // up
        new(1, 0)    // down
    };

    public static bool HasPathToTop(bool[,] walkable, bool[,] occupied, int startRow, int startCol, int rows, int cols)
    {
        if (startRow == 0) return true;

        var visited = new bool[rows, cols];
        var queue = new Queue<Vector2Int>();

        visited[startRow, startCol] = true;
        queue.Enqueue(new Vector2Int(startRow, startCol));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            for (int d = 0; d < Directions.Length; d++)
            {
                int newRow = current.x + Directions[d].x;
                int newCol = current.y + Directions[d].y;

                if (newRow < 0 || newRow >= rows || newCol < 0 || newCol >= cols) continue;
                if (visited[newRow, newCol]) continue;
                if (!walkable[newRow, newCol]) continue;
                if (occupied[newRow, newCol]) continue;

                if (newRow == 0) return true;

                visited[newRow, newCol] = true;
                queue.Enqueue(new Vector2Int(newRow, newCol));
            }
        }

        return false;
    }

    public static List<Vector2Int> FindPathToTop(bool[,] walkable, bool[,] occupied, int startRow, int startCol, int rows, int cols)
    {
        if (startRow == 0) return new List<Vector2Int> { new(startRow, startCol) };

        var visited = new bool[rows, cols];
        var parent = new Vector2Int[rows, cols];
        var queue = new Queue<Vector2Int>();

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                parent[r, c] = new Vector2Int(-1, -1);

        visited[startRow, startCol] = true;
        queue.Enqueue(new Vector2Int(startRow, startCol));

        Vector2Int end = new(-1, -1);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            for (int d = 0; d < Directions.Length; d++)
            {
                int newRow = current.x + Directions[d].x;
                int newCol = current.y + Directions[d].y;

                if (newRow < 0 || newRow >= rows || newCol < 0 || newCol >= cols) continue;
                if (visited[newRow, newCol]) continue;
                if (!walkable[newRow, newCol]) continue;
                if (occupied[newRow, newCol]) continue;

                visited[newRow, newCol] = true;
                parent[newRow, newCol] = current;

                if (newRow == 0)
                {
                    end = new Vector2Int(newRow, newCol);
                    queue.Clear();
                    break;
                }

                queue.Enqueue(new Vector2Int(newRow, newCol));
            }
        }

        if (end.x == -1) return null;

        var path = new List<Vector2Int>();
        var step = end;
        while (step.x != -1)
        {
            path.Add(step);
            step = parent[step.x, step.y];
        }

        path.Reverse();
        return path;
    }

    public static void BuildGrids(LevelData levelData, out bool[,] walkable, out bool[,] occupied)
    {
        int rows = levelData.gridRows;
        int cols = levelData.gridCols;
        walkable = new bool[rows, cols];
        occupied = new bool[rows, cols];

        if (levelData.activeCells != null)
        {
            for (int i = 0; i < levelData.activeCells.Length; i++)
            {
                var cell = levelData.activeCells[i];
                if (cell.row >= 0 && cell.row < rows && cell.col >= 0 && cell.col < cols)
                    walkable[cell.row, cell.col] = true;
            }
        }

        if (levelData.stickmanPlacements != null)
        {
            for (int i = 0; i < levelData.stickmanPlacements.Length; i++)
            {
                var p = levelData.stickmanPlacements[i];
                if (p.row >= 0 && p.row < rows && p.col >= 0 && p.col < cols)
                {
                    walkable[p.row, p.col] = true;
                    occupied[p.row, p.col] = true;
                }
            }
        }
    }
}
