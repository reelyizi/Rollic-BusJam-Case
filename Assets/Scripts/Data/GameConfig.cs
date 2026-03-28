using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "BusJam/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Timer")]
    public float defaultTimerDuration = 720f;

    [Header("Defaults")]
    public int startingCoins = 200;
    public int startingLevel = 1;

    [Header("Environment")]
    public float environmentRotationSpeed = 5f;

    [Range(-0.1f, 0.1f)]
    public float curveStrength = 0.01f;
}
