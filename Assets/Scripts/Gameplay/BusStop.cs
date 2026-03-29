using System.Collections.Generic;
using UnityEngine;

public class BusStop : MonoBehaviour
{
    [SerializeField] private Transform slotsParent;

    private Stickman[] slots;
    private Transform[] slotTransforms;
    private int slotCount;

    public int SlotCount => slotCount;
    public bool IsFull => OccupiedCount >= slotCount;

    public int OccupiedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slotCount; i++)
                if (slots[i] != null) count++;
            return count;
        }
    }

    public void Initialize(int count)
    {
        slotCount = count;
        slots = new Stickman[slotCount];

        slotTransforms = new Transform[slotsParent.childCount];
        for (int i = 0; i < slotsParent.childCount; i++)
            slotTransforms[i] = slotsParent.GetChild(i);
    }

    public bool HasEmptySlot()
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == null) return true;
        return false;
    }

    public int GetFirstEmptySlotIndex()
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == null) return i;
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
    }

    public Stickman GetFirstMatchingPassenger(StickmanColor busColor)
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] != null && slots[i].Color == busColor)
                return slots[i];
        return null;
    }

    public void RemovePassenger(Stickman stickman)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == stickman)
            {
                slots[i] = null;
                return;
            }
        }
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] != null) return false;
        return true;
    }
}
