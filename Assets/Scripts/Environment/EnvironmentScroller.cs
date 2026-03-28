using System.Collections.Generic;
using UnityEngine;

public class EnvironmentScroller : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int count = 3;
    [SerializeField] private float spacing = 15f;
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private float despawnOffset = -15f;

    private readonly List<GameObject> activeObjects = new();

    private void Start()
    {
        for (int i = 0; i < count; i++)
        {
            float z = transform.position.z + i * spacing;
            Spawn(z);
        }
    }

    private void Update()
    {
        float delta = scrollSpeed * Time.deltaTime;

        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            var obj = activeObjects[i];
            var pos = obj.transform.position;
            pos.z -= delta;
            obj.transform.position = pos;

            if (pos.z < transform.position.z + despawnOffset)
            {
                float furthestZ = GetFurthestZ();
                ObjectPool.Instance.Return(obj);
                activeObjects.RemoveAt(i);
                Spawn(furthestZ + spacing);
            }
        }
    }

    private float GetFurthestZ()
    {
        float max = transform.position.z;
        for (int i = 0; i < activeObjects.Count; i++)
        {
            if (activeObjects[i].transform.position.z > max)
                max = activeObjects[i].transform.position.z;
        }
        return max;
    }

    private void Spawn(float z)
    {
        var pos = new Vector3(transform.position.x, transform.position.y, z);
        var obj = ObjectPool.Instance.Get(prefab, pos, Quaternion.identity);
        activeObjects.Add(obj);
    }
}
