using System;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public event Action<Stickman> OnStickmanTapped;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask stickmanLayer;

    private bool isEnabled;

    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
    }

    private void Update()
    {
        if (!isEnabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, stickmanLayer))
            {
                var stickman = hit.collider.GetComponentInParent<Stickman>();
                if (stickman != null && stickman.HasPath)
                    OnStickmanTapped?.Invoke(stickman);
            }
        }
    }
}
