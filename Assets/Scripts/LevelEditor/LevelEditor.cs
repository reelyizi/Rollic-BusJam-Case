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
    public Transform busStopParent;

    public enum BrushType { Normal, Reserved }

    [HideInInspector] public StickmanColor selectedColor = StickmanColor.Red;
    [HideInInspector] public BrushType brushType = BrushType.Normal;
    [HideInInspector] public bool hiddenMode;
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
        if (editData == null) return;

#if UNITY_EDITOR
        if (sourceLevel == null)
        {
            string path = $"Assets/Resources/LevelData/{editData.name}.asset";
            var asset = ScriptableObject.CreateInstance<LevelData>();
            LevelDataCopier.Copy(editData, asset);
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            sourceLevel = asset;
            Debug.Log($"[LevelEditor] Created {path}");
        }
        else
        {
            LevelDataCopier.Copy(editData, sourceLevel);
        }

        UnityEditor.EditorUtility.SetDirty(sourceLevel);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[LevelEditor] Saved {sourceLevel.name}");
#endif
    }
}
