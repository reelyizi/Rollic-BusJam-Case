public static class LevelDataCopier
{
    public static void Copy(LevelData from, LevelData to)
    {
        to.levelNumber = from.levelNumber;
        to.gridRows = from.gridRows;
        to.gridCols = from.gridCols;
        to.timerDuration = from.timerDuration;
        to.busStopSlotCount = from.busStopSlotCount;

        to.stickmanPlacements = CloneArray(from.stickmanPlacements);
        to.busSequence = CloneArray(from.busSequence);
        to.activeCells = CloneArray(from.activeCells);

        if (from.spawnerPlacements != null)
        {
            to.spawnerPlacements = new SpawnerPlacement[from.spawnerPlacements.Length];
            for (int i = 0; i < from.spawnerPlacements.Length; i++)
            {
                to.spawnerPlacements[i] = from.spawnerPlacements[i];
                if (from.spawnerPlacements[i].colorQueue != null)
                    to.spawnerPlacements[i].colorQueue = (StickmanColor[])from.spawnerPlacements[i].colorQueue.Clone();
            }
        }
        else to.spawnerPlacements = null;
    }

    private static T[] CloneArray<T>(T[] source)
    {
        if (source == null) return null;
        var copy = new T[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }
}
