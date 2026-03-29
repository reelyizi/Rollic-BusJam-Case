using UnityEngine;

public class BusVisual : MonoBehaviour
{
    [SerializeField] private Renderer bodyRenderer;

    private MaterialPropertyBlock propBlock;

    public void SetColor(Color color)
    {
        if (bodyRenderer == null) return;

        if (propBlock == null)
            propBlock = new MaterialPropertyBlock();

        bodyRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", color);
        bodyRenderer.SetPropertyBlock(propBlock);
    }
}
