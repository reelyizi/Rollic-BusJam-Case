using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "BusJam/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Defaults")]
    public int startingCoins = 200;
    public int startingLevel = 0;

    [Header("Bus")]
    public float busGap = 2f;

    [Header("Wall")]
    public float wallYOffset = -0.5f;

    [Header("Spawner")]
    public float spawnerYOffset = 0f;

    [Header("Hidden Stickman")]
    public Color hiddenStickmanColor = Color.black;
}
