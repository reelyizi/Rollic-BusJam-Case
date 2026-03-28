using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : Singleton<ObjectPool>
{
    private readonly Dictionary<int, Queue<GameObject>> pools = new();

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        int key = prefab.GetInstanceID();

        if (pools.TryGetValue(key, out var queue) && queue.Count > 0)
        {
            var obj = queue.Dequeue();
            obj.transform.SetParent(parent);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        var newObj = Instantiate(prefab, position, rotation, parent);
        newObj.GetComponent<PooledObject>()?.Init(key);

        if (!newObj.TryGetComponent<PooledObject>(out _))
            newObj.AddComponent<PooledObject>().Init(key);

        return newObj;
    }

    public void Return(GameObject obj)
    {
        if (!obj.TryGetComponent<PooledObject>(out var pooled))
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);

        if (!pools.ContainsKey(pooled.PoolKey))
            pools[pooled.PoolKey] = new Queue<GameObject>();

        pools[pooled.PoolKey].Enqueue(obj);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        int key = prefab.GetInstanceID();

        if (!pools.ContainsKey(key))
            pools[key] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(prefab, transform);
            obj.AddComponent<PooledObject>().Init(key);
            obj.SetActive(false);
            pools[key].Enqueue(obj);
        }
    }
}
