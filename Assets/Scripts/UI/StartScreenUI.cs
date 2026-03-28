using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartScreenUI : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("Play Button")]
    [SerializeField] private Button playButton;
    [SerializeField] private TextMeshProUGUI playButtonText;

    [Header("Settings")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);

        if (settingsButton != null && settingsPanel != null)
            settingsButton.onClick.AddListener(ToggleSettings);

        RefreshUI();

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.StartScreen);
    }

    private void RefreshUI()
    {
        if (SaveManager.Instance == null) return;

        var data = SaveManager.Instance.Data;
        levelText.text = $"Lv. {data.currentLevel}";
        coinText.text = data.coins.ToString();
        playButtonText.text = $"Level {data.currentLevel}";
    }

    private void OnPlayClicked()
    {
        Debug.Log($"[StartScreenUI] Play clicked — starting Level {SaveManager.Instance.Data.currentLevel}");
        // GameManager.Instance.LoadGameplay(); // Enable when Gameplay scene exists
    }

    private void ToggleSettings()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private void OnDestroy()
    {
        playButton.onClick.RemoveListener(OnPlayClicked);
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(ToggleSettings);
    }
}
