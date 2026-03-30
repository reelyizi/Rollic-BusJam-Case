using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusManager : MonoBehaviour
{
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private Transform busStopPosition;
    [SerializeField] private float busDriveSpeed = 8f;
    [SerializeField] private float despawnDistance = 15f;
    [SerializeField] private ColorConfig colorConfig;
    [SerializeField] private GameConfig gameConfig;

    private BusDefinition[] busSequence;
    private readonly List<GameObject> activeBuses = new();
    private int currentBusIndex;
    private int currentBusLoaded;
    private int currentBusBoarded;
    private int currentBusReservedLoaded;
    private bool isShifting;
    private BusStop busStop;

    public event Action OnAllBusesComplete;
    public event Action OnSlotFreed;
    public bool IsDriving => isShifting;

    public void Initialize(BusDefinition[] sequence, BusStop stop)
    {
        busSequence = sequence;
        busStop = stop;
        currentBusIndex = 0;
        ResetCounters();
    }

    public void SpawnAllBuses()
    {
        if (busSequence == null) return;

        float gap = gameConfig != null ? gameConfig.busGap : 2f;
        Vector3 stopPos = busStopPosition != null ? busStopPosition.position : transform.position;

        for (int i = 0; i < busSequence.Length; i++)
        {
            var def = busSequence[i];
            Vector3 pos = stopPos - new Vector3(i * gap, 0f, 0f);

            var obj = ObjectPool.Instance.Get(busPrefab, pos, Quaternion.Euler(0f, 90f, 0f));
            obj.name = $"Bus_{i}_{def.color}";

            var busVisual = obj.GetComponent<BusVisual>();
            if (busVisual != null)
            {
                busVisual.SetColor(colorConfig.GetRenderColor(def.color));
                if (def.reservedSeats > 0)
                    busVisual.SetReservedCount(def.reservedSeats);
            }

            activeBuses.Add(obj);
        }
    }

    public void OnPassengerArrived(Stickman stickman)
    {
        if (isShifting || !HasCurrentBus())
        {
            StartCoroutine(RetryAfterShift(stickman));
            return;
        }

        TryBoardStickman(stickman);
    }

    private System.Collections.IEnumerator RetryAfterShift(Stickman stickman)
    {
        while (isShifting)
            yield return null;

        if (HasCurrentBus())
            TryBoardStickman(stickman);
    }

    private void TryBoardStickman(Stickman stickman)
    {
        if (!HasCurrentBus()) return;

        var def = busSequence[currentBusIndex];
        if (stickman.Color != def.color) return;

        if (stickman.IsReserved)
        {
            if (currentBusReservedLoaded < def.reservedSeats)
                SendToBus(stickman, true);
        }
        else
        {
            int normalCapacity = def.capacity - def.reservedSeats;
            int normalLoaded = currentBusLoaded - currentBusReservedLoaded;
            if (normalLoaded < normalCapacity)
                SendToBus(stickman, false);
        }
    }

    private void CheckWaitingPassengers()
    {
        if (!HasCurrentBus()) return;

        var def = busSequence[currentBusIndex];

        while (currentBusReservedLoaded < def.reservedSeats)
        {
            var match = busStop.GetFirstMatchingPassenger(def.color, true);
            if (match == null) break;
            SendToBus(match, true);
        }

        int normalCapacity = def.capacity - def.reservedSeats;
        int normalLoaded = currentBusLoaded - currentBusReservedLoaded;
        while (normalLoaded < normalCapacity)
        {
            var match = busStop.GetFirstMatchingPassenger(def.color, false);
            if (match == null) break;
            SendToBus(match, false);
            normalLoaded++;
        }
    }

    private void SendToBus(Stickman stickman, bool isReservedSeat)
    {
        busStop.MarkBoarding(stickman);
        currentBusLoaded++;
        if (isReservedSeat)
            currentBusReservedLoaded++;

        var currentBus = activeBuses[0];
        Vector3 busPos = currentBus.transform.position;

        stickman.BoardBus(busPos, () =>
        {
            busStop.ClearSlot(stickman);
            OnSlotFreed?.Invoke();

            var busVisual = currentBus.GetComponent<BusVisual>();
            if (busVisual != null)
                busVisual.ShowNextPassenger(colorConfig.GetRenderColor(stickman.Color));

            stickman.gameObject.SetActive(false);
            currentBusBoarded++;

            if (currentBusBoarded >= busSequence[currentBusIndex].capacity)
                StartCoroutine(DriveAwayAndShift());
        });
    }

    private bool HasCurrentBus()
    {
        return currentBusIndex < busSequence.Length && activeBuses.Count > 0;
    }

    private void ResetCounters()
    {
        currentBusLoaded = 0;
        currentBusBoarded = 0;
        currentBusReservedLoaded = 0;
        isShifting = false;
    }

    private IEnumerator DriveAwayAndShift()
    {
        isShifting = true;

        var departing = activeBuses[0];
        activeBuses.RemoveAt(0);

        currentBusIndex++;
        ResetCounters();

        StartCoroutine(DriveOut(departing));

        if (activeBuses.Count > 0)
        {
            float gap = gameConfig != null ? gameConfig.busGap : 2f;
            Vector3 stopPos = busStopPosition != null ? busStopPosition.position : transform.position;
            yield return StartCoroutine(ShiftBuses(stopPos, gap));
        }

        isShifting = false;

        if (currentBusIndex < busSequence.Length)
            CheckWaitingPassengers();
        else
            OnAllBusesComplete?.Invoke();
    }

    private IEnumerator DriveOut(GameObject bus)
    {
        Vector3 driveDir = bus.transform.forward;
        float traveled = 0f;
        while (traveled < despawnDistance)
        {
            float step = busDriveSpeed * Time.deltaTime;
            bus.transform.position += driveDir * step;
            traveled += step;
            yield return null;
        }
        ObjectPool.Instance.Return(bus);
    }

    private IEnumerator ShiftBuses(Vector3 stopPos, float gap)
    {
        var targets = new Vector3[activeBuses.Count];
        for (int i = 0; i < activeBuses.Count; i++)
            targets[i] = stopPos - new Vector3(i * gap, 0f, 0f);

        bool moving = true;
        while (moving)
        {
            moving = false;
            for (int i = 0; i < activeBuses.Count; i++)
            {
                if (Vector3.Distance(activeBuses[i].transform.position, targets[i]) > 0.05f)
                {
                    activeBuses[i].transform.position = Vector3.MoveTowards(
                        activeBuses[i].transform.position, targets[i], busDriveSpeed * Time.deltaTime);
                    moving = true;
                }
            }
            yield return null;
        }
    }
}
