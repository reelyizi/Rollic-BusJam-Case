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

    public void LoadStartScreen()
    {
        SceneManager.LoadScene("StartScreen");
        SetState(GameState.StartScreen);
    }

    public void LoadGameplay()
    {
        SceneManager.LoadScene("Gameplay");
        SetState(GameState.Gameplay);
    }

    public void LoadStartScreenAsync(Action onReady)
    {
        var op = SceneManager.LoadSceneAsync("StartScreen");
        op.allowSceneActivation = false;
        StartCoroutine(WaitForSceneLoad(op, onReady));
    }

    private System.Collections.IEnumerator WaitForSceneLoad(AsyncOperation op, Action onReady)
    {
        while (op.progress < 0.9f)
            yield return null;

        onReady?.Invoke();
        op.allowSceneActivation = true;
    }
}
