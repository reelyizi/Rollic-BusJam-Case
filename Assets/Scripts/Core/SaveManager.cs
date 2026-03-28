using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    public PlayerData Data { get; private set; }

    [SerializeField] private GameConfig gameConfig;

    private float saveInterval = 10f;
    private float saveTimer;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        Data = PlayerData.Load(gameConfig);
    }

    private void Update()
    {
        saveTimer += Time.unscaledDeltaTime;
        if (saveTimer >= saveInterval)
        {
            saveTimer = 0f;
            Data.Save();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            Data.Save();
    }

    private void OnApplicationQuit()
    {
        Data.Save();
    }

    public void ForceSave()
    {
        Data.Save();
    }

    public GameConfig GetConfig()
    {
        return gameConfig;
    }
}
