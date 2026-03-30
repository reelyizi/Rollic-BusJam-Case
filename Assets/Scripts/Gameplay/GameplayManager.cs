using System.Collections.Generic;
using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    private enum PlayState { PreGame, Playing, Won, Lost }

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BusStop busStop;
    [SerializeField] private BusManager busManager;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private TimerDisplay timerDisplay;
    [SerializeField] private ColorConfig colorConfig;
    [SerializeField] private float winDelay = 2f;

    [Header("UI")]
    [SerializeField] private GameObject tapToStartOverlay;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private PlayState state;
    private readonly List<Stickman> movingStickmen = new();
    private LevelData levelData;

    private void Start()
    {
        int levelIndex = PlayerData.CurrentLevel;
        int levelNumber = levelIndex + 1;
        string path = $"LevelData/Level_{levelNumber:D2}";
        levelData = Resources.Load<LevelData>(path);

        if (levelData == null)
        {
            PlayerData.CurrentLevel = 0;
            levelData = Resources.Load<LevelData>("LevelData/Level_01");

            if (levelData == null)
            {
                Debug.LogError("[GameplayManager] No levels found!");
                return;
            }
        }

        gridManager.Initialize(levelData, colorConfig);
        busStop.Initialize(levelData.busStopSlotCount);
        busManager.Initialize(levelData.busSequence, busStop);
        busManager.SpawnAllBuses();

        if (timerDisplay != null)
            timerDisplay.SetTime(levelData.timerDuration);

        gridManager.RefreshAllPaths();

        inputHandler.OnStickmanTapped += HandleStickmanTapped;
        busManager.OnAllBusesComplete += CheckWinCondition;
        busManager.OnSlotFreed += ProcessWaitingStickmen;
        inputHandler.SetEnabled(false);

        state = PlayState.PreGame;

        if (tapToStartOverlay != null) tapToStartOverlay.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private void Update()
    {
        if (state == PlayState.PreGame && Input.GetMouseButtonDown(0))
        {
            StartGame();
            return;
        }

        if (state == PlayState.Playing)
        {
            if (timerDisplay != null && timerDisplay.IsTimeUp)
                OnLose();

            if (busStop.IsFull && movingStickmen.Count == 0 && !busManager.IsDriving)
                OnLose();
        }
    }

    private void StartGame()
    {
        state = PlayState.Playing;
        inputHandler.SetEnabled(true);

        if (timerDisplay != null)
            timerDisplay.StartTimer();

        if (tapToStartOverlay != null) tapToStartOverlay.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Gameplay);
    }

    private void HandleStickmanTapped(Stickman stickman)
    {
        if (state != PlayState.Playing) return;
        if (movingStickmen.Contains(stickman)) return;

        var occupied = gridManager.GetOccupiedGrid();
        occupied[stickman.GridRow, stickman.GridCol] = false;

        var path = PathFinder.FindPathToTop(gridManager.GetWalkableGrid(), occupied,
            stickman.GridRow, stickman.GridCol, gridManager.Rows, gridManager.Cols);

        if (path == null)
        {
            occupied[stickman.GridRow, stickman.GridCol] = true;
            return;
        }

        gridManager.ClearCell(stickman.GridRow, stickman.GridCol);
        stickman.SetHasPath(false);
        movingStickmen.Add(stickman);
        gridManager.RefreshAllPaths();

        var worldPath = new List<Vector3>();
        for (int i = 0; i < path.Count; i++)
            worldPath.Add(gridManager.GridToWorldPosition(path[i].x, path[i].y));

        stickman.MoveAlongPath(worldPath, () => OnReachedTopRow(stickman));
    }

    private void OnReachedTopRow(Stickman stickman)
    {
        int slotIndex = busStop.GetFirstEmptySlotIndex();

        if (slotIndex == -1)
        {
            waitingAtTop.Add(stickman);
            return;
        }

        MoveToSlot(stickman, slotIndex);
    }

    private void MoveToSlot(Stickman stickman, int slotIndex)
    {
        Vector3 slotPos = busStop.GetSlotPosition(slotIndex);
        busStop.AssignToSlot(slotIndex, stickman);

        stickman.MoveAlongPath(new List<Vector3> { slotPos }, () =>
        {
            movingStickmen.Remove(stickman);
            busManager.OnPassengerArrived(stickman);
            ProcessWaitingStickmen();
            gridManager.RefreshAllPaths();
            CheckWinCondition();
        });
    }

    private readonly List<Stickman> waitingAtTop = new();

    private void ProcessWaitingStickmen()
    {
        for (int i = waitingAtTop.Count - 1; i >= 0; i--)
        {
            int slotIndex = busStop.GetFirstEmptySlotIndex();
            if (slotIndex == -1) break;

            var stickman = waitingAtTop[i];
            waitingAtTop.RemoveAt(i);
            MoveToSlot(stickman, slotIndex);
        }
    }

    private void CheckWinCondition()
    {
        if (gridManager.StickmanCount() == 0 && busStop.IsEmpty() && gridManager.AllSpawnersExhausted())
            OnWin();
    }

    private void OnWin()
    {
        state = PlayState.Won;
        inputHandler.SetEnabled(false);

        if (timerDisplay != null)
            timerDisplay.StopTimer();

        PlayerData.CurrentLevel++;

        if (winPanel != null) winPanel.SetActive(true);
    }

    private void OnLose()
    {
        if (state == PlayState.Lost) return;

        state = PlayState.Lost;
        inputHandler.SetEnabled(false);

        if (timerDisplay != null)
            timerDisplay.StopTimer();

        if (losePanel != null) losePanel.SetActive(true);
    }

    public void ContinueToMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMenuScreen();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScreen");
    }

    public void RestartLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (inputHandler != null)
            inputHandler.OnStickmanTapped -= HandleStickmanTapped;
    }
}
