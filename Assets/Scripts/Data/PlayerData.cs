using System;
using UnityEngine;

[Serializable]
public class PlayerData
{
    public int coins;
    public int currentLevel;
    public float timerRemaining;
    public long lastSaveTimeTicks;
    public bool soundEnabled = true;
    public bool musicEnabled = true;

    private const string SaveKey = "BusJam_SaveData";

    public static PlayerData Load(GameConfig config)
    {
        if (PlayerPrefs.HasKey(SaveKey))
        {
            string json = PlayerPrefs.GetString(SaveKey);
            var data = JsonUtility.FromJson<PlayerData>(json);

            if (data.lastSaveTimeTicks > 0)
            {
                long elapsed = DateTime.UtcNow.Ticks - data.lastSaveTimeTicks;
                float elapsedSeconds = (float)TimeSpan.FromTicks(elapsed).TotalSeconds;
                data.timerRemaining = Mathf.Max(0f, data.timerRemaining - elapsedSeconds);
            }

            return data;
        }

        return CreateDefault(config);
    }

    public void Save()
    {
        lastSaveTimeTicks = DateTime.UtcNow.Ticks;
        string json = JsonUtility.ToJson(this);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public static PlayerData CreateDefault(GameConfig config)
    {
        return new PlayerData
        {
            coins = config.startingCoins,
            currentLevel = config.startingLevel,
            timerRemaining = 0f,
            lastSaveTimeTicks = DateTime.UtcNow.Ticks,
            soundEnabled = true,
            musicEnabled = true
        };
    }
}
