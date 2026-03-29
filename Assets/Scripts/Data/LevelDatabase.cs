using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "BusJam/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public LevelData[] levels;

    public LevelData GetLevel(int levelNumber)
    {
        if (levelNumber < 0 || levelNumber >= levels.Length)
            return null;

        return levels[levelNumber];
    }

    public int LevelCount => levels != null ? levels.Length : 0;
}
