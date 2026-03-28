using UnityEngine;

public class BusBounce : MonoBehaviour
{
    [SerializeField] private float bounceHeight = 0.05f;
    [SerializeField] private float bounceSpeed = 2f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.localPosition;
    }

    private void Update()
    {
        var pos = startPos;
        pos.y += Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        transform.localPosition = pos;
    }
}
