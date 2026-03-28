using UnityEngine;
using TMPro;

public class TimerDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private float remainingTime;
    private bool isRunning;

    private void OnEnable()
    {
        if (SaveManager.Instance != null)
            remainingTime = SaveManager.Instance.Data.timerRemaining;

        isRunning = true;
        UpdateDisplay();
    }

    private void Update()
    {
        if (!isRunning || remainingTime <= 0f) return;

        remainingTime -= Time.unscaledDeltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
        }

        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.timerRemaining = remainingTime;

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResumeTimer()
    {
        isRunning = true;
    }
}
