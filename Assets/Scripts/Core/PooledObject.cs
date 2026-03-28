using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public int PoolKey { get; private set; }

    public void Init(int key)
    {
        PoolKey = key;
    }

    public void ReturnToPool()
    {
        ObjectPool.Instance.Return(gameObject);
    }
}
