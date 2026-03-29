using UnityEngine;

[ExecuteInEditMode]
public class LevelEditor : MonoBehaviour
{
    public const int GridRows = 8;
    public const int GridCols = 8;

    [Header("Data")]
    public LevelData sourceLevel;
    public ColorConfig colorConfig;
    public GameConfig gameConfig;

    [Header("Prefabs")]
    public GameObject stickmanPrefab;
    public GameObject busPrefab;
    public GameObject wallPrefab;
    public GameObject spawnerPrefab;

    [Header("Scene References")]
    public Transform gridParent;
    public Transform busSpawnOrigin;

    [HideInInspector] public StickmanColor selectedColor = StickmanColor.Red;
    [HideInInspector] public bool pathMode;
    [HideInInspector] public bool spawnerMode;
    [HideInInspector] public Vector2Int selectedSpawnerCell = new(-1, -1);
    [HideInInspector] public LevelData editData;

    private LevelEditorVisuals visuals;

    public LevelEditorVisuals Visuals
    {
        get
        {
            visuals ??= new LevelEditorVisuals(this);
            return visuals;
        }
    }

    public void LoadLevel()
    {
        if (sourceLevel == null) return;

        editData = ScriptableObject.CreateInstance<LevelData>();
        LevelDataCopier.Copy(sourceLevel, editData);
        Visuals.RebuildScene();
    }

    public void SaveLevel()
    {
        if (sourceLevel == null || editData == null) return;

        LevelDataCopier.Copy(editData, sourceLevel);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(sourceLevel);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[LevelEditor] Saved {sourceLevel.name}");
#endif
    }
}
