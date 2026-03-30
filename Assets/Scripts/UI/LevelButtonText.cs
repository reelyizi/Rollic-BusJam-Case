using UnityEngine;
using TMPro;

public class LevelButtonText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;

    private void Start()
    {
        int level = PlayerData.CurrentLevel + 1;
        levelText.text = $"LEVEL {level}";
    }
}
