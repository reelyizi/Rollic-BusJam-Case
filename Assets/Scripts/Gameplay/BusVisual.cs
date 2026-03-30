using UnityEngine;
using TMPro;

public class BusVisual : MonoBehaviour
{
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private GameObject[] seatPassengers;
    [SerializeField] private GameObject reservedCanvas;
    [SerializeField] private TextMeshProUGUI reservedCountText;

    private MaterialPropertyBlock propBlock;
    private int nextSeat;

    public void SetColor(Color color)
    {
        if (bodyRenderer == null) return;

        if (propBlock == null)
            propBlock = new MaterialPropertyBlock();

        bodyRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", color);
        bodyRenderer.SetPropertyBlock(propBlock);

        nextSeat = 0;
        if (seatPassengers != null)
            for (int i = 0; i < seatPassengers.Length; i++)
                if (seatPassengers[i] != null)
                    seatPassengers[i].SetActive(false);

        if (reservedCanvas != null)
            reservedCanvas.SetActive(false);
    }

    public void SetReservedCount(int count)
    {
        if (reservedCanvas != null)
        {
            reservedCanvas.SetActive(count > 0);
            if (reservedCountText != null)
                reservedCountText.text = count.ToString();
        }
    }

    public void ShowNextPassenger(Color color)
    {
        if (seatPassengers == null || nextSeat >= seatPassengers.Length) return;

        var passenger = seatPassengers[nextSeat];
        if (passenger == null) { nextSeat++; return; }

        passenger.SetActive(true);

        var renderer = passenger.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            if (propBlock == null)
                propBlock = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propBlock);
        }

        nextSeat++;
    }
}
