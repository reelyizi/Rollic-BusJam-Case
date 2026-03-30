using UnityEngine;

public static class PlayerData
{
    private const string LevelKey = "CurrentLevel";

    public static int CurrentLevel
    {
        get => PlayerPrefs.GetInt(LevelKey, 0);
        set
        {
            PlayerPrefs.SetInt(LevelKey, value);
            PlayerPrefs.Save();
        }
    }
}
