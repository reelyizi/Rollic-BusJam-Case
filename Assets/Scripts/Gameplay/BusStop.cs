using System.Collections.Generic;
using UnityEngine;

public class BusStop : MonoBehaviour
{
    [SerializeField] private float slotSpacing = 1.2f;
    [SerializeField] private Transform slotsOrigin;

    private Stickman[] slots;
    private Vector3[] slotPositions;
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
        slotPositions = new Vector3[slotCount];

        Vector3 origin = slotsOrigin != null ? slotsOrigin.position : transform.position;

        for (int i = 0; i < slotCount; i++)
        {
            slotPositions[i] = origin + new Vector3(i * slotSpacing, 0f, 0f);
        }
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
        return slotPositions[index];
    }

    public void AssignToSlot(int index, Stickman stickman)
    {
        slots[index] = stickman;
    }

    public List<Stickman> RemoveMatchingPassengers(StickmanColor busColor)
    {
        var matched = new List<Stickman>();

        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] != null && slots[i].Color == busColor)
            {
                matched.Add(slots[i]);
                slots[i] = null;
            }
        }

        CompactSlots();
        return matched;
    }

    private void CompactSlots()
    {
        int writeIndex = 0;
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] != null)
            {
                if (i != writeIndex)
                {
                    slots[writeIndex] = slots[i];
                    slots[i] = null;

                    slots[writeIndex].transform.position = slotPositions[writeIndex];
                }
                writeIndex++;
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
