using System.Collections;
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
    [SerializeField] private LevelDatabase levelDatabase;
    [SerializeField] private ColorConfig colorConfig;

    [Header("UI")]
    [SerializeField] private GameObject tapToStartOverlay;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private PlayState state;
    private LevelData currentLevelData;
    private bool isMoving;

    private void Start()
    {
        int levelIndex = SaveManager.Instance != null ? SaveManager.Instance.Data.currentLevel : 0;
        currentLevelData = levelDatabase.GetLevel(levelIndex);

        if (currentLevelData == null)
        {
            Debug.LogError($"[GameplayManager] Level {levelIndex} not found in database!");
            return;
        }

        gridManager.Initialize(currentLevelData, colorConfig);
        busStop.Initialize(currentLevelData.busStopSlotCount);
        busManager.Initialize(currentLevelData.busSequence);

        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.timerRemaining = currentLevelData.timerDuration;

        gridManager.RefreshAllPaths();

        inputHandler.OnStickmanTapped += HandleStickmanTapped;
        busManager.OnBusReady += HandleBusReady;
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
            if (SaveManager.Instance != null && SaveManager.Instance.Data.timerRemaining <= 0f)
            {
                OnLose();
            }
        }
    }

    private void StartGame()
    {
        state = PlayState.Playing;
        inputHandler.SetEnabled(true);
        busManager.SpawnNextBus();

        if (tapToStartOverlay != null) tapToStartOverlay.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Gameplay);
    }

    private void HandleStickmanTapped(Stickman stickman)
    {
        if (state != PlayState.Playing || isMoving) return;

        if (!busStop.HasEmptySlot())
        {
            OnLose();
            return;
        }

        var occupied = gridManager.GetOccupiedGrid();
        occupied[stickman.GridRow, stickman.GridCol] = false;

        var path = PathFinder.FindPathToTop(gridManager.GetWalkableGrid(), occupied, stickman.GridRow, stickman.GridCol,
            gridManager.Rows, gridManager.Cols);

        if (path == null)
        {
            occupied[stickman.GridRow, stickman.GridCol] = true;
            return;
        }

        isMoving = true;
        gridManager.ClearCell(stickman.GridRow, stickman.GridCol);
        stickman.SetHasPath(false);

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
            OnLose();
            return;
        }

        Vector3 slotPos = busStop.GetSlotPosition(slotIndex);

        stickman.MoveAlongPath(new List<Vector3> { slotPos }, () =>
        {
            busStop.AssignToSlot(slotIndex, stickman);
            gridManager.RefreshAllPaths();
            busManager.TryLoadPassengers(busStop);
            isMoving = false;

            CheckWinCondition();
        });
    }

    private void HandleBusReady(StickmanColor busColor)
    {
        busManager.TryLoadPassengers(busStop);
    }

    private void CheckWinCondition()
    {
        if (gridManager.StickmanCount() == 0 && busStop.IsEmpty() && gridManager.AllSpawnersExhausted())
        {
            OnWin();
        }
    }

    private void OnWin()
    {
        state = PlayState.Won;
        inputHandler.SetEnabled(false);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.currentLevel++;
            SaveManager.Instance.ForceSave();
        }

        if (winPanel != null) winPanel.SetActive(true);
        Debug.Log("[GameplayManager] Level Complete!");
    }

    private void OnLose()
    {
        if (state == PlayState.Lost) return;

        state = PlayState.Lost;
        inputHandler.SetEnabled(false);

        if (losePanel != null) losePanel.SetActive(true);
        Debug.LogWarning("[GameplayManager] Game Over!");
    }

    private void OnDestroy()
    {
        if (inputHandler != null)
            inputHandler.OnStickmanTapped -= HandleStickmanTapped;
        if (busManager != null)
            busManager.OnBusReady -= HandleBusReady;
    }
}
