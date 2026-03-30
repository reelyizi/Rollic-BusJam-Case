using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        Loading,
        StartScreen,
        Gameplay,
        GameOver
    }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnStateChanged;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        CurrentState = GameState.Loading;
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void LoadMenuScreen()
    {
        SceneManager.LoadScene("MenuScreen");
        SetState(GameState.StartScreen);
    }

    public void LoadGameplay()
    {
        SceneManager.LoadScene("GameplayScreen");
        SetState(GameState.Gameplay);
    }
}
