using System.Collections.Generic;
using UnityEngine;

public class BusStop : MonoBehaviour
{
    [SerializeField] private Transform slotsParent;

    private Stickman[] slots;
    private bool[] boarding;
    private Transform[] slotTransforms;
    private int slotCount;

    public int SlotCount => slotCount;
    public bool IsFull => AvailableCount <= 0;

    public int OccupiedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slotCount; i++)
                if (slots[i] != null || boarding[i]) count++;
            return count;
        }
    }

    public int AvailableCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slotCount; i++)
                if (slots[i] == null && !boarding[i]) count++;
            return count;
        }
    }

    public void Initialize(int count)
    {
        slotTransforms = new Transform[slotsParent.childCount];
        for (int i = 0; i < slotsParent.childCount; i++)
            slotTransforms[i] = slotsParent.GetChild(i);

        slotCount = Mathf.Clamp(count, 3, 7);
        slotCount = Mathf.Min(slotCount, slotTransforms.Length);
        slots = new Stickman[slotCount];
        boarding = new bool[slotCount];

        for (int i = 0; i < slotTransforms.Length; i++)
            slotTransforms[i].gameObject.SetActive(i < slotCount);
    }

    public bool HasEmptySlot()
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == null && !boarding[i]) return true;
        return false;
    }

    public int GetFirstEmptySlotIndex()
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == null && !boarding[i]) return i;
        return -1;
    }

    public Vector3 GetSlotPosition(int index)
    {
        if (index >= 0 && index < slotTransforms.Length)
            return slotTransforms[index].position;
        return transform.position;
    }

    public void AssignToSlot(int index, Stickman stickman)
    {
        slots[index] = stickman;
        boarding[index] = false;
    }

    public Stickman GetFirstMatchingPassenger(StickmanColor busColor)
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] != null && !boarding[i] && slots[i].Color == busColor)
                return slots[i];
        return null;
    }

    public Stickman GetFirstMatchingPassenger(StickmanColor busColor, bool requireReserved)
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] != null && !boarding[i] && slots[i].Color == busColor && slots[i].IsReserved == requireReserved)
                return slots[i];
        return null;
    }

    public void MarkBoarding(Stickman stickman)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == stickman)
            {
                boarding[i] = true;
                return;
            }
        }
    }

    public void ClearSlot(Stickman stickman)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == stickman)
            {
                slots[i] = null;
                boarding[i] = false;
                return;
            }
        }
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] != null || boarding[i]) return false;
        return true;
    }
}
